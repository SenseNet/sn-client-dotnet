using System;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Authentication;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace SenseNet.Client.TestsForDocs.Infrastructure
{
    //UNDONE:- Feature request: find a solution for the tests to get a raw response.
    //UNDONE:- Feature request: find a solution for the tests to get a final request url instead of making the request.
    [TestClass]
    public class ClientIntegrationTestBase
    {
        public static readonly string Url = "https://localhost:44362";

        [AssemblyInitialize]
        public static void InitializeAllTests(TestContext context)
        {
            var repository = InitServer(context);
            var cancel = new CancellationToken();

            EnsureBasicStructureAsync(repository, cancel).ConfigureAwait(false).GetAwaiter().GetResult();

            SnTrace.Custom.Enabled = true;
            SnTrace.Test.Enabled = true;
        }
        private static async Task EnsureBasicStructureAsync(IRepository repository, CancellationToken cancel)
        {
            await EnsureContentAsync("/Root/Content", "Folder", repository, cancel);
            await EnsureContentAsync("/Root/Content/IT", "Workspace", repository, cancel);
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", c =>
            {
                c["Description"] = "Document library of IT";
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago", "Folder", repository, cancel);
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder", repository, cancel);
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File", repository, cancel);
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Munich", "Folder", repository, cancel);
            await EnsureContentAsync("/Root/IMS/Public", "Domain", repository, cancel);
            await EnsureContentAsync("/Root/IMS/Public/Editors", "Group", repository, cancel);

            await EnsureContentAsync("/Root/Content/Cars", "Folder", c =>
            {
                c["Description"] = "This folder contains our cars.";
            }, repository, cancel);
        }

        [TestInitialize]
        public void InitializeTest()
        {
        }

        private static IRepository InitServer(TestContext context)
        {
            // workaround for authenticating using the configured client id and secret
            var config = new ConfigurationBuilder()
                .SetBasePath(context.DeploymentDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<ClientIntegrationTestBase>()
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
            
            var server = repository.Server;

            var ctx = ClientContext.Current;
            ctx.RemoveServers(ctx.Servers);
            ctx.AddServer(server);

            return repository;
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

        protected IRepositoryCollection GetRepositoryCollection(Action<IServiceCollection>? addServices = null)
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<ClientIntegrationTestBase>()
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
