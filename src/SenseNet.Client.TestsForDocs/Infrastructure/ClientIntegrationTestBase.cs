using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.TestsForDocs.Infrastructure
{
    //UNDONE:- Feature request: find a solution for the tests to get a raw response.
    //UNDONE:- Feature request: find a solution for the tests to get a final request url instead of making the request.
    [TestClass]
    public class ClientIntegrationTestBase
    {
        public static readonly string Url = "https://localhost:44362";

        [AssemblyInitialize]
        public static void InititalizeAllTests(TestContext testContext)
        {
            ClientContext.Current.AddServer(new ServerContext
            {
                Url = Url,
                Username = "builtin\\admin",
                Password = "admin"
            });

            EnsureBasicStructureAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            SnTrace.Custom.Enabled = true;
            SnTrace.Test.Enabled = true;
        }
        private static async Task EnsureBasicStructureAsync()
        {
            var c = await Content.LoadAsync("/Root/Content");
            if (c == null)
            {
                c = Content.CreateNew("/Root", "Folder", "Content");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content", "Workspace", "IT");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT", "DocumentLibrary", "Document_Library");
                c["Description"] = "Document library of IT";
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Chicago");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT/Document_Library", "Folder", "Chicago");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT/Document_Library", "Folder", "Calgary");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT/Document_Library/Calgary", "File", "BusinessPlan.docx");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Munich");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT/Document_Library", "Folder", "Munich");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/IMS/Public");
            if (c == null)
            {
                c = Content.CreateNew("/Root/IMS", "Domain", "Public");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/IMS/Public/Editors");
            if (c == null)
            {
                c = Content.CreateNew("/Root/IMS/Public", "Group", "Editors");
                await c.SaveAsync();
            }
        }

        [TestInitialize]
        public void InitializeTest()
        {
            var ctx = ClientContext.Current;
            ctx.RemoveServers(ctx.Servers);
            ctx.AddServer(new ServerContext
            {
                Url = Url,
                Username = "builtin\\admin",
                Password = "admin"
            });
        }

        protected Task<Content> EnsureContentAsync(string path, string typeName)
        {
            return EnsureContentAsync(path, typeName, null);
        }
        protected async Task<Content> EnsureContentAsync(string path, string typeName, Action<Content> setProperties)
        {
            var content = await Content.LoadAsync(path);
            if (content == null)
            {
                var parentPath = RepositoryPath.GetParentPath(path);
                var name = RepositoryPath.GetFileName(path);
                content = Content.CreateNew(parentPath, typeName, name);
                if (setProperties == null)
                {
                    await content.SaveAsync();
                    return content;
                }
            }

            if (setProperties != null)
            {
                setProperties(content);
                await content.SaveAsync();
            }

            return content;
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
                //.AddUserSecrets<ContentTests>()
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
