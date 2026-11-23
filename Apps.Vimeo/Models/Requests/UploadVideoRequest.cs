using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Vimeo.Models.Requests;

public class UploadVideoRequest : VideoMetadataRequest
{
    [Display("File", Description = "Map a file from a previous step.")]
    public FileReference File { get; set; } = default!;
}
