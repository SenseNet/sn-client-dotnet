using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using SenseNet.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SenseNet.Client;
using SenseNet.Testing;

namespace DifferentNamespace
{
    public class MyContent : Content
    {
        public MyContent(ILogger<MyContent> logger) : base(logger) { }
    }
}
namespace SenseNet.Client.Tests
{
    public class MyContent : Content
    {
        public string HelloMessage => $"Hello {this.Name}!";
        public MyContent(ILogger<MyContent> logger) : base(logger) { }
    }
    public class MyContent2 : Content { public MyContent2(ILogger<MyContent> logger) : base(logger) { } }
    public class MyContent3 : Content { public MyContent3(ILogger<MyContent> logger) : base(logger) { } }
    public class MyContent4 : Content { public MyContent4(ILogger<MyContent> logger) : base(logger) { } }

    [TestClass]
    public class RepositoryTests
    {

        private const string ExampleUrl = "https://example.com";

        [TestMethod]
        public async Task Repository_Default()
        {
            // ALIGN
            var repositoryService = GetRepositoryService(services =>
            {
                services.ConfigureSenseNetRepository(opt => { opt.Url = ExampleUrl; });
            });

            // ACTION
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual(ExampleUrl, repository.Server.Url);
        }
        [TestMethod]
        public async Task Repository_Named()
        {
            // ALIGN
            var repositoryService = GetRepositoryService(services =>
            {
                services.ConfigureSenseNetRepository("repo1", opt => { opt.Url = ExampleUrl; });
                services.ConfigureSenseNetRepository("repo2", opt => { opt.Url = "https://url2"; });
            });

            // ACT
            var repo = await repositoryService.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);
            var repo1 = await repositoryService.GetRepositoryAsync("repo1", CancellationToken.None).ConfigureAwait(false);
            var repo2 = await repositoryService.GetRepositoryAsync("repo2", CancellationToken.None).ConfigureAwait(false);

            Assert.IsNull(repo.Server.Url);
            Assert.AreEqual(ExampleUrl, repo1.Server.Url);
            Assert.AreEqual("https://url2", repo2.Server.Url);
        }

        [TestMethod]
        public async Task Repository_LoadContent_Localhost()
        {
            // PREPARATION
            var repositoryService = GetRepositoryService();
            var repository = await repositoryService.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("Content", content.Name);
        }

        private static IRepositoryCollection GetRepositoryService(Action<IServiceCollection> addServices = null)
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
                //.AddSingleton<ITokenProvider, TestTokenProvider>()
                //.AddSingleton<ITokenStore, TokenStore>()
                .ConfigureSenseNetRepository("local", repositoryOptions =>
                {
                    // set test url and authentication in user secret
                    config.GetSection("sensenet:repository").Bind(repositoryOptions);
                });

            addServices?.Invoke(services);

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IRepositoryCollection>();
        }
    }
}
