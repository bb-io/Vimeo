using Apps.Vimeo.Actions;
using Apps.Vimeo.Api;
using Apps.Vimeo.Models.Dtos;
using Apps.Vimeo.Models.Requests;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Tests.Vimeo;

[TestClass]
public class VimeoContractTests
{
    [DataTestMethod]
    [DataRow("123456")]
    [DataRow("/videos/123456")]
    [DataRow("https://vimeo.com/123456/hash")]
    [DataRow("https://player.vimeo.com/video/123456?h=hash")]
    public void VideoIdsAcceptCommonVimeoFormats(string input) =>
        Assert.AreEqual("123456", VimeoIds.Video(input));

    [TestMethod]
    public void RejectsArbitraryUrlsAsVideoIds() =>
        Assert.ThrowsException<PluginMisconfigurationException>(() => VimeoIds.Video("https://example.org/123456"));

    [TestMethod]
    public void UploadsDefaultToPrivateAndUpdatesDoNotChangePrivacyByDefault()
    {
        var upload = VideoLibraryActions.Metadata(new VideoMetadataRequest(), isUpload: true);
        var patch = VideoLibraryActions.Metadata(new VideoMetadataRequest { Title = "New title" });

        Assert.AreEqual("nobody", ((Dictionary<string, object>)upload["privacy"])["view"]);
        Assert.IsFalse(patch.ContainsKey("privacy"));
        Assert.AreEqual("New title", patch["name"]);
    }

    [TestMethod]
    public void UpdateCanDisableDownloadsAndClearDescription()
    {
        var patch = VideoLibraryActions.Metadata(new VideoMetadataRequest { AllowDownloads = false, Description = "" });

        Assert.AreEqual("", patch["description"]);
        Assert.AreEqual(false, ((Dictionary<string, object>)patch["privacy"])["download"]);
    }

    [TestMethod]
    public void MapsVimeoProcessingStatus()
    {
        var complete = VideoLibraryActions.ToResponse(new VideoDto { Uri = "/videos/12", Transcode = new() { Status = "complete" } });
        var processing = VideoLibraryActions.ToResponse(new VideoDto { Status = "available", Transcode = new() { Status = "in_progress" } });

        Assert.AreEqual("12", complete.VideoId);
        Assert.IsTrue(complete.IsReady);
        Assert.IsFalse(processing.IsReady);
    }

    [TestMethod]
    public void ReadsBothVimeoCollectionFormats()
    {
        var array = VimeoClient.ParsePage<FolderDto>(JToken.Parse("""[{"uri":"/me/projects/8","name":"Review"}]"""));
        var page = VimeoClient.ParsePage<FolderDto>(JToken.Parse(
            """{"data":[{"uri":"/me/projects/8","name":"Review"}],"paging":{"next":"/me/projects?page=2"}}"""));

        Assert.AreEqual(array.Data.Single().Name, page.Data.Single().Name);
        Assert.AreEqual("/me/projects?page=2", page.Paging?.Next);
    }

    [TestMethod]
    public void BuildsPathsForAccountSearchAndFolderSearch()
    {
        var global = VideoLibraryActions.SearchRequest("Product demo", null);
        var folder = VideoLibraryActions.SearchRequest("Product demo", "456");

        Assert.AreEqual("me/videos", global.Resource);
        Assert.AreEqual("Product demo", global.Parameters.Single(value => value.Name == "query").Value);
        Assert.AreEqual("me/projects/456/videos", folder.Resource);
        Assert.IsFalse(folder.Parameters.Any(value => value.Name == "query"));
    }

    [TestMethod]
    public void PagingKeepsTheTokenOnTheVimeoApiHost()
    {
        var page = VimeoClient.SafeNextPage("/me/videos?page=2");
        Assert.AreEqual("https://api.vimeo.com/me/videos?page=2", page.AbsoluteUri);
        Assert.ThrowsException<PluginApplicationException>(() => VimeoClient.SafeNextPage("//example.org/?page=2"));

        using var client = new VimeoClient([new("access_token", "test")]);
        Assert.AreEqual(page, client.BuildUri(new RestRequest(page)));
    }

    [TestMethod]
    public void SignedTransfersRejectInvalidDestinations()
    {
        Assert.AreEqual("files.tus.vimeo.com", VimeoTransfer.TransferUri("https://files.tus.vimeo.com/upload?token=fake").Host);
        Assert.ThrowsException<PluginApplicationException>(() => VimeoTransfer.TransferUri("https://vimeo.com.example.org/upload"));
        Assert.ThrowsException<PluginApplicationException>(() => VimeoTransfer.TransferUri("http://files.tus.vimeo.com/upload"));
    }
}
