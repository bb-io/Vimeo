using Apps.Vimeo.Invocables;
using Apps.Vimeo.Actions;
using Apps.Vimeo.Api;
using Apps.Vimeo.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Vimeo.Handlers;

public class FolderDataHandler(InvocationContext context) : VimeoInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        var term = context.SearchString?.Trim() ?? string.Empty;
        var matches = new List<DataSourceItem>();

        await foreach (var page in client.Pages<FolderDto>(FolderActions.SearchRequest()))
        {
            foreach (var item in page)
            {
                var id = VimeoIds.FromUri(item.Uri);
                if (item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || id.Contains(term, StringComparison.Ordinal))
                    matches.Add(new DataSourceItem(id, $"{item.Name} ({id})"));

                if (matches.Count == 50)
                    return matches;
            }
        }

        return matches;
    }
}
