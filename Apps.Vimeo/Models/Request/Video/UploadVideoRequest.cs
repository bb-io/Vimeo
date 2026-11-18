using Apps.Vimeo.DataSourceHandlers.Static;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Vimeo.Models.Request.Video;

public class UploadVideoRequest
{
    public FileReference File { get; set; }
    
    public string? Name { get; set; }
    
    public string? Description { get; set; }
    
    [StaticDataSource(typeof(VideoPrivacyViewHandler))]
    public string? PrivacyView { get; set; }
}