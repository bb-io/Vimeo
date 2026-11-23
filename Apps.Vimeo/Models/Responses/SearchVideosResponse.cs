using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Responses;

public class SearchVideosResponse
{
    [Display("Videos")]
    public List<VideoResponse> Videos { get; set; } = [];
}
