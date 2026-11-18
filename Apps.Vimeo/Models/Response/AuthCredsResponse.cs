namespace Apps.Vimeo.Models.Response;

public class AuthCredsResponse
{
    public string AccessToken { get; set; }
    
    public UserResponse User { get; set; }
}