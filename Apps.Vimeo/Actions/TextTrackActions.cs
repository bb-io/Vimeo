using Apps.Vimeo.Invocables;
using System.Text;
using Apps.Vimeo.Api;
using Apps.Vimeo.Models.Dtos;
using Apps.Vimeo.Models.Requests;
using Apps.Vimeo.Models.Responses;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Vimeo.Actions;

[ActionList("Text tracks")]
public class TextTrackActions(InvocationContext context, IFileManagementClient files) : VimeoInvocable(context)
{
    private const int MaxCaptionBytes = 10 * 1024 * 1024;

    [Action("Search text tracks", Description = "Find subtitles and captions for a selected video.")]
    public async Task<SearchTextTracksResponse> SearchTextTracks([ActionParameter] VideoRequest input)
    {
        var videoId = VimeoIds.Video(input.VideoId);
        using var client = CreateClient();
        var tracks = await client.Collection<TrackDto>(new RestRequest($"videos/{videoId}/texttracks"));
        return new() { Tracks = tracks.Select(ToResponse).ToList() };
    }

    [Action("Download text track", Description = "Download a subtitle or caption track as a WebVTT file.")]
    public async Task<DownloadTextTrackResponse> DownloadTextTrack([ActionParameter] TextTrackRequest input)
    {
        var videoId = VimeoIds.Video(input.VideoId);
        var trackId = VimeoIds.Numeric(input.TrackId, "text track");
        var path = $"videos/{videoId}/texttracks/{trackId}";

        using var client = CreateClient();
        var track = await client.Read<TrackDto>(new RestRequest(path).AddQueryParameter("fields", VimeoClient.TrackFields));
        if (string.IsNullOrWhiteSpace(track.Link))
            throw new PluginMisconfigurationException("Vimeo did not provide a file for this track. Check the account owner connection and the track in Vimeo.");

        using var response = await VimeoTransfer.DownloadTrack(track.Link);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            return new() { File = await files.UploadAsync(stream, "text/vtt", $"vimeo-{videoId}-{trackId}.vtt") };
        }
        catch (Exception exception) when (exception is not PluginMisconfigurationException)
        {
            throw new PluginApplicationException("Blackbird could not save the downloaded Vimeo text track. Try again.");
        }
    }

    [Action("Upload text track", Description = "Upload a WebVTT subtitle or caption file. You can activate it after upload or leave it inactive for review.")]
    public async Task<TextTrackResponse> UploadTextTrack([ActionParameter] UploadTextTrackRequest input)
    {
        var videoId = VimeoIds.Video(input.VideoId);
        var type = input.Type ?? "subtitles";
        var language = input.Language?.Trim() ?? string.Empty;
        if (language.Length is < 2 or > 35 || !language.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            throw new PluginMisconfigurationException("Choose a Vimeo text track language or enter a valid language code.");

        if (type is not ("subtitles" or "captions"))
            throw new PluginMisconfigurationException("Choose Subtitles or Captions as the track type.");

        var file = await ReadCaption(input);
        var name = string.IsNullOrWhiteSpace(input.Name) ? language : input.Name.Trim();
        using var client = CreateClient();
        var track = await client.Read<TrackDto>(new RestRequest($"videos/{videoId}/texttracks", Method.Post)
            .AddJsonBody(new { name, type, language, active = false }));
        var trackId = VimeoIds.Numeric(VimeoIds.FromUri(track.Uri), "text track");

        try
        {
            await VimeoTransfer.UploadTrack(track.Link, file);
            if (input.Activate == true)
                track = await SetTrack(client, videoId, trackId, new Dictionary<string, object> { ["active"] = true });
        }
        catch (Exception exception) when (exception is PluginMisconfigurationException or PluginApplicationException)
        {
            throw new PluginApplicationException(
                $"{exception.Message} Text track ID {trackId} already exists on Vimeo video {videoId}. Check it before repeating the step.");
        }

        return ToResponse(track);
    }

    [Action("Update text track", Description = "Rename a subtitle or caption track, or activate or deactivate it in the player.")]
    public async Task<TextTrackResponse> UpdateTextTrack([ActionParameter] UpdateTextTrackRequest input)
    {
        var videoId = VimeoIds.Video(input.VideoId);
        var trackId = VimeoIds.Numeric(input.TrackId, "text track");
        var changes = new Dictionary<string, object>();
        if (input.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new PluginMisconfigurationException("Enter a name that contains text.");

            changes["name"] = input.Name.Trim();
        }

        if (input.Active is bool active)
            changes["active"] = active;
        if (changes.Count == 0)
            throw new PluginMisconfigurationException("Enter a name or choose whether the track should be active.");

        using var client = CreateClient();
        return ToResponse(await SetTrack(client, videoId, trackId, changes));
    }

    private async Task<byte[]> ReadCaption(UploadTextTrackRequest input)
    {
        if (input.File is null || !string.Equals(Path.GetExtension(input.File.Name), ".vtt", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Map a WebVTT file with the .vtt extension.");

        try
        {
            await using var stream = await files.DownloadAsync(input.File);
            using var content = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                if (content.Length + read > MaxCaptionBytes)
                    throw new PluginMisconfigurationException("Choose a WebVTT file that is 10 MB or smaller.");

                content.Write(buffer, 0, read);
            }

            var bytes = content.ToArray();
            var text = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
            if (!text.StartsWith("WEBVTT", StringComparison.Ordinal) ||
                (text.Length > 6 && text[6] is not (' ' or '\t' or '\r' or '\n')))
                throw new PluginMisconfigurationException("Use a valid UTF-8 WebVTT file starting with WEBVTT.");

            return bytes;
        }
        catch (DecoderFallbackException)
        {
            throw new PluginMisconfigurationException("Save the WebVTT file with UTF-8 encoding and try again.");
        }
        catch (Exception exception) when (exception is not PluginMisconfigurationException)
        {
            throw new PluginApplicationException("Blackbird could not read the WebVTT file. Check the input file and try again.");
        }
    }

    private static Task<TrackDto> SetTrack(VimeoClient client, string videoId, string trackId, Dictionary<string, object> changes) =>
        client.Read<TrackDto>(new RestRequest($"videos/{videoId}/texttracks/{trackId}", Method.Patch).AddJsonBody(changes));

    private static TextTrackResponse ToResponse(TrackDto track) => new()
    {
        TrackId = VimeoIds.FromUri(track.Uri),
        Name = track.Name,
        Language = track.Language,
        Type = track.Type,
        Active = track.Active
    };
}
