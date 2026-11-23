# Vimeo for Blackbird

<!-- begin docs -->

Use the Vimeo app to upload and organize videos and review multilingual subtitles and captions.

## Upgrade the existing app

Vimeo 1.1.0 is based on the `main` branch of the [Blackbird Vimeo repository](https://github.com/bb-io/Vimeo). It uses the existing Vimeo app identity and includes the original **Upload video** action with the same field mappings. Publish it through the existing Vimeo catalog release pipeline. For an organization that previously installed the app as a custom app, open its existing Vimeo entry, choose **New version → Update App**, and upload `Vimeo-1.1.0-app.zip`. [Blackbird deployment instructions](https://docs.blackbird.io/sdk/deploying/)

## Connect to Vimeo

### OAuth

Use your existing Vimeo OAuth connection in Blackbird. If you use **Add video to folder**, sign in again to grant the new `interact` scope. A fresh OAuth sign-in on a configured Blackbird deployment follows the same steps as before. Choose **OAuth**, sign in to Vimeo, and authorize access.

For deployment administrators, configure `BLACKBIRD_VIMEO_CLIENT_ID` and `BLACKBIRD_VIMEO_CLIENT_SECRET` in the environment of the Blackbird host running the Vimeo app's OAuth services. Use the existing Vimeo developer app registration and the same Blackbird bridge callback ending in `/AuthorizationCode`. The release package contains no OAuth credential values. Existing OAuth access tokens are still read from the saved connection.

### Personal access token

1. Create or open a Vimeo developer app in [Vimeo My Apps](https://developer.vimeo.com/apps).
2. Generate a personal access token under **Authenticated (you)**.
3. Select the scopes needed for the workflow. For subtitle and caption access, generate the token as the account owner of the video.
4. In the Vimeo app in Blackbird, choose **Personal access token**, paste the token, and save it.

| Workflow | Vimeo scopes |
| --- | --- |
| Search and read videos, folders or text tracks, or monitor new videos | `public` and `private` |
| Upload or import videos | `upload` and `edit`, in addition to read scopes |
| Update videos or text track settings | `edit`, in addition to read scopes |
| Upload WebVTT subtitles or captions | `upload` and `edit`, in addition to read scopes |
| Create folders | `create`, in addition to read scopes |
| Add videos to folders | `interact`, in addition to read scopes |

Vimeo may require upload approval for your developer app. Vimeo privacy options and account limits depend on your plan. Each action requires its own scopes even when the connection check succeeds. [Vimeo upload access](https://help.vimeo.com/hc/en-us/articles/12427803706001-How-to-request-API-upload-access)

## Actions

| Action | What it does |
| --- | --- |
| **Search videos** | Find videos in the connected account, optionally by search term or folder. Returns up to 50 results by default and accepts a maximum of 500. In a folder, the search term matches titles and descriptions. |
| **Get video** | Return video metadata, privacy, upload status, and transcoding status. Use **Ready to play** to route subsequent workflow steps. |
| **Update video** | Change the title, description, visibility, or whether viewers can download the video. Omitted fields keep their current values. |
| **Upload video** | Send the file’s download URL to Vimeo. The existing inputs are **File**, **Name**, **Description**, and **PrivacyView**. As before, Vimeo uses the source filename when you omit **Name**. This action has no outputs. |
| **Upload video file** | Send a file directly to Vimeo from an earlier Blackbird step and return the Vimeo video ID. The file must have a known size. If you omit the title, the source filename is used. |
| **Import video from URL** | Give Vimeo a direct HTTPS file URL and the file size in bytes. Vimeo fetches the file asynchronously. Use a link accessible without cookies or authorization headers. A presigned link should stay valid for at least six hours. |
| **Search folders** | Find folders returned by the connected account’s Vimeo folder listing. |
| **Create folder** | Create a new folder with the supplied name. |
| **Add video to folder** | Add an existing video to the selected Vimeo folder. |
| **Search text tracks** | List a video’s text tracks with their IDs, languages, types, and active status. |
| **Download text track** | Download a selected subtitle or caption as a WebVTT file available to later steps. |
| **Upload text track** | Add a UTF-8 WebVTT subtitle or caption file up to 10 MB. The track is a subtitle by default. Set **Make active** to enable it in the player after upload. |
| **Update text track** | Rename a track, activate it after review, or deactivate it. |

Video inputs accept a selected video, a numeric ID, or a standard Vimeo video URL. The connected account must be able to access the video. To choose a text track manually, select the video first. IDs returned by previous steps can also be mapped directly.

**Upload video file** and **Import video from URL** default to **Private**. **Upload video** continues to use Vimeo’s default visibility when you omit **PrivacyView**. Transcoding runs asynchronously. Use **Get video** to check for completion before steps that need a playable video. **Upload video file** can resume individual chunks during the same Flight. If the Flight fails and reports a video or track ID, inspect the item before retrying to avoid creating a duplicate. [Vimeo upload documentation](https://developer.vimeo.com/api/upload/videos)

## Trigger

**On new video added** polls for video records created in the connected account. Its first run establishes a baseline. After that, it starts one Flight per new video. A Flight may start while the video is still uploading or transcoding. [Vimeo transcoding status](https://help.vimeo.com/hc/en-us/articles/12427776744593-Get-video-transcode-status-from-the-API)

## Example Bird

**Review a subtitle translation before publishing it**

1. Start with **On new video added**. If the workflow needs a playable copy, use **Get video** and check **Ready to play**.
2. Find the source subtitle with **Search text tracks**, then pass its ID to **Download text track**.
3. Translate or review the WebVTT in your preferred tool, keeping the cue timings. Pass the resulting file to **Upload text track**, set the target language, and leave **Make active** off.
4. Route the upload through a Blackbird Checkpoint. On approval, pass the returned text track ID to **Update text track** and enable **Active**.

## Build and verify

Use the .NET 8 SDK from the source root.

```sh
dotnet test Apps.Vimeo.sln --configuration Release
dotnet publish Apps.Vimeo/Apps.Vimeo.csproj --configuration Release --output publish
```

Build a ZIP with the contents of `publish` at the archive root. The included automated tests cover legacy action and OAuth connection compatibility, polling state, request contracts, response parsing, and resuming an interrupted upload. For a live smoke test after deployment, run an existing Bird with **Upload video** using its saved OAuth connection. Then sign in to a new OAuth connection and confirm folder assignment on a Vimeo account with a disposable video. Search for it, get its processing status, then upload an inactive test WebVTT track, download and compare its content, and activate it. Use a short private test upload to verify the video transfer. [Blackbird deployment instructions](https://docs.blackbird.io/sdk/deploying/)

## API references

- [Getting Started with the Vimeo API](https://developer.vimeo.com/api/guides/start)
- [Vimeo API Reference](https://developer.vimeo.com/api/reference)
- [Working with Text Track Uploads](https://developer.vimeo.com/api/upload/texttracks)

<!-- end docs -->
