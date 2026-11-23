using Apps.Vimeo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Vimeo.Models.Requests;

public class VideoMetadataRequest
{
    [Display("Title", Description = "The title shown on Vimeo.")]
    public string? Title { get; set; }

    [Display("Description", Description = "The description shown on Vimeo.")]
    public string? Description { get; set; }

    [Display("Who can watch", Description = "Choose a visibility setting supported by your Vimeo plan.")]
    [StaticDataSource(typeof(PrivacyDataHandler))]
    public string? Privacy { get; set; }

    [Display("Allow downloads", Description = "Control whether viewers can download the video.")]
    public bool? AllowDownloads { get; set; }
}
