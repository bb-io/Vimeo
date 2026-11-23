using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Responses;

public class SearchFoldersResponse
{
    [Display("Folders")]
    public List<FolderResponse> Folders { get; set; } = [];
}
