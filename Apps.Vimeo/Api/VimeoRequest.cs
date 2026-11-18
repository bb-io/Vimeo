using Apps.Vimeo.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;

namespace Apps.Vimeo.Api;

public class VimeoRequest : BlackBirdRestRequest
{
    public VimeoRequest(string resource, Method method, IEnumerable<AuthenticationCredentialsProvider> creds) : base(
        resource, method, creds)
    {
    }

    protected override void AddAuth(IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        this.AddHeader("Authorization", $"Bearer {creds.Get(CredsNames.AccessToken).Value}");
    }
}