using Apps.Vimeo.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Vimeo.Invocables;

public class VimeoInvocable : BaseInvocable
{
    protected AuthenticationCredentialsProvider[] Creds =>
        InvocationContext.AuthenticationCredentialsProviders.ToArray();

    protected VimeoClient Client { get; }
    public VimeoInvocable(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = new();
    }
}