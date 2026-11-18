using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Vimeo.DataSourceHandlers.Static;

public class VideoPrivacyViewHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData() => new()
    {
        { "anybody", "Anybody" },
        { "nobody", "Nobody" },
        { "unlisted", "Unlisted" },
    };
}