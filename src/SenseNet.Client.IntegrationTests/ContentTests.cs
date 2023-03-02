using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Extensions.DependencyInjection;
using System.Threading.Channels;

namespace SenseNet.Client.IntegrationTests
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
            if (await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false))
            {
                await repository.DeleteContentAsync(path, true, cancel).ConfigureAwait(false);
                Assert.IsFalse(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));
            }

            // 2 - Create brand new content and test its existence
            var content = repository.CreateContent(parentPath, contentTypeName, contentName);
            await content.SaveAsync().ConfigureAwait(false);
            Assert.IsTrue(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));

            // 3 - Delete the content and check the repository is clean
            await repository.DeleteContentAsync(path, true, cancel).ConfigureAwait(false);
            Assert.IsFalse(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task IT_Content_Query()
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

            // ACT
            var request = new QueryContentRequest
            {
                ContentQuery = "TypeIs:User",
                Select = new[] {"Name", "Path", "Type"},
                OrderBy = new[] {"Name"}
            };
            var contents = await repository.QueryAsync(request, cancel).ConfigureAwait(false);

            // ASSERT
            var names = contents.Select(x => x.Name).ToArray();
            Assert.IsTrue(names.Contains("Admin"));
            Assert.IsTrue(names.Contains("Visitor"));
            var types = contents.Select(x => x["Type"].ToString()).Distinct().ToArray();
            Assert.AreEqual(1, types.Length);
            Assert.AreEqual("User", types[0]);
        }
        [TestMethod]
        public async Task IT_Content_QueryCount()
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

            // ACT-1: get collection
            var request = new QueryContentRequest
            {
                ContentQuery = "TypeIs:User",
                OrderBy = new[] { "Name" }
            };
            var contents = await repository.QueryAsync(request, cancel).ConfigureAwait(false);
            var expectedCount = contents.Count();

            // ASSERT-1
            Assert.IsTrue(expectedCount > 0);

            // ACT-2
            var actualCount = await repository.QueryCountAsync(request, cancel).ConfigureAwait(false);

            // ASSERT-2
            Assert.AreEqual(expectedCount, actualCount);
        }

        [TestMethod]
        public async Task IT_Content_Query_Depth()
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
            var rootName = nameof(IT_Content_Query_Depth);
            var rootPath = $"/Root/Content/{rootName}";
            await CreateStructureForDepthTests(repository, rootName, cancel).ConfigureAwait(false);

            // ACT
            var request = new LoadCollectionRequest
            {
                Path = rootPath,
                ContentQuery = "Name:'*-1'",
                Select = new[] { "Name", "Path", "Type" },
                OrderBy = new[] { "Path" }
            };
            var result = await repository.QueryAsync(request, cancel).ConfigureAwait(false);
            var contents = result.ToArray();

            // ASSERT
            Assert.AreEqual(4, contents.Length);
            Assert.AreEqual($"{rootPath}/Folder-0/File-1", contents[0].Path);
            Assert.AreEqual($"{rootPath}/Folder-1", contents[1].Path);
            Assert.AreEqual($"{rootPath}/Folder-1/File-1", contents[2].Path);
            Assert.AreEqual($"{rootPath}/Folder-2/File-1", contents[3].Path);

            // ACT-2
            request = new LoadCollectionRequest
            {
                Path = $"{rootPath}/Folder-1",
                ContentQuery = "Name:'*-1'",
                Select = new[] { "Name", "Path", "Type" },
                OrderBy = new[] { "Path" }
            };
            result = await repository.QueryAsync(request, cancel).ConfigureAwait(false);
            contents = result.ToArray();

            // ASSERT-2
            Assert.AreEqual(2, contents.Length);
            Assert.AreEqual($"{rootPath}/Folder-1", contents[0].Path);
            Assert.AreEqual($"{rootPath}/Folder-1/File-1", contents[1].Path);

        }
        [TestMethod]
        public async Task IT_Content_Collection_Depth()
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
            var rootName = nameof(IT_Content_Query_Depth);
            var rootPath = $"/Root/Content/{rootName}";
            await CreateStructureForDepthTests(repository, rootName, cancel).ConfigureAwait(false);

            // ACT
            var request = new LoadCollectionRequest
            {
                Path = rootPath,
                ContentQuery = "Name:'*-1'",
                Select = new[] { "Name", "Path", "Type" },
                OrderBy = new[] { "Path" }
            };
            var result = await repository.LoadCollectionAsync(request, cancel).ConfigureAwait(false);
            var contents = result.ToArray();

            // ASSERT
            Assert.AreEqual(1, contents.Length);
            Assert.AreEqual($"{rootPath}/Folder-1", contents[0].Path);
        }

        private async Task CreateStructureForDepthTests(IRepository repository, string rootName, CancellationToken cancel)
        {
            var path = "/Root/Content/" + rootName;
            if (await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false))
                await repository.DeleteContentAsync(path, true, cancel).ConfigureAwait(false);

            var rootFolder = repository.CreateContent("/Root/Content", "Folder", rootName);
            await rootFolder.SaveAsync().ConfigureAwait(false);
            for (var i = 0; i < 3; i++)
            {
                var folder = repository.CreateContent(rootFolder.Path, "Folder", "Folder-" + i);
                await folder.SaveAsync().ConfigureAwait(false);
                for (var j = 0; j < 3; j++)
                {
                    var file = repository.CreateContent(folder.Path, "File", "File-" + j);
                    await file.SaveAsync().ConfigureAwait(false);
                }
            }
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

    }
}
