using Apps.Vimeo.Invocables;
using Apps.Vimeo.Actions;
using Apps.Vimeo.Models.Dtos;
using Apps.Vimeo.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.Vimeo.Polling;

[PollingEventList("Videos")]
public class VideoPolling(InvocationContext context) : VimeoInvocable(context)
{
    private const int BatchSize = 100;
    private static readonly TimeSpan Overlap = TimeSpan.FromMinutes(5);

    [PollingEvent("On new video added", Description = "Start one Flight per new video in the connected Vimeo account.")]
    [MultipleEvents]
    public async Task<PollingEventResponse<VideoPollingMemory, List<VideoResponse>>> OnNewVideo(
        PollingEventRequest<VideoPollingMemory> request)
    {
        var now = DateTime.UtcNow;
        var memory = request.Memory;
        if (memory is null || memory.StartedAt == default || memory.ScannedThrough == default)
            return BuildResponse(null, now, []);

        var cutoff = GetCutoff(memory.StartedAt, memory.ScannedThrough);
        using var client = CreateClient();
        var videos = new List<VideoResponse>();
        await foreach (var page in client.Pages<VideoDto>(VideoLibraryActions.SearchRequest(null, null)))
        {
            videos.AddRange(page.Where(video => video.CreatedAt > cutoff && video.CreatedAt <= now)
                .Select(VideoLibraryActions.ToResponse));

            if (page.Any(video => video.CreatedAt <= cutoff))
                break;
        }

        return BuildResponse(memory, now, videos);
    }

    internal static PollingEventResponse<VideoPollingMemory, List<VideoResponse>> BuildResponse(
        VideoPollingMemory? memory, DateTime now, IEnumerable<VideoResponse> videos)
    {
        if (memory is null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new() { StartedAt = now, ScannedThrough = now }
            };
        }

        var cutoff = GetCutoff(memory.StartedAt, memory.ScannedThrough);
        var candidates = videos
            .Where(video => video.CreatedAt > cutoff && video.CreatedAt <= now &&
                            video.VideoId.Length > 0 && !memory.EmittedVideoIds.ContainsKey(video.VideoId))
            .DistinctBy(video => video.VideoId)
            .OrderBy(video => video.CreatedAt)
            .ThenBy(video => video.VideoId, StringComparer.Ordinal)
            .Take(BatchSize + 1)
            .ToList();
        var batch = candidates.Take(BatchSize).ToList();
        var scannedThrough = candidates.Count <= BatchSize ? now : memory.ScannedThrough;
        var nextCutoff = GetCutoff(memory.StartedAt, scannedThrough);
        var emitted = memory.EmittedVideoIds
            .Where(item => item.Value > nextCutoff)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

        foreach (var video in batch.Where(video => video.CreatedAt > nextCutoff))
            emitted[video.VideoId] = video.CreatedAt!.Value;

        return new()
        {
            FlyBird = batch.Count > 0,
            Result = batch.Count > 0 ? batch : null,
            Memory = new()
            {
                StartedAt = memory.StartedAt,
                ScannedThrough = scannedThrough,
                EmittedVideoIds = emitted
            }
        };
    }

    private static DateTime GetCutoff(DateTime startedAt, DateTime scannedThrough)
    {
        var overlapped = scannedThrough - Overlap;
        return overlapped > startedAt ? overlapped : startedAt;
    }
}
