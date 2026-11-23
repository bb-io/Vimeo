using Apps.Vimeo.Invocables;
using Apps.Vimeo.Api;
using Apps.Vimeo.Models.Dtos;
using Apps.Vimeo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Vimeo.Handlers;

public class TextTrackDataHandler(InvocationContext context, [ActionParameter] VideoRequest video)
    : VimeoInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(video?.VideoId))
            return [];

        var videoId = VimeoIds.Video(video.VideoId);
        var term = context.SearchString?.Trim() ?? string.Empty;
        using var client = CreateClient();
        var tracks = await client.Collection<TrackDto>(new RestRequest($"videos/{videoId}/texttracks"));
        return tracks.Select(track =>
            {
                var id = VimeoIds.FromUri(track.Uri);
                return new DataSourceItem(id, $"{track.Name ?? track.Language} ({track.Language}, {track.Type}, {id})");
            })
            .Where(track => track.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Take(50)
            .ToList();
    }
}
