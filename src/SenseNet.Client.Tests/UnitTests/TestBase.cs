using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests.UnitTests;

public abstract class TestBase
{
    protected readonly string LocalServer = "local";
    protected readonly string FakeServer = "fake";

    protected IRestCaller CreateRestCallerFor(string returnValueOfGetResponseStringAsync)
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returnValueOfGetResponseStringAsync));
        return restCaller;
    }

    private class TestRestCaller : IRestCaller
    {
        private string _responseString;
        public ServerContext Server { get; set; }

        public TestRestCaller(string responseString)
        {
            _responseString = responseString;
        }

        public Task<string> GetResponseStringAsync(Uri uri, HttpMethod method, string postData, Dictionary<string, IEnumerable<string>> additionalHeaders,
            CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task ProcessWebResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders, HttpContent postData,
            Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task ProcessWebRequestResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
            Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor, Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
        {
            responseProcessor(new HttpResponseMessage(HttpStatusCode.OK)
                {Content = new StringContent(_responseString)}, CancellationToken.None);
            return Task.CompletedTask;
        }
    }

    protected IRestCaller CreateRestCallerForProcessWebRequestResponse(string responseString)
    {
        return new TestRestCaller(responseString);
    }

    protected IRepositoryCollection GetRepositoryCollection(Action<IServiceCollection>? addServices = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        var config = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json", optional: true)
            .AddUserSecrets<RepositoryTests>()
            .Build();

        services
            .AddSingleton<IConfiguration>(config)
            .AddSenseNetClient()
            //.AddSingleton<ITokenProvider, TestTokenProvider>()
            //.AddSingleton<ITokenStore, TokenStore>()
            .ConfigureSenseNetRepository(LocalServer, repositoryOptions =>
            {
                // set test url and authentication in user secret
                config.GetSection("sensenet:repository").Bind(repositoryOptions);
            })
            .ConfigureSenseNetRepository(FakeServer, repositoryOptions =>
            {
                // url to nothing
                repositoryOptions.Url = "https://urlfor.unittests";
                // Avoid the 4 second authentication request
                repositoryOptions.Authentication.ApiKey = null;
                repositoryOptions.Authentication.ClientId = null;
                repositoryOptions.Authentication.ClientSecret = null;
            });

        addServices?.Invoke(services);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IRepositoryCollection>();
    }

    protected string RemoveWhitespaces(string value)
    {
        return value.Replace(" ", "").Replace("\t", "")
            .Replace("\r", "").Replace("\n", "");
    }

}