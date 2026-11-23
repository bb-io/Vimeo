using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class LanguageDto
{
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}
