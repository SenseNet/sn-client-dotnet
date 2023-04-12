using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tools;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class RestCallerTests
{
    [TestMethod]
    public async Task RestCaller_Request_Uri_Absolute_Schema_Replaced()
    {
        var request = await ProcessWebRequestResponseTestAsync("http://example.com:1234/oodata.svc/Root",
            HttpMethod.Post, null);
        Assert.IsNotNull(request);
        Assert.IsNotNull(request.RequestUri);
        Assert.AreEqual("https://example.com:1234/oodata.svc/Root", request.RequestUri.ToString());
    }
    [TestMethod]
    public async Task RestCaller_Request_Uri_Absolute_Host_Replaced()
    {
        var request = await ProcessWebRequestResponseTestAsync("ftp://getting.warez.com/ooodata.svc/Root",
            HttpMethod.Post, null);
        Assert.IsNotNull(request);
        Assert.IsNotNull(request.RequestUri);
        Assert.AreEqual("https://example.com:1234/ooodata.svc/Root", request.RequestUri.ToString());
    }
    [TestMethod]
    public async Task RestCaller_Request_Uri_Relative_WithoutPrefix()
    {
        var request = await ProcessWebRequestResponseTestAsync("ooodata.svc/Root",
            HttpMethod.Post, null);
        Assert.IsNotNull(request);
        Assert.IsNotNull(request.RequestUri);
        Assert.AreEqual("https://example.com:1234/Root", request.RequestUri.ToString());
    }
    [TestMethod]
    public async Task RestCaller_Request_Uri_Relative_WithPrefix()
    {
        var request = await ProcessWebRequestResponseTestAsync("/ooodata.svc/Root",
            HttpMethod.Post, null);
        Assert.IsNotNull(request);
        Assert.IsNotNull(request.RequestUri);
        Assert.AreEqual("https://example.com:1234/ooodata.svc/Root", request.RequestUri.ToString());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task RestCaller_Request_Method_Null()
    {
        var _ = await ProcessWebRequestResponseTestAsync("odata.svc/Root", null, null);
    }
    [TestMethod]
    public async Task RestCaller_Request_Method_Get()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Get, null);
        Assert.IsNotNull(request);
        Assert.AreEqual(HttpMethod.Get, request.Method);
    }
    [TestMethod]
    public async Task RestCaller_Request_Method_Post()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Post, null);
        Assert.IsNotNull(request);
        Assert.AreEqual(HttpMethod.Post, request.Method);
    }
    [TestMethod]
    public async Task RestCaller_Request_Method_Patch()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Patch, null);
        Assert.IsNotNull(request);
        Assert.AreEqual(HttpMethod.Patch, request.Method);
    }
    [TestMethod]
    public async Task RestCaller_Request_Method_Delete()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Delete, null);
        Assert.IsNotNull(request);
        Assert.AreEqual(HttpMethod.Delete, request.Method);
    }
    [TestMethod]
    public async Task RestCaller_Request_Method_Put()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Put, null);
        Assert.IsNotNull(request);
        Assert.AreEqual(HttpMethod.Put, request.Method);
    }
    [TestMethod]
    public async Task RestCaller_Request_Method_Head()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Head, null);
        Assert.IsNotNull(request);
        Assert.AreEqual(HttpMethod.Head, request.Method);
    }
    [TestMethod]
    public async Task RestCaller_Request_Method_Options()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Options, null);
        Assert.IsNotNull(request);
        Assert.AreEqual(HttpMethod.Options, request.Method);
    }
    [TestMethod]
    public async Task RestCaller_Request_Method_Trace()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Trace, null);
        Assert.IsNotNull(request);
        Assert.AreEqual(HttpMethod.Trace, request.Method);
    }

    [TestMethod]
    public async Task RestCaller_Request_Headers_Null()
    {
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Trace, null);
        Assert.IsNotNull(request);
    }
    [TestMethod]
    public async Task RestCaller_Request_Headers_Empty()
    {
        var headers = new Dictionary<string, IEnumerable<string>>();
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Trace, headers);
        Assert.IsNotNull(request);
    }
    [TestMethod]
    public async Task RestCaller_Request_Headers()
    {
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            {"Header1", new[] {"Value1"}},
            {"Header2", new[] {"Value2", "Value3", "Value4"}}
        };

        // ACT
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Trace, headers);

        // ASSERT
        Assert.IsNotNull(request);
        string? value1 = null;
        if (request.Headers.TryGetValues("Header1", out var values1))
            value1 = string.Join(", ", values1);
        Assert.AreEqual("Value1", value1);
        string? value2 = null;
        if (request.Headers.TryGetValues("Header2", out var values2))
            value2 = string.Join(", ", values2);
        Assert.AreEqual("Value2, Value3, Value4", value2);
    }
    [TestMethod]
    public async Task RestCaller_Request_Headers_Auth_AccessToken()
    {
        var accessToken = "46516516163175165524756";
        var server = new ServerContext();
        server.Url = "https://example.com/";
        server.Authentication.AccessToken = accessToken;

        // ACT
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Trace, null, server);

        // ASSERT
        Assert.IsNotNull(request);
        string? value = null;
        if (request.Headers.TryGetValues("Authorization", out var values))
            value = string.Join(", ", values);
        Assert.AreEqual("Bearer " + accessToken, value);
    }
    [TestMethod]
    public async Task RestCaller_Request_Headers_Auth_ApiKey()
    {
        var apiKey = "My API Key";
        var server = new ServerContext();
        server.Url = "https://example.com/";
        server.Authentication.ApiKey = apiKey;

        // ACT
        var request = await ProcessWebRequestResponseTestAsync("odata.svc/Root", HttpMethod.Trace, null, server);

        // ASSERT
        Assert.IsNotNull(request);
        string? value1 = null;
        if (request.Headers.TryGetValues("apikey", out var values))
            value1 = string.Join(", ", values);
        Assert.AreEqual(apiKey, value1);
    }

    /* =============================================================================== TOOLS */

    public async Task<HttpRequestMessage?> ProcessWebRequestResponseTestAsync(string url, HttpMethod method,
        Dictionary<string, IEnumerable<string>>? headers, ServerContext? server = null)
    {
        var logger = new Logger<DefaultRetrier>(new NullLoggerFactory());
        var retrier = new DefaultRetrier(Options.Create(new RetrierOptions()), logger);
        var restCaller = new DefaultRestCaller(retrier);
        restCaller.Server = server ?? new ServerContext { IsTrusted = true, Logger = logger, Url = "https://example.com:1234" };
        var cancellation = new CancellationTokenSource();

        HttpRequestMessage? httpRequestMessage = null;
        try
        {
            await restCaller.ProcessWebRequestResponseAsync(
                relativeUrl: url,
                method: method,
                additionalHeaders: headers,
                requestProcessor: (handler, client, request) =>
                {
                    httpRequestMessage = request;
                    cancellation.Cancel();
                },
                responseProcessor: (message, token) => Task.CompletedTask,
                cancel: cancellation.Token);
            Assert.Fail("The expected OperationCanceledException was not thrown.");
        }
        catch (OperationCanceledException) { }

        return httpRequestMessage;
    }
}