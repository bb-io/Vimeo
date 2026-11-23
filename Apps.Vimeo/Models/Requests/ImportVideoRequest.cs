using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Requests;

public class ImportVideoRequest : VideoMetadataRequest
{
    [Display("Source video URL", Description = "Use a direct HTTPS file URL Vimeo can fetch without cookies or authorization.")]
    public string SourceUrl { get; set; } = string.Empty;

    [Display("File size in bytes", Description = "Map the size of the source file in bytes from your storage provider.")]
    public long SizeBytes { get; set; }
}
