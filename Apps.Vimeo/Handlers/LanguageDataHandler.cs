using Apps.Vimeo.Invocables;
using Apps.Vimeo.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Vimeo.Handlers;

public class LanguageDataHandler(InvocationContext context) : VimeoInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        var term = context.SearchString?.Trim() ?? string.Empty;
        var languages = await client.Collection<LanguageDto>(
            new RestRequest("languages").AddQueryParameter("filter", "texttracks"));

        return languages.Where(item => item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                       item.Code.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new DataSourceItem(item.Code, $"{item.Name} ({item.Code})")).ToList();
    }
}
