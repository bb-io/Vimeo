using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Vimeo.Api;

internal static class VimeoTransfer
{
    private const int ChunkSize = 16 * 1024 * 1024;
    private const string TusVersion = "1.0.0";

    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    {
        Timeout = TimeSpan.FromMinutes(3)
    };

    internal static async Task UploadVideo(string? uploadLink, Stream source, long size, HttpClient? httpClient = null)
    {
        var client = httpClient ?? Client;
        var uri = TransferUri(uploadLink);
        var buffer = new byte[(int)Math.Min(ChunkSize, size)];
        long offset = 0;
        while (offset < size)
        {
            var length = (int)Math.Min(buffer.Length, size - offset);
            await source.ReadExactlyAsync(buffer.AsMemory(0, length));
            await UploadChunk(client, uri, buffer, length, offset);
            offset += length;
        }

        if (await source.ReadAsync(buffer.AsMemory(0, 1)) != 0)
            throw new PluginMisconfigurationException("The source file size changed while Vimeo was receiving it. Check the original file.");
    }

    internal static async Task UploadTrack(string? link, byte[] file)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, TransferUri(link))
        {
            Content = new ByteArrayContent(file)
        };
        request.Headers.Accept.ParseAdd("application/vnd.vimeo.*+json;version=3.4");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/vtt");

        using var response = await Send(request);
        if (!response.IsSuccessStatusCode)
            throw TransferError(response.StatusCode);
    }

    internal static async Task<HttpResponseMessage> DownloadTrack(string? link)
    {
        var uri = TransferUri(link);
        for (var redirects = 0; redirects <= 3; redirects++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await Send(request);
            if (response.IsSuccessStatusCode)
                return response;

            var location = response.Headers.Location;
            var status = response.StatusCode;
            response.Dispose();
            if ((int)status is >= 300 and < 400 && location is not null)
            {
                uri = TransferUri(new Uri(uri, location).AbsoluteUri);
                continue;
            }

            throw TransferError(status);
        }

        throw new PluginApplicationException("Vimeo redirected the caption file too many times.");
    }

    internal static Uri TransferUri(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length > 0 ||
            !IsVimeoHost(uri.IdnHost))
        {
            throw new PluginApplicationException("Vimeo returned an unsupported file transfer URL.");
        }

        return uri;
    }

    private static async Task UploadChunk(HttpClient client, Uri uri, byte[] buffer, int length, long start)
    {
        var end = start + length;
        var offset = start;
        for (var attempt = 0; attempt < 3 && offset < end; attempt++)
        {
            using var request = TusRequest(HttpMethod.Patch, uri);
            request.Headers.Add("Upload-Offset", offset.ToString(CultureInfo.InvariantCulture));
            var sent = (int)(offset - start);
            request.Content = new ByteArrayContent(buffer, sent, length - sent);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");

            HttpResponseMessage? response = null;
            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
            }

            using (response)
            {
                if (response?.IsSuccessStatusCode == true && Offset(response) is long returned)
                {
                    offset = returned;
                }
                else
                {
                    if (response is not null && !response.IsSuccessStatusCode &&
                        response.StatusCode is not (HttpStatusCode.Conflict or HttpStatusCode.RequestTimeout) &&
                        (int)response.StatusCode < 500)
                    {
                        throw TransferError(response.StatusCode);
                    }

                    using var check = TusRequest(HttpMethod.Head, uri);
                    using var confirmed = await Send(check, client);
                    if (!confirmed.IsSuccessStatusCode)
                        throw TransferError(confirmed.StatusCode);

                    offset = Offset(confirmed) ?? -1;
                }
            }

            if (offset < start || offset > end)
                throw new PluginApplicationException("Vimeo returned an unexpected upload offset.");
        }

        if (offset != end)
            throw new PluginApplicationException("Vimeo could not confirm receipt of the complete video.");
    }

    private static HttpRequestMessage TusRequest(HttpMethod method, Uri uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Tus-Resumable", TusVersion);
        return request;
    }

    private static long? Offset(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Upload-Offset", out var values) &&
        long.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private static async Task<HttpResponseMessage> Send(HttpRequestMessage request, HttpClient? httpClient = null)
    {
        try
        {
            return await (httpClient ?? Client).SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            throw new PluginApplicationException("The Vimeo file transfer failed or timed out. Try again later.");
        }
    }

    private static bool IsVimeoHost(string host) =>
        host.Equals("vimeo.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".vimeo.com", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("vimeocdn.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".vimeocdn.com", StringComparison.OrdinalIgnoreCase);

    private static Exception TransferError(HttpStatusCode status) => (int)status switch
    {
        400 => new PluginMisconfigurationException("Vimeo rejected the file. Check its format and try again."),
        401 or 403 or 404 or 410 => new PluginMisconfigurationException("The Vimeo file link is unavailable or expired. Check the item in Vimeo."),
        413 => new PluginMisconfigurationException("Vimeo rejected the size of this file."),
        429 => new PluginApplicationException("Vimeo rate limited this file transfer. Try again later."),
        _ => new PluginApplicationException($"Vimeo returned HTTP {(int)status} during the file transfer.")
    };
}
