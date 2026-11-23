using Apps.Vimeo.Invocables;
using System.Globalization;
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

[ActionList("Uploads")]
public class UploadActions(InvocationContext context, IFileManagementClient files) : VimeoInvocable(context)
{
    [Action("Upload video file", Description = "Send a video file to Vimeo. The video is private unless you choose another visibility.")]
    public async Task<VideoResponse> UploadVideoFile([ActionParameter] UploadVideoRequest input)
    {
        if (input.File?.Size is not { } size || size <= 0)
            throw new PluginMisconfigurationException("Map a non-empty video file with a known size from an earlier step.");

        var metadata = VideoLibraryActions.Metadata(input, isUpload: true);
        if (input.Title is null && !string.IsNullOrWhiteSpace(input.File.Name))
            metadata["name"] = input.File.Name;

        metadata["upload"] = new { approach = "tus", size = size.ToString(CultureInfo.InvariantCulture) };

        Stream source;
        try
        {
            source = await files.DownloadAsync(input.File);
        }
        catch (Exception exception) when (exception is not PluginMisconfigurationException)
        {
            throw new PluginApplicationException("Blackbird could not open the source video file. Check the file in the previous step.");
        }

        await using (source)
        {
            using var client = CreateClient();
            var video = await client.Read<VideoDto>(new RestRequest("me/videos", Method.Post).AddJsonBody(metadata));
            var videoId = VimeoIds.FromUri(video.Uri);
            try
            {
                await VimeoTransfer.UploadVideo(video.Upload?.UploadLink, source, size);
            }
            catch (Exception exception) when (exception is PluginMisconfigurationException or PluginApplicationException or
                                               IOException or OperationCanceledException or HttpRequestException)
            {
                var reason = exception is PluginMisconfigurationException or PluginApplicationException
                    ? exception.Message
                    : "Blackbird could not finish reading the source video file.";
                throw new PluginApplicationException($"{reason} Vimeo video ID {videoId} may contain an incomplete upload. Check it before repeating the step.");
            }

            return VideoLibraryActions.ToResponse(video);
        }
    }

    [Action("Import video from URL", Description = "Ask Vimeo to fetch a video file from a direct HTTPS URL. The video is private by default.")]
    public async Task<VideoResponse> ImportVideo([ActionParameter] ImportVideoRequest input)
    {
        if (!Uri.TryCreate(input.SourceUrl?.Trim(), UriKind.Absolute, out var source) ||
            source.Scheme != Uri.UriSchemeHttps || source.UserInfo.Length > 0 || source.IsLoopback ||
            source.AbsoluteUri.Length > 16_384)
        {
            throw new PluginMisconfigurationException("Enter a direct HTTPS video file URL that Vimeo can access.");
        }

        if (input.SizeBytes <= 0)
            throw new PluginMisconfigurationException("Enter the source video file size in bytes.");

        var metadata = VideoLibraryActions.Metadata(input, isUpload: true);
        metadata["upload"] = new
        {
            approach = "pull",
            link = source.AbsoluteUri,
            size = input.SizeBytes.ToString(CultureInfo.InvariantCulture)
        };

        using var client = CreateClient();
        var video = await client.Read<VideoDto>(new RestRequest("me/videos", Method.Post).AddJsonBody(metadata));
        return VideoLibraryActions.ToResponse(video);
    }
}
