using System.Web;
using Apps.Vimeo.Actions;
using Apps.Vimeo.Connections;
using Apps.Vimeo.Connections.OAuth;
using Apps.Vimeo.DataSourceHandlers.Static;
using Apps.Vimeo.Models.Request.Video;
using Apps.Vimeo.Models.Response;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using RestSharp;

namespace Tests.Vimeo;

[TestClass]
public class LegacyCompatibilityTests
{
    [TestMethod]
    public void LegacyUploadKeepsItsIdentityAndFields()
    {
        var method = typeof(VideoActions).GetMethod(nameof(VideoActions.UploadVideo))!;
        Assert.AreEqual("Apps.Vimeo", method.DeclaringType!.Assembly.GetName().Name);
        Assert.AreEqual("Apps.Vimeo.Actions.VideoActions", method.DeclaringType.FullName);
        Assert.AreEqual(typeof(Task), method.ReturnType);
        Assert.AreEqual("Upload video", method.GetCustomAttributes(typeof(ActionAttribute), false)
            .Cast<ActionAttribute>().Single().Name);
        Assert.AreEqual(typeof(UploadVideoRequest), method.GetParameters().Single().ParameterType);
        CollectionAssert.AreEquivalent(
            new[] { "File", "Name", "Description", "PrivacyView" },
            typeof(UploadVideoRequest).GetProperties().Select(property => property.Name).ToArray());

        var values = new VideoPrivacyViewHandler().GetData().Select(item => item.Value).ToArray();
        CollectionAssert.AreEqual(new[] { "anybody", "nobody", "unlisted" }, values);
    }

    [TestMethod]
    public void LegacyUploadUsesTheOriginalPullRequest()
    {
        var request = VideoActions.BuildUploadRequest(new()
        {
            File = CreateLegacyFile(),
            Description = "Description",
            PrivacyView = "unlisted"
        }, "42");

        Assert.AreEqual("users/42/videos", request.Resource);
        Assert.AreEqual(Method.Post, request.Method);
        var parameters = request.Parameters.ToDictionary(parameter => parameter.Name!, parameter => parameter.Value);
        Assert.AreEqual("https://example.com/movie.mp4", parameters["upload.link"]);
        Assert.AreEqual("pull", parameters["upload.approach"]);
        Assert.AreEqual("movie.mp4", parameters["name"]);
        Assert.AreEqual("Description", parameters["description"]);
        Assert.AreEqual("unlisted", parameters["privacy.view"]);
    }

    [TestMethod]
    public void LegacyUploadWithTokenOnlyTargetsTheConnectedAccount()
    {
        var request = VideoActions.BuildUploadRequest(new()
        {
            File = CreateLegacyFile(),
            Name = "Custom title"
        }, null);

        Assert.AreEqual("me/videos", request.Resource);
        Assert.AreEqual("Custom title", request.Parameters.Single(value => value.Name == "name").Value);
        Assert.IsFalse(request.Parameters.Any(value => value.Name == "privacy.view"));
    }

    [TestMethod]
    public void OAuthStillSavesTheLegacyTokenAndUserId()
    {
        var definition = new ConnectionDefinition();
        var oauth = definition.ConnectionPropertyGroups.Single(group => group.Name == "OAuth");
        Assert.AreEqual(ConnectionAuthenticationType.OAuth2, oauth.AuthenticationType);
        Assert.IsFalse(oauth.ConnectionProperties.Any());

        var response = JsonConvert.DeserializeObject<AuthCredsResponse>(
            """{"access_token":"fixture-token","user":{"uri":"/users/42"}}""")!;
        var credentials = OAuthTokenService.CreateCredentials(response);
        var providers = definition.CreateAuthorizationCredentialsProviders(credentials).ToDictionary(item => item.KeyName);

        Assert.AreEqual("fixture-token", providers["access_token"].Value);
        Assert.AreEqual("42", providers["user_id"].Value);
    }

    [TestMethod]
    public void OAuthAuthorizationKeepsTheExistingBridgeCallback()
    {
        const string clientVariable = "BLACKBIRD_VIMEO_CLIENT_ID";
        var previousValue = Environment.GetEnvironmentVariable(clientVariable);
        try
        {
            Environment.SetEnvironmentVariable(clientVariable, "fixture-client-id");
            var context = new InvocationContext
            {
                UriInfo = new UriInfo
                {
                    BridgeServiceUrl = new Uri("https://bridge.example.com/"),
                    AuthorizationCodeRedirectUri = new Uri("https://blackbird.example.com/oauth/callback")
                }
            };
            var url = new Uri(new OAuthAuthorizeService(context).GetAuthorizationUrl(new() { ["state"] = "fixture-state" }));
            var query = HttpUtility.ParseQueryString(url.Query);

            Assert.AreEqual("https://bridge.example.com/oauth", url.GetLeftPart(UriPartial.Path));
            Assert.AreEqual("https://bridge.example.com/AuthorizationCode", query["redirect_uri"]);
            Assert.AreEqual("https://blackbird.example.com/oauth/callback", query["actual_redirect_uri"]);
            Assert.AreEqual("https://api.vimeo.com/oauth/authorize", query["authorization_url"]);
            Assert.AreEqual("fixture-client-id", query["client_id"]);
            Assert.AreEqual("fixture-state", query["state"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(clientVariable, previousValue);
        }
    }

    [TestMethod]
    public void PollingAndFolderActionsRequestTheNewScopeWithoutDroppingOldScopes()
    {
        CollectionAssert.IsSubsetOf(
            new[] { "upload", "video_files", "create", "edit", "private", "interact" },
            Apps.Vimeo.ApplicationConstants.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static FileReference CreateLegacyFile()
    {
        var reference = new FileReference { Name = "movie.mp4", ContentType = "video/mp4" };
        typeof(FileReference).GetProperty(nameof(FileReference.Url))!.SetValue(reference, "https://example.com/movie.mp4");
        return reference;
    }
}
