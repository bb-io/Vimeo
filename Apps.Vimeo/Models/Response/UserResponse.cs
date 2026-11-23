using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Response;

public class UserResponse
{
    [JsonProperty("uri")]
    public string? Uri { get; set; }
}
