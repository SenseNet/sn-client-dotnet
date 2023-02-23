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
        public async Task Repository_RegisterGlobalContentType_TypeParam()
        {
            // ACTION
            var repositoryService = GetRepositoryService(services =>
            {
                services.RegisterGlobalContentType<MyContent>();
            });

            // ASSERT
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            var content = repository.CreateContent<MyContent>();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
            repository.GlobalContentTypes.ContentTypes.TryGetValue("MyContent", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_RegisterGlobalContentType_TypeParamAndDifferentName()
        {
            // ACTION
            var repositoryService = GetRepositoryService(services =>
            {
                services.RegisterGlobalContentType<MyContent>("MyType");
            });

            // ASSERT
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            var content = repository.CreateContent<MyContent>();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
            repository.GlobalContentTypes.ContentTypes.TryGetValue("MyType", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_RegisterGlobalContentType_Type()
        {
            // ACTION
            var repositoryService = GetRepositoryService(services =>
            {
                services.RegisterGlobalContentType(typeof(MyContent));
            });

            // ASSERT
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            var content = repository.CreateContent<MyContent>();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
            repository.GlobalContentTypes.ContentTypes.TryGetValue("MyContent", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_RegisterGlobalContentType_TypeAndDifferentName()
        {
            // ACTION
            var repositoryService = GetRepositoryService(services =>
            {
                services.RegisterGlobalContentType(typeof(MyContent), "MyType");
            });

            // ASSERT
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            var content = repository.CreateContent<MyContent>();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
            repository.GlobalContentTypes.ContentTypes.TryGetValue("MyType", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }

        [TestMethod]
        public async Task Repository_RegisterContentTypes()
        {
            // ALIGN
            var repositoryService = GetRepositoryService(services =>
            {
                services.ConfigureSenseNetRepository(
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes
                            .Add<MyContent>()
                            .Add<MyContent2>("MyContent_2")
                            .Add(typeof(MyContent3))
                            .Add(typeof(MyContent4), "MyContent_4")
                            ;
                    });
            });

            // ACTION
            var repository = await repositoryService.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            var repositoryAcc = new ObjectAccessor(repository);
            var services = (IServiceProvider)repositoryAcc.GetField("_services");
            Assert.AreEqual(ExampleUrl, repository.Server.Url);
            Assert.AreEqual(0, repository.GlobalContentTypes.ContentTypes.Count);
            var contentTypeRegistrations = repository.Server.RegisteredContentTypes.ContentTypes
                .OrderBy(x => x.Value.Name)
                .ToArray();

            Assert.AreEqual(4, contentTypeRegistrations.Length);

            Assert.IsNotNull(services.GetService<MyContent>());
            Assert.AreEqual("MyContent", contentTypeRegistrations[0].Key);
            Assert.AreEqual(typeof(MyContent), contentTypeRegistrations[0].Value);

            Assert.IsNotNull(services.GetService<MyContent2>());
            Assert.AreEqual("MyContent_2", contentTypeRegistrations[1].Key);
            Assert.AreEqual(typeof(MyContent2), contentTypeRegistrations[1].Value);

            Assert.IsNotNull(services.GetService<MyContent3>());
            Assert.AreEqual("MyContent3", contentTypeRegistrations[2].Key);
            Assert.AreEqual(typeof(MyContent3), contentTypeRegistrations[2].Value);

            Assert.IsNotNull(services.GetService<MyContent4>());
            Assert.AreEqual("MyContent_4", contentTypeRegistrations[3].Key);
            Assert.AreEqual(typeof(MyContent4), contentTypeRegistrations[3].Value);
        }
        [TestMethod]
        public async Task Repository_RegisterContentTypes_DifferentTypeSameName()
        {
            // ALIGN
            var repo1Name = "Repo1";
            var repo2Name = "Repo2";
            var exampleUrl2 = "https://example2.com";
            var repositoryService = GetRepositoryService(services =>
            {
                services.ConfigureSenseNetRepository(repo1Name,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<MyContent>();
                    });
                services.ConfigureSenseNetRepository(repo2Name,
                    configure: options => { options.Url = exampleUrl2; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<DifferentNamespace.MyContent>();
                    });
            });

            // ACT
            var repository1 = await repositoryService.GetRepositoryAsync(repo1Name, CancellationToken.None).ConfigureAwait(false);
            var repository2 = await repositoryService.GetRepositoryAsync(repo2Name, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            // check repo1
            Assert.AreEqual(ExampleUrl, repository1.Server.Url);
            Assert.AreEqual(0, repository1.GlobalContentTypes.ContentTypes.Count);
            var contentTypeRegistrations1 = repository1.Server.RegisteredContentTypes.ContentTypes
                .OrderBy(x => x.Value.Name)
                .ToArray();
            Assert.AreEqual(1, contentTypeRegistrations1.Length);
            Assert.AreEqual("MyContent", contentTypeRegistrations1[0].Key);
            Assert.AreEqual(typeof(MyContent), contentTypeRegistrations1[0].Value);

            // check repo2
            Assert.AreEqual(exampleUrl2, repository2.Server.Url);
            Assert.AreEqual(0, repository2.GlobalContentTypes.ContentTypes.Count);
            var contentTypeRegistrations2 = repository2.Server.RegisteredContentTypes.ContentTypes
                .OrderBy(x => x.Value.Name)
                .ToArray();
            Assert.AreEqual(1, contentTypeRegistrations2.Length);
            Assert.AreEqual("MyContent", contentTypeRegistrations2[0].Key);
            Assert.AreEqual(typeof(DifferentNamespace.MyContent), contentTypeRegistrations2[0].Value);

            // check services
            var repositoryAcc = new ObjectAccessor(repository1);
            var services = (IServiceProvider)repositoryAcc.GetField("_services");
            Assert.IsNotNull(services.GetService<MyContent>());
            Assert.IsNotNull(services.GetService<DifferentNamespace.MyContent>());
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

                services.RegisterGlobalContentType<MyContent>();
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
