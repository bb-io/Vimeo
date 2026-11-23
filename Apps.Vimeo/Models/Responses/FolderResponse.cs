using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Responses;

public class FolderResponse
{
    [Display("Folder ID")]
    public string FolderId { get; set; } = string.Empty;

    [Display("Name")]
    public string Name { get; set; } = string.Empty;
}
