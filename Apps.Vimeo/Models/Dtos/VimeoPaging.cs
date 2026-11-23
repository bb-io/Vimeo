using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class VimeoPaging
{
    [JsonProperty("next")]
    public string? Next { get; set; }
}
