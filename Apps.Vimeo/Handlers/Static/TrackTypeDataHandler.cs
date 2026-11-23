using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Vimeo.Handlers.Static;

public class TrackTypeDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new("subtitles", "Subtitles"),
        new("captions", "Captions")
    ];
}
