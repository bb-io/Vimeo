using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class UploadDto : ProcessingDto
{
    [JsonProperty("upload_link")]
    public string? UploadLink { get; set; }
}
