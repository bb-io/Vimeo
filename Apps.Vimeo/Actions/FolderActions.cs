using Apps.Vimeo.Invocables;
using Apps.Vimeo.Api;
using Apps.Vimeo.Models.Dtos;
using Apps.Vimeo.Models.Requests;
using Apps.Vimeo.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Vimeo.Actions;

[ActionList("Folders")]
public class FolderActions(InvocationContext context) : VimeoInvocable(context)
{
    [Action("Search folders", Description = "Get the folders accessible to the connected account.")]
    public async Task<SearchFoldersResponse> SearchFolders()
    {
        using var client = CreateClient();
        var response = new SearchFoldersResponse();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var page in client.Pages<FolderDto>(SearchRequest()))
        {
            response.Folders.AddRange(page.Where(item => seen.Add(item.Uri)).Select(ToResponse));
        }

        return response;
    }

    [Action("Create folder", Description = "Create a folder in the connected Vimeo account.")]
    public async Task<FolderResponse> CreateFolder([ActionParameter] CreateFolderRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new PluginMisconfigurationException("Enter a folder name.");

        using var client = CreateClient();
        var folder = await client.Read<FolderDto>(
            new RestRequest("me/projects", Method.Post).AddJsonBody(new { name = input.Name.Trim() }));
        return ToResponse(folder);
    }

    [Action("Add video to folder", Description = "Add an existing video to a Vimeo folder.")]
    public async Task AddVideoToFolder([ActionParameter] AddVideoToFolderRequest input)
    {
        var folderId = VimeoIds.Numeric(input.FolderId, "folder");
        var videoId = VimeoIds.Video(input.VideoId);
        using var client = CreateClient();
        await client.ExecuteWithErrorHandling(new RestRequest($"me/projects/{folderId}/videos/{videoId}", Method.Put));
    }

    internal static RestRequest SearchRequest() => new RestRequest("me/projects")
        .AddQueryParameter("per_page", 100)
        .AddQueryParameter("sort", "name")
        .AddQueryParameter("direction", "asc")
        .AddQueryParameter("fields", "data.uri,data.name,paging.next");

    private static FolderResponse ToResponse(FolderDto folder) => new()
    {
        FolderId = VimeoIds.FromUri(folder.Uri),
        Name = folder.Name
    };
}
