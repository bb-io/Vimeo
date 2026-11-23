using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class TrackDto
{
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("language")]
    public string? Language { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("link")]
    public string? Link { get; set; }
}
