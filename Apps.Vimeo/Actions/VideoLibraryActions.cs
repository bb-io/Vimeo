using Apps.Vimeo.Invocables;
using Apps.Vimeo.Api;
using Apps.Vimeo.Models.Dtos;
using Apps.Vimeo.Models.Requests;
using Apps.Vimeo.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Vimeo.Actions;

[ActionList("Video library")]
public class VideoLibraryActions(InvocationContext context) : VimeoInvocable(context)
{
    [Action("Search videos", Description = "Find videos in the connected Vimeo account or a selected folder.")]
    public async Task<SearchVideosResponse> SearchVideos([ActionParameter] SearchVideosRequest input)
    {
        var limit = input.Limit ?? 50;
        if (limit is < 1 or > 500)
            throw new PluginMisconfigurationException("Enter a maximum result count between 1 and 500.");

        var term = input.Query?.Trim();
        var folderId = input.FolderId is null ? null : VimeoIds.Numeric(input.FolderId, "folder");
        var request = SearchRequest(term, folderId);
        var response = new SearchVideosResponse();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        using var client = CreateClient();
        await foreach (var page in client.Pages<VideoDto>(request))
        {
            foreach (var video in page)
            {
                if (folderId is not null && !string.IsNullOrEmpty(term) &&
                    video.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) != true &&
                    video.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) != true)
                    continue;

                if (!seen.Add(video.Uri))
                    continue;

                response.Videos.Add(ToResponse(video));
                if (response.Videos.Count == limit)
                    return response;
            }
        }

        return response;
    }

    [Action("Get video", Description = "Get a video's details, privacy, and upload and transcoding status.")]
    public async Task<VideoResponse> GetVideo([ActionParameter] VideoRequest input)
    {
        using var client = CreateClient();
        var id = VimeoIds.Video(input.VideoId);
        var video = await client.Read<VideoDto>(new RestRequest($"videos/{id}").AddQueryParameter("fields", VimeoClient.VideoFields));
        return ToResponse(video);
    }

    [Action("Update video", Description = "Change a video's title, description, visibility, or download setting.")]
    public async Task<VideoResponse> UpdateVideo([ActionParameter] UpdateVideoRequest input)
    {
        var id = VimeoIds.Video(input.VideoId);
        var body = Metadata(input);
        if (body.Count == 0)
            throw new PluginMisconfigurationException("Enter at least one video setting to update.");

        using var client = CreateClient();
        var request = new RestRequest($"videos/{id}", Method.Patch)
            .AddQueryParameter("fields", VimeoClient.VideoFields)
            .AddJsonBody(body);
        return ToResponse(await client.Read<VideoDto>(request));
    }

    internal static RestRequest SearchRequest(string? query, string? folderId)
    {
        var resource = folderId is null ? "me/videos" : $"me/projects/{folderId}/videos";
        var request = new RestRequest(resource)
            .AddQueryParameter("per_page", 100)
            .AddQueryParameter("sort", "date")
            .AddQueryParameter("direction", "desc")
            .AddQueryParameter("fields", $"data.{VimeoClient.VideoFields.Replace(",", ",data.")},paging.next");

        if (folderId is null && !string.IsNullOrWhiteSpace(query))
            request.AddQueryParameter("query", query);

        return request;
    }

    internal static Dictionary<string, object> Metadata(VideoMetadataRequest input, bool isUpload = false)
    {
        var body = new Dictionary<string, object>();
        if (input.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                throw new PluginMisconfigurationException("Enter a title that contains text.");

            body["name"] = input.Title.Trim();
        }

        if (input.Description is not null)
            body["description"] = input.Description;

        var privacy = new Dictionary<string, object>();
        var visibility = input.Privacy ?? (isUpload ? "nobody" : null);
        if (visibility is not null)
        {
            if (visibility is not ("anybody" or "nobody" or "disable" or "unlisted"))
                throw new PluginMisconfigurationException("Choose Public, Private, Unlisted, or Hide from Vimeo.");

            privacy["view"] = visibility;
        }

        if (input.AllowDownloads is bool allow)
            privacy["download"] = allow;
        if (privacy.Count > 0)
            body["privacy"] = privacy;

        return body;
    }

    internal static VideoResponse ToResponse(VideoDto video) => new()
    {
        VideoId = VimeoIds.FromUri(video.Uri),
        Title = video.Name,
        Description = video.Description,
        Url = video.Link,
        Language = video.Language,
        DurationSeconds = video.Duration,
        CreatedAt = video.CreatedAt,
        UpdatedAt = video.UpdatedAt,
        Privacy = video.Privacy?.View,
        AllowDownloads = video.Privacy?.Download,
        Status = video.Status,
        UploadStatus = video.Upload?.Status,
        TranscodeStatus = video.Transcode?.Status,
        IsReady = video.Transcode?.Status == "complete" ||
                  (video.Transcode?.Status is null && video.Status == "available")
    };
}
