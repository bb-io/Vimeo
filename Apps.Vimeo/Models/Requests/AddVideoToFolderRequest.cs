using Apps.Vimeo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Vimeo.Models.Requests;

public class AddVideoToFolderRequest : FolderRequest
{
    [Display("Video ID", Description = "Select a video or enter its numeric ID or Vimeo URL.")]
    [DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = string.Empty;
}
