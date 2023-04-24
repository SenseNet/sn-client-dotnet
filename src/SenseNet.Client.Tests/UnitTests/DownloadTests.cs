using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Channels;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class DownloadTests : TestBase
{
    private readonly CancellationToken _cancel = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

    [TestMethod]
    public async Task Download_ById()
    {
        await DownloadTest(
            new DownloadRequest {ContentId = 999999},
            "/binaryhandler.ashx?nodeid=999999&propertyname=Binary").ConfigureAwait(false);
    }
    [TestMethod]
    public async Task Download_ByPath()
    {
        await DownloadTest(
            new DownloadRequest { Path = "/Root/Content/MyFile" },
            "/binaryhandler.ashx?nodeid=999998&propertyname=Binary").ConfigureAwait(false);
    }
    [TestMethod]
    public async Task Download_Property()
    {
        await DownloadTest(
            new DownloadRequest { ContentId = 999999, PropertyName = "SecondaryStream" },
            "/binaryhandler.ashx?nodeid=999999&propertyname=SecondaryStream").ConfigureAwait(false);
    }
    [TestMethod]
    public async Task Download_Version()
    {
        await DownloadTest(
            new DownloadRequest { ContentId = 999999, Version = "V2.1.D" },
            "/binaryhandler.ashx?nodeid=999999&propertyname=Binary&version=V2.1.D").ConfigureAwait(false);
    }
    [TestMethod]
    public async Task Download_PropertyAndVersion()
    {
        await DownloadTest(
            new DownloadRequest { ContentId = 999999, PropertyName = "SecondaryStream", Version = "V2.1.D" },
            "/binaryhandler.ashx?nodeid=999999&propertyname=SecondaryStream&version=V2.1.D").ConfigureAwait(false);
    }

    private async Task DownloadTest(DownloadRequest request, string expectedUrl)
    {
        var fileContent = "111111111122222222223333333333444";
        var restCaller = Substitute.For<IRestCaller>();
        var byPath = request.ContentId == 0;
        if(byPath)
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Id"": ""999998"" }}"));
        restCaller
               .ProcessWebResponseAsync(Arg.Any<string>(), Arg.Any<HttpMethod>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(),
                Arg.Any<HttpContent>(),
                Arg.Do<Func<HttpResponseMessage, CancellationToken, Task>>(arg =>
                {
                    var response = new StringContent(fileContent);
                    response.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
                    response.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse("attachment; filename = numbers.txt");
                    response.Headers.ContentLength = 33L;
                    var msg = new HttpResponseMessage() { Content = response };
                    arg(msg, _cancel);
                }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", _cancel)
            .ConfigureAwait(false);

        // ACT
        string? text = null;
        StreamProperties? properties = null;
        await repository.DownloadAsync(request, async (stream, props) =>
        {
            properties = props;
            using var reader = new StreamReader(stream);
            text = await reader.ReadToEndAsync().ConfigureAwait(false);
        }, _cancel).ConfigureAwait(false);

        // ASSERT
        Assert.AreEqual(fileContent, text);
        Assert.IsNotNull(properties);
        Assert.AreEqual("text/plain", properties.MediaType);
        Assert.AreEqual("numbers.txt", properties.FileName);
        Assert.AreEqual(33L, properties.ContentLength);
        // Test restCaller calls
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(byPath ? 3 : 2, calls.Length);
        var call = calls[byPath ? 2 : 1];
        Assert.AreEqual("ProcessWebResponseAsync", call.GetMethodInfo().Name);
        var arguments = call.GetArguments();
        // string relativeUrl
        Assert.AreEqual(expectedUrl, arguments[0]?.ToString());
        // HttpMethod method
        Assert.AreEqual(HttpMethod.Get, arguments[1]);
        // Dictionary<string, IEnumerable<string>> additionalHeaders
        Assert.AreEqual(0, (arguments[2] as Dictionary<string, IEnumerable<string>>)?.Count ?? 0);
        // HttpContent postData
        var postData = arguments[3] as StringContent;
        Assert.IsNull(postData);
    }

}