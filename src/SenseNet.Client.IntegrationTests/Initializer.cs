using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Client.Authentication;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.IntegrationTests;

[TestClass]
public class Initializer
{
    [AssemblyInitialize]
    public static void InitializeAllTests(TestContext context)
    {
        InitializeServer(context);
    }

    public static void InitializeServer(TestContext? context = null)
    {
        var server = new ServerContext
        {
            Url = "https://localhost:44362"
        };

        if (context != null)
        {
            // workaround for authenticating using the configured client id and secret
            var config = new ConfigurationBuilder()
                .SetBasePath(context.DeploymentDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<Initializer>()
                .Build();

            var options = new RepositoryOptions();
            config.GetSection("sensenet:repository").Bind(options);

            var services = new ServiceCollection()
                .AddLogging()
                .AddSenseNetClientTokenStore()
                .BuildServiceProvider();

            var tokenStore = services.GetRequiredService<ITokenStore>();
            var token = tokenStore.GetTokenAsync(server,
                options.Authentication.ClientId,
                options.Authentication.ClientSecret,
                CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

            server.Authentication.AccessToken = token;
        }

        ClientContext.Current.RemoveAllServers();
        ClientContext.Current.AddServers(new[]
        {
            server
        });

        // for testing purposes
        //ClientContext.Current.ChunkSizeInBytes = 1024;
    }
}