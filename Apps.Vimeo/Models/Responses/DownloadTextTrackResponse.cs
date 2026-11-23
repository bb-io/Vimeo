using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Vimeo.Models.Responses;

public class DownloadTextTrackResponse
{
    [Display("WebVTT file")]
    public FileReference File { get; set; } = default!;
}
