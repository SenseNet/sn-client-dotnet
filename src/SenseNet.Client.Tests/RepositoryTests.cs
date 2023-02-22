using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using SenseNet.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class RepositoryTests
    {
        private class TestRestCaller : IRestCaller
        {
            private readonly string _expectedResponse;
            public TestRestCaller(string expectedResponse) { _expectedResponse = expectedResponse; }
            public Task<string> GetResponseStringAsync(Uri uri, ServerContext server) => Task.FromResult(_expectedResponse);
        }

        private class MyContent : Content
        {
            public string HelloMessage => $"Hello {this.Name}!";
            public MyContent(ILogger<MyContent> logger) : base(logger) { }
        }

        private const string ExampleUrl = "https://example.com";

        [TestMethod]
        public async Task Repository_Default()
        {
            // PREPARATION
            var repositoryService = GetRepositoryService(services =>
            {
                services.ConfigureSenseNetRepository(opt => { opt.Url = ExampleUrl; });
            });

            // ACTION
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(ExampleUrl, repository.Server.Url);
        }
        [TestMethod]
        public async Task Repository_Named()
        {
            // PREPARATION
            var rs = GetRepositoryService(services =>
            {
                services.ConfigureSenseNetRepository("repo1", opt => { opt.Url = ExampleUrl; });
                services.ConfigureSenseNetRepository("repo2", opt => { opt.Url = "https://url2"; });
            });

            // ACTION
            var repo = await rs.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);
            var repo1 = await rs.GetRepositoryAsync("repo1", CancellationToken.None).ConfigureAwait(false);
            var repo2 = await rs.GetRepositoryAsync("repo2", CancellationToken.None).ConfigureAwait(false);

            Assert.IsNull(repo.Server.Url);
            Assert.AreEqual(ExampleUrl, repo1.Server.Url);
            Assert.AreEqual("https://url2", repo2.Server.Url);
        }
        [TestMethod]
        public async Task Repository_LoadContent_Localhost()
        {
            // ALIGN
            var testRestCaller = new TestRestCaller(@"{
  ""d"": {
    ""Id"": 1000002,
    ""Name"": ""Content"",
    ""Type"": ""Content1"",
    ""StringProperty"": ""StringValue1""
  }
}");
            var repositoryService = GetRepositoryService(services =>
            {
                services.AddSingleton<IRestCaller>(testRestCaller);
            });
            var repository = await repositoryService.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("Content", content.Name);
        }
        [TestMethod]
        public async Task Repository_LoadContent_Localhost_CustomType()
        {
            // ALIGN
            var testRestCaller = new TestRestCaller(@"{
  ""d"": {
    ""Id"": 1000002,
    ""Name"": ""Content"",
    ""Type"": ""Content1"",
    ""StringProperty"": ""StringValue1""
  }
}");
            var repositoryService = GetRepositoryService(services =>
            {
                services.AddSingleton<IRestCaller>(testRestCaller);
            });
            var repository = await repositoryService.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync<MyContent>("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("Hello Content!", content.HelloMessage);
        }

        private static IRepositoryService GetRepositoryService(Action<IServiceCollection> addServices = null)
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<RepositoryTests>()
                .Build();

            services
                .AddSingleton<IConfiguration>(config)
                .AddSenseNetClient()
                .AddTransient<MyContent, MyContent>() // <------------------- Register custom type
                //.AddSingleton<ITokenProvider, TestTokenProvider>()
                //.AddSingleton<ITokenStore, TokenStore>()
                .ConfigureSenseNetRepository("local", repositoryOptions =>
                {
                    // set test url and authentication in user secret
                    config.GetSection("sensenet:repository").Bind(repositoryOptions);
                });

            addServices?.Invoke(services);

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IRepositoryService>();
        }
    }
}
