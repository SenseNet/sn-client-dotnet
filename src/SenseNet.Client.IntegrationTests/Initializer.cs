using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            // create a service collection and register the sensenet client
            var services = new ServiceCollection()
                .AddSenseNetClient()
                .ConfigureSenseNetRepository(repositoryOptions =>
                {
                    config.GetSection("sensenet:repository").Bind(repositoryOptions);
                })
                .BuildServiceProvider();

            // get the repository amd extract the server context
            var repositories = services.GetRequiredService<IRepositoryCollection>();
            var repository = repositories.GetRepositoryAsync(CancellationToken.None).GetAwaiter().GetResult();
            
            server = repository.Server;
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