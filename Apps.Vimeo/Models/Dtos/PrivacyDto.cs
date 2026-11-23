using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class PrivacyDto
{
    [JsonProperty("view")]
    public string? View { get; set; }

    [JsonProperty("download")]
    public bool? Download { get; set; }
}
