using Apps.Vimeo.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Vimeo.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups =>
    [
        new()
        {
            Name = "OAuth",
            AuthenticationType = ConnectionAuthenticationType.OAuth2,
            ConnectionProperties = []
        },
        new()
        {
            Name = "Personal access token",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties =
            [
                new(CredsNames.AccessToken)
                {
                    DisplayName = "Personal access token",
                    Description = "Generate an Authenticated (you) token in Vimeo's developer app settings.",
                    Sensitive = true
                }
            ]
        }
    ];

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values) =>
        values.Select(value => new AuthenticationCredentialsProvider(value.Key, value.Value)).ToList();
}
