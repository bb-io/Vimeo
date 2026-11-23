using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Vimeo.Api;

internal static class VimeoIds
{
    internal static string Video(string? value)
    {
        var input = value?.Trim() ?? string.Empty;
        if (input.StartsWith("/videos/", StringComparison.Ordinal))
            input = FromUri(input);
        else if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
                 uri.Scheme == Uri.UriSchemeHttps &&
                 uri.Host is "vimeo.com" or "www.vimeo.com" or "player.vimeo.com")
        {
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            input = parts switch
            {
                [var id] => id,
                ["video", var id] => id,
                [var id, _] when uri.Host != "player.vimeo.com" => id,
                _ => string.Empty
            };
        }

        return Numeric(input, "video");
    }

    internal static string Numeric(string? value, string resource)
    {
        var id = value?.Trim() ?? string.Empty;
        if (id.Length is < 1 or > 24 || !id.All(char.IsAsciiDigit))
            throw new PluginMisconfigurationException($"Enter a valid Vimeo {resource} ID.");

        return id;
    }

    internal static string FromUri(string? value) => value?.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
}
