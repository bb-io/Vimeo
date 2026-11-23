using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Vimeo.Handlers.Static;

public class PrivacyDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new("nobody", "Private"),
        new("unlisted", "Unlisted"),
        new("disable", "Hide from Vimeo"),
        new("anybody", "Public")
    ];
}
