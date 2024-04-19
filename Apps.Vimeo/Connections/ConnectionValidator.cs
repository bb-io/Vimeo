using Apps.Vimeo.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.Vimeo.Connections;

public class ConnectionValidator: IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        var client = new VimeoClient();
        var request = new VimeoRequest("me", Method.Get, authenticationCredentialsProviders);

        await client.ExecuteWithErrorHandling(request);
        
        return new()
        {
            IsValid = true
        };
    }
}