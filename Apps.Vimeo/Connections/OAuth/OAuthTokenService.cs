using System.Text;
using Apps.Vimeo.Constants;
using Apps.Vimeo.Invocables;
using Apps.Vimeo.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication.OAuth2;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;

namespace Apps.Vimeo.Connections.OAuth;

public class OAuthTokenService : VimeoInvocable, IOAuth2TokenService
{
    public OAuthTokenService(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> RequestToken(string state, string code, Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var request = new RestRequest("https://api.vimeo.com/oauth/access_token", Method.Post)
            .WithJsonBody(new
            {
                grant_type = "authorization_code",
                code,
                redirect_uri = $"{InvocationContext.UriInfo.BridgeServiceUrl.ToString().TrimEnd('/')}/AuthorizationCode"
            });

        request.AddHeader("Authorization",
            $"Basic {Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(ApplicationConstants.ClientId + ":" + ApplicationConstants.ClientSecret))}");

        var response = await Client.ExecuteWithErrorHandling<AuthCredsResponse>(request);

        return new()
        {
            [CredsNames.AccessToken] = response.AccessToken,
            [CredsNames.UserId] = response.User.Uri.Split("/").Last(),
        };
    }

    public Task RevokeToken(Dictionary<string, string> values)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, string>> RefreshToken(Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(values);
    }

    public bool IsRefreshToken(Dictionary<string, string> values) => false;
}