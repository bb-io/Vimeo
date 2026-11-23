using Apps.Vimeo.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Vimeo.Invocables;

public class VimeoInvocable(InvocationContext context) : BaseInvocable(context)
{
    protected VimeoClient CreateClient() => new(InvocationContext.AuthenticationCredentialsProviders);
}
