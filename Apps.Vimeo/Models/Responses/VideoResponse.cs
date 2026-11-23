using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Responses;

public class VideoResponse
{
    [Display("Video ID")]
    public string VideoId { get; set; } = string.Empty;

    [Display("Title")]
    public string? Title { get; set; }

    [Display("Description")]
    public string? Description { get; set; }

    [Display("Vimeo URL")]
    public string? Url { get; set; }

    [Display("Language")]
    public string? Language { get; set; }

    [Display("Duration in seconds")]
    public int? DurationSeconds { get; set; }

    [Display("Created at")]
    public DateTime? CreatedAt { get; set; }

    [Display("Updated at")]
    public DateTime? UpdatedAt { get; set; }

    [Display("Who can watch")]
    public string? Privacy { get; set; }

    [Display("Downloads allowed")]
    public bool? AllowDownloads { get; set; }

    [Display("Vimeo status")]
    public string? Status { get; set; }

    [Display("Upload status")]
    public string? UploadStatus { get; set; }

    [Display("Transcode status", Description = "Vimeo reports complete, in_progress, or error.")]
    public string? TranscodeStatus { get; set; }

    [Display("Ready to play", Description = "True when Vimeo reports the video is available or transcoding is complete.")]
    public bool IsReady { get; set; }
}
