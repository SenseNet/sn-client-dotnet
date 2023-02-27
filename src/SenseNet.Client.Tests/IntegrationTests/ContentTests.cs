using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Tests.UnitTests;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests.IntegrationTests
{
    [TestClass]
    public class ContentTests
    {
        [TestMethod]
        public async Task IT_Content_Load()
        {
            // ALIGN-1
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", CancellationToken.None).ConfigureAwait(false);
            var expectedPath = "/Root/Content";

            // ACT-1: Load by path
            var content1 = await repository.LoadContentAsync(expectedPath, CancellationToken.None).ConfigureAwait(false);

            // ASSERT-1: not null
            Assert.IsNotNull(content1);

            // ALIGN-2
            var contentId = content1.Id;

            // ACT-2: Load by Id
            var content2 = await repository.LoadContentAsync(contentId, CancellationToken.None).ConfigureAwait(false);

            // ASSERT-2
            Assert.IsNotNull(content2);
            Assert.AreEqual(expectedPath, content2.Path);
        }

        [TestMethod]
        public async Task IT_Content_ExistsCreateDelete()
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
            var parentPath = "/Root/Content";
            var contentName = nameof(IT_Content_ExistsCreateDelete);
            var contentTypeName = "Folder";
            var path = $"{parentPath}/{contentName}";

            // OPERATIONS
            // 1 - Delete content if exists for the clean test
            if (await repository.IsContentExistAsync(path, cancel).ConfigureAwait(false))
            {
                await repository.DeleteContentAsync(path, true, cancel).ConfigureAwait(false);
                Assert.IsFalse(await repository.IsContentExistAsync(path, cancel).ConfigureAwait(false));
            }

            // 2 - Create brand new content and test its existence
            var content = repository.CreateContent(parentPath, contentTypeName, contentName);
            await content.SaveAsync().ConfigureAwait(false); //UNDONE: missing CancellationToken parameter of SaveAsync()
            Assert.IsTrue(await repository.IsContentExistAsync(path, cancel).ConfigureAwait(false));

            // 3 - Delete the content and check the repository is clean
            await repository.DeleteContentAsync(path, true, cancel).ConfigureAwait(false);
            Assert.IsFalse(await repository.IsContentExistAsync(path, cancel).ConfigureAwait(false));
        }

        /* ================================================================================================== TOOLS */

        private static IRepositoryCollection GetRepositoryCollection(Action<IServiceCollection> addServices = null)
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
            return provider.GetRequiredService<IRepositoryCollection>();
        }

    }
}
