using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute;
using SenseNet.Testing;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class UploadTests : TestBase
{
    private readonly CancellationToken _cancel = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

    [TestMethod]
    public async Task Upload_Path_Stream()
    {
        var restCaller = Substitute.For<IRestCaller>();
        var multipartData = new List<string>();
        restCaller
            .ProcessWebResponseAsync(Arg.Any<string>(), Arg.Any<HttpMethod>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(),
                Arg.Do<HttpContent>(arg => multipartData.Add(arg.ReadAsStringAsync(_cancel).Result)),
                Arg.Do<Func<HttpResponseMessage, CancellationToken, Task>>(arg =>
                {
                    var msg = new HttpResponseMessage() { Content = new StringContent(@"{
    ""Url"":""/Root/MyContent/File1.txt"",
    ""Thumbnail_url"":""/Root/MyContent/File1.txt"",
    ""Name"":""File1.txt"",
    ""Length"":999,
    ""Type"":""File"",
    ""Id"":99999
}") };
                    arg(msg, _cancel);
                }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        var fileContent = "111111111122222222223333333333444";

        UploadResult? result;
        // ACT
        await using (var uploadStream = Tools.GenerateStreamFromString(fileContent))
        {
            var request = new UploadRequest { ParentPath = "/Root/MyContent", ContentName = "File1.txt" };
            result = await repository.UploadAsync(request, uploadStream, _cancel).ConfigureAwait(false);
        }

        // ASSERT
        Assert.IsNotNull(result);
        Assert.AreEqual("/Root/MyContent/File1.txt", result.Url);
        Assert.AreEqual("File1.txt", result.Name);
        Assert.AreEqual("File", result.Type);
        Assert.AreEqual(99999, result.Id);
        Assert.AreEqual(999L, result.Length);
        // Test restCaller calls
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("ProcessWebResponseAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        // string relativeUrl
        Assert.IsTrue(arguments[0]?.ToString()?.Contains("/Root('MyContent')/Upload"));
        // HttpMethod method
        Assert.AreEqual(HttpMethod.Post, arguments[1]);
        // Dictionary<string, IEnumerable<string>> additionalHeaders
        Assert.AreEqual(0, (arguments[2] as Dictionary<string, IEnumerable<string>>)?.Count ?? 0);
        // HttpContent postData
        var postData = arguments[3] as StringContent;
        Assert.IsNotNull(postData);
        var stringContent = await postData.ReadAsStringAsync(_cancel);
        // see UseChunk
        Assert.AreEqual("FileName=File1.txt&ContentType=File&PropertyName=Binary&" +
                        "UseChunk=False&" +
                        "Overwrite=True&FileLength=33", stringContent);

        // Chunks
        void AssertChunkRequest(string multipartData, string expectedChunk)
        {
            var lines = multipartData.Split('\n').Select(x => x.Trim()).ToArray();
            var actualChunk = lines[^3];
            Assert.AreEqual(actualChunk, expectedChunk);
        }
        Assert.AreEqual(2, multipartData.Count);
        AssertChunkRequest(multipartData[1], "111111111122222222223333333333444");
    }

    [TestMethod]
    public async Task Upload_Path_Stream_Chunk()
    {
        var restCaller = Substitute.For<IRestCaller>();
        var multipartData = new List<string>();
        restCaller
            .ProcessWebResponseAsync(Arg.Any<string>(), Arg.Any<HttpMethod>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(),
                Arg.Do<HttpContent>(arg => multipartData.Add(arg.ReadAsStringAsync(_cancel).Result)),
                Arg.Do<Func<HttpResponseMessage, CancellationToken, Task>>(arg =>
                {
                    var msg = multipartData.Count == 1
                        ? new HttpResponseMessage()
                        {
                            Content = new StringContent(@"<<chunkToken>>")
                        }
                        : new HttpResponseMessage()
                        {
                            Content = new StringContent(@"{
    ""Url"":""/Root/MyContent/File1.txt"",
    ""Thumbnail_url"":""/Root/MyContent/File1.txt"",
    ""Name"":""File1.txt"",
    ""Length"":999,
    ""Type"":""File"",
    ""Id"":99999
}")
                        };
                    arg(msg, _cancel);
                }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            ;

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        var fileContent = "111111111122222222223333333333444";

        UploadResult? result;
        var progressValues = new List<int>();
        using (new Swindler<int>(
                   hack: 10,
                   getter: () => ClientContext.Current.ChunkSizeInBytes,
                   setter: value => ClientContext.Current.ChunkSizeInBytes = value))
        {
            // ACT
            await using (var uploadStream = Tools.GenerateStreamFromString(fileContent))
            {
                var request = new UploadRequest { ParentPath = "/Root/MyContent", ContentName = "File1.txt" };
                result = await repository.UploadAsync(request, uploadStream, progress => { progressValues.Add(progress); }, _cancel).ConfigureAwait(false);
            }
        }

        // ASSERT
        // result
        Assert.IsNotNull(result);
        Assert.AreEqual("/Root/MyContent/File1.txt", result.Url);
        Assert.AreEqual("File1.txt", result.Name);
        Assert.AreEqual("File", result.Type);
        Assert.AreEqual(99999, result.Id);
        Assert.AreEqual(999L, result.Length);
        // Test restCaller calls
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(6, calls.Length);
        Assert.AreEqual("ProcessWebResponseAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        // string relativeUrl
        Assert.IsTrue(arguments[0]?.ToString()?.Contains("/Root('MyContent')/Upload"));
        // HttpMethod method
        Assert.AreEqual(HttpMethod.Post, arguments[1]);
        // Dictionary<string, IEnumerable<string>> additionalHeaders
        Assert.AreEqual(0, (arguments[2] as Dictionary<string, IEnumerable<string>>)?.Count ?? 0);
        // HttpContent postData
        var postData = arguments[3] as StringContent;
        Assert.IsNotNull(postData);
        var stringContent = await postData.ReadAsStringAsync(_cancel);
        // see UseChunk
        Assert.AreEqual("FileName=File1.txt&ContentType=File&PropertyName=Binary&" +
                        "UseChunk=True&" +
                        "Overwrite=True&FileLength=33", stringContent);

        // Chunks
        void AssertChunkRequest(string multipartData, string expectedChunk)
        {
            var lines = multipartData.Split('\n').Select(x => x.Trim()).ToArray();
            var chunkToken = lines[^7];
            Assert.AreEqual("<<chunkToken>>", chunkToken);
            var actualChunk = lines[^3];
            Assert.AreEqual(actualChunk, expectedChunk);
        }
        Assert.AreEqual(5, multipartData.Count);
        AssertChunkRequest(multipartData[1], "1111111111");
        AssertChunkRequest(multipartData[2], "2222222222");
        AssertChunkRequest(multipartData[3], "3333333333");
        AssertChunkRequest(multipartData[4], "444");

        // Progress
        var actualProgress = string.Join(",", progressValues.Select(x => x.ToString()));
        Assert.AreEqual("10,20,30,33", actualProgress);
    }

    [TestMethod]
    public async Task Upload_Path_Text()
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .ProcessWebResponseAsync(Arg.Any<string>(), Arg.Any<HttpMethod>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(),
                Arg.Any<HttpContent>(),
                Arg.Do<Func<HttpResponseMessage, CancellationToken, Task>>(arg =>
                {
                    var msg = new HttpResponseMessage() {Content = new StringContent(@"{
    ""Url"":""/Root/MyContent/File1.txt"",
    ""Thumbnail_url"":""/Root/MyContent/File1.txt"",
    ""Name"":""File1.txt"",
    ""Length"":999,
    ""Type"":""File"",
    ""Id"":99999
}") };
                    arg(msg, _cancel);
                }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);
        var fileText = "File text file text";

        // ACT
        var request = new UploadRequest {ParentPath = "/Root/MyContent", ContentName = "File1.txt"};
        var result = await repository.UploadAsync(request, fileText, _cancel).ConfigureAwait(false);

        // ASSERT
        Assert.IsNotNull(result);
        Assert.AreEqual("/Root/MyContent/File1.txt", result.Url);
        Assert.AreEqual("File1.txt", result.Name);
        Assert.AreEqual("File", result.Type);
        Assert.AreEqual(99999, result.Id);
        Assert.AreEqual(999L, result.Length);
        // Test restCaller calls
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(2, calls.Length);
        Assert.AreEqual("ProcessWebResponseAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        // string relativeUrl
        Assert.IsTrue(arguments[0]?.ToString()?.Contains("/Root('MyContent')/Upload"));
        // HttpMethod method
        Assert.AreEqual(HttpMethod.Post, arguments[1]);
        // Dictionary<string, IEnumerable<string>> additionalHeaders
        Assert.AreEqual(0, (arguments[2] as Dictionary<string, IEnumerable<string>>)?.Count ?? 0);
        // HttpContent postData
        var postData = arguments[3] as StringContent;
        Assert.IsNotNull(postData);
        var stringContent = await postData.ReadAsStringAsync(_cancel);
        var json = stringContent.Substring("models=[".Length).TrimEnd(']');
        JObject model = JsonHelper.Deserialize(json);
        // models=[{
        //   "FileName":"File1.txt",
        //   "ContentType":"File",
        //   "PropertyName":"Binary",
        //   "UseChunk":false,
        //   "Overwrite":true,
        //   "FileLength":19,
        //   "FileText":"File text file text"
        // }] 
        Assert.AreEqual("File1.txt", model["FileName"].Value<string>());
        Assert.AreEqual("File", model["ContentType"].Value<string>());
        Assert.AreEqual("Binary", model["PropertyName"].Value<string>());
        Assert.AreEqual(false, model["UseChunk"].Value<bool>());
        Assert.AreEqual(true, model["Overwrite"].Value<bool>());
        Assert.AreEqual(fileText.Length, model["FileLength"].Value<int>());
        Assert.AreEqual(fileText, model["FileText"].Value<string>());
    }

}