using System.Text;
using Apps.Vimeo.Constants;
using Apps.Vimeo.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Apps.Vimeo.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication.OAuth2;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;

namespace Apps.Vimeo.Connections.OAuth;

public class OAuthTokenService : BaseInvocable, IOAuth2TokenService
{
    public OAuthTokenService(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> RequestToken(string state, string code, Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var request = new RestRequest("oauth/access_token", Method.Post)
            .WithJsonBody(new
            {
                grant_type = "authorization_code",
                code,
                redirect_uri = $"{InvocationContext.UriInfo.BridgeServiceUrl.ToString().TrimEnd('/')}/AuthorizationCode"
            });

        request.AddHeader("Authorization",
            $"Basic {Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(ApplicationConstants.ClientId + ":" + ApplicationConstants.ClientSecret))}");

        using var client = new VimeoClient();
        AuthCredsResponse response;
        try
        {
            response = await client.Read<AuthCredsResponse>(request, cancellationToken);
        }
        catch (PluginMisconfigurationException)
        {
            throw new PluginMisconfigurationException(
                "Vimeo could not authorize the connection. Check the developer app credentials and OAuth redirect registration, then retry sign-in.");
        }

        return CreateCredentials(response);
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

    internal static Dictionary<string, string> CreateCredentials(AuthCredsResponse response)
    {
        var userId = VimeoIds.FromUri(response.User?.Uri);
        if (string.IsNullOrWhiteSpace(response.AccessToken) || string.IsNullOrWhiteSpace(userId))
            throw new PluginApplicationException("Vimeo did not return the access token and account ID needed to connect.");

        return new()
        {
            [CredsNames.AccessToken] = response.AccessToken,
            [CredsNames.UserId] = userId
        };
    }
}