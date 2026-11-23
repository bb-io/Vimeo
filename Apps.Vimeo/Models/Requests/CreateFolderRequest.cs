using Blackbird.Applications.Sdk.Common;

namespace Apps.Vimeo.Models.Requests;

public class CreateFolderRequest
{
    [Display("Name", Description = "The folder name shown in Vimeo.")]
    public string Name { get; set; } = string.Empty;
}
