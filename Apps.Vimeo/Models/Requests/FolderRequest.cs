using Apps.Vimeo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Vimeo.Models.Requests;

public class FolderRequest
{
    [Display("Folder ID", Description = "Select a folder or enter its numeric ID.")]
    [DataSource(typeof(FolderDataHandler))]
    public string FolderId { get; set; } = string.Empty;
}
