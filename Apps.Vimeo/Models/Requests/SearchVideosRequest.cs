using Apps.Vimeo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Vimeo.Models.Requests;

public class SearchVideosRequest
{
    [Display("Search term", Description = "Search Vimeo for matching videos.")]
    public string? Query { get; set; }

    [Display("Folder ID", Description = "Select a folder or enter its numeric ID.")]
    [DataSource(typeof(FolderDataHandler))]
    public string? FolderId { get; set; }

    [Display("Maximum results", Description = "Defaults to 50. Enter a number from 1 to 500.")]
    public int? Limit { get; set; }
}
