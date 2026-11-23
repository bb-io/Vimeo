using Apps.Vimeo.Handlers;
using Apps.Vimeo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Vimeo.Models.Requests;

public class UploadTextTrackRequest : VideoRequest
{
    [Display("WebVTT file", Description = "Map a UTF-8 .vtt file from a previous step.")]
    public FileReference File { get; set; } = default!;

    [Display("Language", Description = "The language of the WebVTT file.")]
    [DataSource(typeof(LanguageDataHandler))]
    public string Language { get; set; } = string.Empty;

    [Display("Track type", Description = "Defaults to subtitles.")]
    [StaticDataSource(typeof(TrackTypeDataHandler))]
    public string? Type { get; set; }

    [Display("Name", Description = "The name shown in the player. Defaults to the language code.")]
    public string? Name { get; set; }

    [Display("Make active", Description = "Enable this track in the player after upload. Defaults to false.")]
    public bool? Activate { get; set; }
}
