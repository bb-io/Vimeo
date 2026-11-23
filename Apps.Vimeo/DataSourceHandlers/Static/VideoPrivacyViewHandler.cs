using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Vimeo.DataSourceHandlers.Static;

public class VideoPrivacyViewHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new("anybody", "Anybody"),
        new("nobody", "Nobody"),
        new("unlisted", "Unlisted")
    ];
}
