using Apps.Vimeo.Invocables;
using Apps.Vimeo.Actions;
using Apps.Vimeo.Api;
using Apps.Vimeo.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Vimeo.Handlers;

public class VideoDataHandler(InvocationContext context) : VimeoInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        var term = context.SearchString?.Trim() ?? string.Empty;
        var request = VideoLibraryActions.SearchRequest(term, null);
        var matches = new List<DataSourceItem>();

        await foreach (var page in client.Pages<VideoDto>(request))
        {
            matches.AddRange(page.Select(item =>
            {
                var id = VimeoIds.FromUri(item.Uri);
                return new DataSourceItem(id, $"{item.Name} ({id})");
            }).Take(50 - matches.Count));

            if (matches.Count >= 50)
                break;
        }

        return matches;
    }
}
