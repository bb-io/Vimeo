using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Responses;

public class SearchTextTracksResponse
{
    [Display("Text tracks")]
    public List<TextTrackResponse> Tracks { get; set; } = [];
}
