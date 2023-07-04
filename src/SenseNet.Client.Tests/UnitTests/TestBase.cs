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

}