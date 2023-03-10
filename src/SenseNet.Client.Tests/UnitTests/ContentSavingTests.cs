using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class ContentSavingTests
{
    public class TestContent_A : Content
    {
        public TestContent_A(IRestCaller restCaller, ILogger<TestContent_A> logger) : base(restCaller, logger) { }
        public int Index { get; set; }
    }
    [TestMethod]
    public async Task Content_T_Save_1()
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .PostContentAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.RegisterGlobalContentType<TestContent_A>();
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var content = repository.CreateContent<TestContent_A>("/Root/Content", null, "MyContent-1");
        content.Index = 999;
        await content.SaveAsync().ConfigureAwait(false);

        // ASSERT
        var arguments = restCaller.ReceivedCalls().Single().GetArguments();
        Assert.AreEqual("/Root/Content", arguments[0]); // parentPath
        dynamic data = arguments[1]!;
        Assert.IsNotNull(data);
        Assert.AreEqual("MyContent-1", data.Name);
        Assert.AreEqual("TestContent_A", data.__ContentType);
        Assert.AreEqual(999, (int)data["Index"]);
    }

    /* ====================================================================== TOOLS */

    private static IRepositoryCollection GetRepositoryCollection(Action<IServiceCollection>? addServices = null)
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