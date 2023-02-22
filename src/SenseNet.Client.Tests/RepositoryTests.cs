using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using SenseNet.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace SenseNet.Client.Tests
{
    public class MyContent : Content
    {
        public string HelloMessage => $"Hello {this.Name}!";
        public MyContent(ILogger<MyContent> logger) : base(logger) { }
    }
    [TestClass]
    public class RepositoryTests
    {

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
            var repositoryService = GetRepositoryService(services =>
            {
                services.ConfigureSenseNetRepository("repo1", opt => { opt.Url = ExampleUrl; });
                services.ConfigureSenseNetRepository("repo2", opt => { opt.Url = "https://url2"; });
            });

            // ACTION
            var repo = await repositoryService.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);
            var repo1 = await repositoryService.GetRepositoryAsync("repo1", CancellationToken.None).ConfigureAwait(false);
            var repo2 = await repositoryService.GetRepositoryAsync("repo2", CancellationToken.None).ConfigureAwait(false);

            Assert.IsNull(repo.Server.Url);
            Assert.AreEqual(ExampleUrl, repo1.Server.Url);
            Assert.AreEqual("https://url2", repo2.Server.Url);
        }

        [TestMethod]
        public async Task Repository_LoadContent_GeneralType()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Name"": ""Content"" }}"));
            var repositoryService = GetRepositoryService(services =>
            {
                services.AddSingleton<IRestCaller>(restCaller);
            });
            var repository = await repositoryService.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(content);
            Assert.AreEqual("Content", content.Name);
        }
        [TestMethod]
        public async Task Repository_LoadContent_KnownCustomType()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Name"": ""Content"" }}"));
            var repositoryService = GetRepositoryService(services =>
            {
                services.AddSingleton<IRestCaller>(restCaller);
                services.AddTransient<MyContent, MyContent>();
            });
            var repository = await repositoryService.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync<MyContent>("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("Hello Content!", content.HelloMessage);
        }
        [TestMethod]
        public async Task Repository_LoadContent_KnownCustomTypeAsGeneral()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Type"": ""MyContent"", ""Name"": ""Content"" }}"));
            var repositoryService = GetRepositoryService(services =>
            {
                services.AddSingleton<IRestCaller>(restCaller);

                 services.RegisterContentType<MyContent>();
                //services.RegisterContentType(typeof(MyContent));
                //services.ConfigureContentType(typeof(Repo2.Book), "Book");
            });
            var repository = await repositoryService.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(content);
            Assert.AreEqual("Content", content.Name);
            Assert.AreEqual(typeof(MyContent), content.GetType());
        }


        [TestMethod]
        public async Task Repository_RegisterContentType_Type()
        {
            // ACTION
            var repositoryService = GetRepositoryService(services =>
            {
                services.RegisterContentType(typeof(MyContent));
            });

            // ASSERT
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            var content = repository.CreateContent<MyContent>();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
            repository.ContentTypes.ContentTypes.TryGetValue("MyContent", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_RegisterContentType_TypeParam()
        {
            // ACTION
            var repositoryService = GetRepositoryService(services =>
            {
                services.RegisterContentType<MyContent>();
                //services.RegisterContentType(typeof(MyContent));
                //services.ConfigureContentType(typeof(Repo2.Book), "Book");
            });

            // ASSERT
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            var content = repository.CreateContent<MyContent>();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
            repository.ContentTypes.ContentTypes.TryGetValue("MyContent", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_RegisterContentType_TypeParamAndDifferentName()
        {
            // ACTION
            var repositoryService = GetRepositoryService(services =>
            {
                services.RegisterContentType<MyContent>("MyType");
            });

            // ASSERT
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            var content = repository.CreateContent<MyContent>();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
            repository.ContentTypes.ContentTypes.TryGetValue("MyType", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_RegisterContentType_TypeAndDifferentName()
        {
            // ACTION
            var repositoryService = GetRepositoryService(services =>
            {
                services.RegisterContentType(typeof(MyContent), "MyType");
            });

            // ASSERT
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            var content = repository.CreateContent<MyContent>();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
            repository.ContentTypes.ContentTypes.TryGetValue("MyType", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
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
