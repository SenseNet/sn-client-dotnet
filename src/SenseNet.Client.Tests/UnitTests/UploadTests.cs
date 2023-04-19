using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class UploadTests : TestBase
{
    [TestMethod]
    public async Task Upload_Path_Stream()
    {
        var stream = Tools.GenerateStreamFromString("File Content");
        var content = await Content.UploadAsync("/Root/Content", "TestFile1", stream)
            .ConfigureAwait(false);
    }
    [TestMethod]
    public async Task Upload_Path_Text()
    {
        var content = await Content.UploadTextAsync("/Root/Content", "TestFile1", "File Content", CancellationToken.None)
            .ConfigureAwait(false);
    }
}