using Apps.Vimeo.Api;
using Apps.Vimeo.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using RestSharp;

namespace Apps.Vimeo.Connections;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new VimeoClient(authenticationCredentialsProviders);
            await client.Read<AccountDto>(new RestRequest("me").AddQueryParameter("fields", "uri"), cancellationToken);
            return new() { IsValid = true, Message = "Connected to Vimeo." };
        }
        catch (Exception exception)
        {
            return new()
            {
                IsValid = false,
                Message = exception is PluginMisconfigurationException or PluginApplicationException
                    ? exception.Message
                    : "Could not validate the Vimeo connection. Check your Vimeo account access and try again."
            };
        }
    }
}
