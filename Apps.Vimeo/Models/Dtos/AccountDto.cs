using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class AccountDto
{
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;
}
