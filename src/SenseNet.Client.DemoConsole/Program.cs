using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SenseNet.Client;
using SenseNet.Extensions.DependencyInjection;
using Serilog;

// The default host builder adds all the necessary features
// for logging and configuration.
var host = Host.CreateDefaultBuilder()
    .UseSerilog((context, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);
    })
    .ConfigureServices((context, services) =>
    {
        // add the sensenet client feature and configure the repository
        services
            .AddSenseNetClient()
            .ConfigureSenseNetRepository(repositoryOptions =>
            {
                // the section path can be anything
                context.Configuration.GetSection("sensenet:repository").Bind(repositoryOptions);
            });
    }).Build();

// Get the main entry point for the client API.
var repositoryCollection = host.Services.GetRequiredService<IRepositoryCollection>();

// Get the repository instance. This instance is already set up with authentication,
// can be pinned or your can get it using this API multiple times.
var repository = await repositoryCollection.GetRepositoryAsync(CancellationToken.None);

// Access the repository.
var children = await repository.LoadCollectionAsync(new LoadCollectionRequest
{
    Path = "/Root/Content",
    OrderBy = new []{ "Name" }
}, CancellationToken.None);

foreach (var content in children)
{
    Console.WriteLine($"Content: {content.Path}");
}