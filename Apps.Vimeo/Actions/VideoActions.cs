using Apps.Vimeo.Api;
using Apps.Vimeo.Constants;
using Apps.Vimeo.Invocables;
using Apps.Vimeo.Models.Request.Video;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using RestSharp;

namespace Apps.Vimeo.Actions;

[ActionList]
public class VideoActions : VimeoInvocable
{
    private readonly IFileManagementClient _fileManagementClient;

    public VideoActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : base(
        invocationContext)
    {
        _fileManagementClient = fileManagementClient;
    }

    [Action("Upload video", Description = "Upload a new video")]
    public Task UploadVideo([ActionParameter] UploadVideoRequest input)
    {
        var parameters = new List<KeyValuePair<string, string?>>()
        {
            new("upload.link", input.File.Url ?? throw new ArgumentException("File url is empty")),
            new("name", input.Name ?? input.File.Name),
            new("description", input.Description),
            new("upload.approach", "pull"),
            new("privacy.view", input.PrivacyView)
        }.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToList();

        var endpoint = $"users/{Creds.Get(CredsNames.UserId).Value}/videos";
        var request = new VimeoRequest(endpoint, Method.Post, Creds);
        parameters.ForEach(x => request.AddParameter(x.Key, x.Value));

        return Client.ExecuteWithErrorHandling(request);
    }
}