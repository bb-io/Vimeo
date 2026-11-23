using Apps.Vimeo.Api;
using Apps.Vimeo.Constants;
using Apps.Vimeo.Invocables;
using Apps.Vimeo.Models.Request.Video;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Vimeo.Actions;

[ActionList]
public class VideoActions(InvocationContext invocationContext)
    : VimeoInvocable(invocationContext)
{
    [Action("Upload video", Description = "Upload a video file to the connected account with optional title, description, and viewing privacy settings.")]
    public async Task UploadVideo([ActionParameter] UploadVideoRequest input)
    {
        var userId = InvocationContext.AuthenticationCredentialsProviders
            .FirstOrDefault(credential => credential.KeyName == CredsNames.UserId)?.Value;
        var request = BuildUploadRequest(input, userId);

        using var client = CreateClient();
        await client.ExecuteWithErrorHandling(request);
    }

    internal static RestRequest BuildUploadRequest(UploadVideoRequest input, string? userId)
    {
        if (string.IsNullOrWhiteSpace(input.File?.Url))
            throw new PluginMisconfigurationException("Map a video file that Vimeo can retrieve from its download URL.");

        var endpoint = string.IsNullOrEmpty(userId)
            ? "me/videos"
            : $"users/{VimeoIds.Numeric(userId, "account")}/videos";
        var request = new RestRequest(endpoint, Method.Post);
        var parameters = new Dictionary<string, string?>
        {
            ["upload.link"] = input.File.Url,
            ["name"] = input.Name ?? input.File.Name,
            ["description"] = input.Description,
            ["upload.approach"] = "pull",
            ["privacy.view"] = input.PrivacyView
        };

        foreach (var parameter in parameters.Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value)))
            request.AddParameter(parameter.Key, parameter.Value!);

        return request;
    }
}
