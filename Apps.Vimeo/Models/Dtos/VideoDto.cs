using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class VideoDto
{
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("link")]
    public string? Link { get; set; }

    [JsonProperty("language")]
    public string? Language { get; set; }

    [JsonProperty("duration")]
    public int? Duration { get; set; }

    [JsonProperty("created_time")]
    public DateTime? CreatedAt { get; set; }

    [JsonProperty("modified_time")]
    public DateTime? UpdatedAt { get; set; }

    [JsonProperty("privacy")]
    public PrivacyDto? Privacy { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("upload")]
    public UploadDto? Upload { get; set; }

    [JsonProperty("transcode")]
    public ProcessingDto? Transcode { get; set; }
}
