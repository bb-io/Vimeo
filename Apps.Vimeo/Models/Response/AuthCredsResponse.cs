using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Response;

public class AuthCredsResponse
{
    [JsonProperty("access_token")]
    public string? AccessToken { get; set; }

    [JsonProperty("user")]
    public UserResponse? User { get; set; }
}
