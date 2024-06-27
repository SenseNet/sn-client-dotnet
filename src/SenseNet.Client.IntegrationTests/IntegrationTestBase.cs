using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.IntegrationTests;

public class IntegrationTestBase
{
    [AssemblyInitialize]
    public static void InitializeAllTests(TestContext context)
    {
        SnTrace.Custom.Enabled = true;
        SnTrace.Test.Enabled = true;
    }

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void InitializeTest()
    {
        SnTrace.Custom.Enabled = true;
        SnTrace.Test.Enabled = true;
        SnTrace.Test.Write($">>>> TEST: {TestContext.TestName}");
    }

    protected IRepositoryCollection GetRepositoryCollection(Action<IServiceCollection> addServices = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<ContentTests>()
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

    protected static Task<Content> EnsureContentAsync(string path, string typeName, IRepository repository, CancellationToken cancel)
    {
        return EnsureContentAsync(path, typeName, null, repository, cancel);
    }
    protected static async Task<Content> EnsureContentAsync(string path, string typeName, Action<Content>? setProperties, IRepository repository, CancellationToken cancel)
    {
        var content = await repository.LoadContentAsync(path, cancel);
        if (content == null)
        {
            var parentPath = RepositoryPath.GetParentPath(path);
            var name = RepositoryPath.GetFileName(path);
            content = repository.CreateContent(parentPath, typeName, name);
            if (setProperties == null)
            {
                await content.SaveAsync(cancel);
                return content;
            }
        }

        if (setProperties != null)
        {
            setProperties(content);
            await content.SaveAsync(cancel);
        }

        return content;
    }

}