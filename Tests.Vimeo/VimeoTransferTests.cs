using Apps.Vimeo.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Vimeo;

[TestClass]
public class VimeoTransferTests
{
    [TestMethod]
    public async Task VideoTransferResumesAtTheServerOffsetAfterAConnectionDrop()
    {
        using var handler = new TusTestHandler();
        using var client = new HttpClient(handler);
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);

        await VimeoTransfer.UploadVideo("https://files.tus.vimeo.com/test", stream, 5, client);

        CollectionAssert.AreEqual(new[] { "PATCH", "HEAD", "PATCH" }, handler.Methods);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, handler.FirstBody);
        CollectionAssert.AreEqual(new byte[] { 3, 4, 5 }, handler.ResumedBody);
    }
}
