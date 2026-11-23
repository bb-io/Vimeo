using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Vimeo;

internal class TusTestHandler : HttpMessageHandler
{
    internal List<string> Methods { get; } = [];

    internal byte[]? FirstBody { get; private set; }

    internal byte[]? ResumedBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Methods.Add(request.Method.Method);
        Assert.AreEqual("1.0.0", request.Headers.GetValues("Tus-Resumable").Single());
        Assert.IsNull(request.Headers.Authorization);
        if (request.Method == HttpMethod.Head)
            return Respond(HttpStatusCode.OK, "2");

        var offset = request.Headers.GetValues("Upload-Offset").Single();
        Assert.AreEqual("application/offset+octet-stream", request.Content!.Headers.ContentType!.MediaType);
        var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        if (offset == "0")
        {
            FirstBody = body;
            throw new HttpRequestException("Simulated connection drop after two bytes");
        }

        Assert.AreEqual("2", offset);
        ResumedBody = body;
        return Respond(HttpStatusCode.NoContent, "5");
    }

    private static HttpResponseMessage Respond(HttpStatusCode status, string offset)
    {
        var response = new HttpResponseMessage(status);
        response.Headers.Add("Upload-Offset", offset);
        return response;
    }
}
