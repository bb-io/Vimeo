using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Vimeo;

public static class ApplicationConstants
{
    public const string Scope = "public private upload video_files create edit interact";

    internal static string ClientId => GetOAuthSetting("BLACKBIRD_VIMEO_CLIENT_ID");

    internal static string ClientSecret => GetOAuthSetting("BLACKBIRD_VIMEO_CLIENT_SECRET");

    private static string GetOAuthSetting(string name)
    {
        var value = Environment.GetEnvironmentVariable(name)?.Trim();
        if (string.IsNullOrEmpty(value))
            throw new PluginMisconfigurationException(
                "Vimeo OAuth is not configured on this Blackbird host. Contact your Blackbird administrator or connect with a personal access token.");

        return value;
    }
}
