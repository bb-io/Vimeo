using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class ProcessingDto
{
    [JsonProperty("status")]
    public string? Status { get; set; }
}
