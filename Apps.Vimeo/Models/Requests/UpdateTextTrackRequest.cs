using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Requests;

public class UpdateTextTrackRequest : TextTrackRequest
{
    [Display("Name", Description = "The name shown in the player.")]
    public string? Name { get; set; }

    [Display("Active", Description = "Enable or disable this track in the player.")]
    public bool? Active { get; set; }
}
