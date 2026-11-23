using System.Net;
using Apps.Vimeo.Constants;
using Apps.Vimeo.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Vimeo.Api;

public class VimeoClient : BlackBirdRestClient
{
    internal const string VideoFields = "uri,name,description,link,language,duration,created_time,modified_time,privacy.view,privacy.download,status,upload.status,transcode.status";
    internal const string TrackFields = "uri,name,language,type,link,active";
    private const string BaseUrl = "https://api.vimeo.com/";

    public VimeoClient()
        : base(new RestClientOptions
        {
            BaseUrl = new Uri(BaseUrl),
            FollowRedirects = false,
            Timeout = TimeSpan.FromSeconds(25)
        })
    {
        this.AddDefaultHeader("Accept", "application/vnd.vimeo.*+json;version=3.4");
    }

    public VimeoClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : this()
    {
        var token = credentials.FirstOrDefault(item => item.KeyName == CredsNames.AccessToken)?.Value?.Trim();
        if (string.IsNullOrEmpty(token) || token.Any(char.IsWhiteSpace) || token.Any(char.IsControl))
            throw new PluginMisconfigurationException("Reconnect Vimeo or enter a valid authenticated personal access token.");

        this.AddDefaultHeader("Authorization", $"Bearer {token}");
    }

    protected override JsonSerializerSettings? JsonSettings => JsonConfig.JsonSettings;

    public override Task<T> ExecuteWithErrorHandling<T>(RestRequest request) => Read<T>(request);

    public override Task<RestResponse> ExecuteWithErrorHandling(RestRequest request) => Send(request);

    internal async Task<T> Read<T>(RestRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Send(request, cancellationToken);
        try
        {
            return JsonConvert.DeserializeObject<T>(response.Content ?? string.Empty, JsonSettings)
                ?? throw new JsonSerializationException();
        }
        catch (JsonException)
        {
            throw (request.Method == Method.Get || request.Resource == "oauth/access_token")
                ? new PluginApplicationException("Vimeo returned an unreadable response. Try again.")
                : UnconfirmedWrite();
        }
    }

    internal async Task<List<T>> Collection<T>(RestRequest request)
    {
        var results = new List<T>();
        await foreach (var page in Pages<T>(request))
            results.AddRange(page);

        return results;
    }

    internal async IAsyncEnumerable<List<T>> Pages<T>(RestRequest firstPage)
    {
        var request = firstPage;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var pageNumber = 0; pageNumber < 100; pageNumber++)
        {
            var page = ParsePage<T>(await Read<JToken>(request));
            yield return page.Data;
            if (string.IsNullOrEmpty(page.Paging?.Next))
                yield break;

            var next = SafeNextPage(page.Paging.Next);
            if (!visited.Add(next.AbsoluteUri))
                throw new PluginApplicationException("Vimeo repeated a results page. Try again.");

            request = new RestRequest(next);
        }

        throw new PluginApplicationException("Vimeo returned too many results. Narrow the search or select a folder.");
    }

    internal static VimeoPage<T> ParsePage<T>(JToken payload)
    {
        try
        {
            var serializer = JsonSerializer.Create(new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc });
            return payload switch
            {
                JArray entries when entries.ToObject<List<T>>(serializer) is { } values => new() { Data = values },
                JObject obj when obj["data"] is JArray && obj.ToObject<VimeoPage<T>>(serializer) is { } page => page,
                _ => throw new JsonSerializationException()
            };
        }
        catch (JsonException)
        {
            throw new PluginApplicationException("Vimeo returned an unreadable collection.");
        }
    }

    protected override Exception ConfigureErrorException(RestResponse response) => (int)response.StatusCode switch
    {
        400 or 422 => new PluginMisconfigurationException("Vimeo rejected the input. Check the selected IDs, video settings, or caption language and file format."),
        401 => new PluginMisconfigurationException("Vimeo rejected the access token. Reconnect with OAuth or replace the personal access token."),
        402 or 403 => new PluginMisconfigurationException("Vimeo denied this request. Check the token scopes, account ownership, plan, and API upload permission."),
        404 => new PluginMisconfigurationException("Vimeo could not find this resource. Check its ID and whether the connected account can access it."),
        409 or 412 => new PluginMisconfigurationException("The resource is in a state that blocks this operation. Check it in Vimeo and try again."),
        413 => new PluginMisconfigurationException("Vimeo rejected the file size. Check the account limits and try a smaller file."),
        429 => new PluginApplicationException("Vimeo rate limited the request. Wait for the account's rate limit to reset and try again."),
        _ => new PluginApplicationException($"Vimeo returned HTTP {(int)response.StatusCode}. Check Vimeo's status and try again.")
    };

    private async Task<RestResponse> Send(RestRequest request, CancellationToken cancellationToken = default)
    {
        var isCreate = request.Method == Method.Post && request.Resource != "oauth/access_token";
        RestResponse response;
        try
        {
            response = await ExecuteAsync(request, cancellationToken);
            if (request.Method == Method.Get && response.StatusCode == HttpStatusCode.TooManyRequests &&
                ShortRetryDelay(response) is { } delay)
            {
                await Task.Delay(delay, cancellationToken);
                response = await ExecuteAsync(request, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutException ||
                                          exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw isCreate ? UnconfirmedWrite() : Unreachable();
        }

        if (response.IsSuccessful)
            return response;

        var status = (int)response.StatusCode;
        if (isCreate && (status is 0 or 408 || status >= 500 || status is >= 300 and < 400))
            throw UnconfirmedWrite();

        throw status is 0 or 408 ? Unreachable() : ConfigureErrorException(response);
    }

    internal static Uri SafeNextPage(string value)
    {
        if (!Uri.TryCreate(new Uri(BaseUrl), value, out var next) || next.Scheme != Uri.UriSchemeHttps ||
            !next.Host.Equals("api.vimeo.com", StringComparison.OrdinalIgnoreCase) || !next.IsDefaultPort ||
            next.UserInfo.Length > 0)
            throw new PluginApplicationException("Vimeo returned an invalid results page URL.");

        return next;
    }

    private static TimeSpan? ShortRetryDelay(RestResponse response)
    {
        var value = response.Headers?.FirstOrDefault(header =>
            string.Equals(header.Name, "Retry-After", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
        if (int.TryParse(value, out var seconds) && seconds is >= 0 and <= 5)
            return TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(150);

        return null;
    }

    private static PluginApplicationException Unreachable() =>
        new("Blackbird could not reach Vimeo or the request timed out. Check Vimeo's status and try again.");

    private static PluginApplicationException UnconfirmedWrite() =>
        new("Vimeo did not confirm the new item. Check Vimeo for the result before repeating this step, since repeating it could create a duplicate.");
}
