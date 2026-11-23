using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Responses;

public class TextTrackResponse
{
    [Display("Text track ID")]
    public string TrackId { get; set; } = string.Empty;

    [Display("Name")]
    public string? Name { get; set; }

    [Display("Language")]
    public string? Language { get; set; }

    [Display("Track type")]
    public string? Type { get; set; }

    [Display("Active")]
    public bool Active { get; set; }
}
