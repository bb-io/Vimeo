using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class FolderDto
{
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}
