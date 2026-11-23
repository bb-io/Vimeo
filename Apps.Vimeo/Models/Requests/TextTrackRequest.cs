using Apps.Vimeo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Vimeo.Models.Requests;

public class TextTrackRequest : VideoRequest
{
    [Display("Text track ID", Description = "Select a track or map the ID from a previous step.")]
    [DataSource(typeof(TextTrackDataHandler))]
    public string TrackId { get; set; } = string.Empty;
}
