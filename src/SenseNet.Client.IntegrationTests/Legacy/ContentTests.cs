using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SenseNet.Client.Security;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.IntegrationTests.Legacy
{
    [TestClass]
    public class ContentTests : IntegrationTestBase
    {
        private readonly CancellationToken _cancel = new CancellationToken(false);

        [ClassInitialize]
        public static void ClassInitializer(TestContext context)
        {
            Initializer.InitializeServer(context);
        }

        [TestMethod]
        public async Task Content_Create()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var content = repository.CreateContent("/Root", "Folder", Guid.NewGuid().ToString());
            await content.SaveAsync(_cancel).ConfigureAwait(false);

            content = await repository.LoadContentAsync(content.Id, _cancel).ConfigureAwait(false);
            Assert.IsNotNull(content, "Content was not created.");
        }
        [TestMethod]
        public async Task Content_Modify()
        {
            var cancel = new CancellationTokenSource().Token;
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            ClientContext.Current.Server.IsTrusted = true;

            var content0 = repository.CreateContent("/Root", "Folder", Guid.NewGuid().ToString());
            content0["Index"] = 42;
            await content0.SaveAsync(cancel).ConfigureAwait(false);
            var contentId = content0.Id;

            var content1 = await repository.LoadContentAsync(contentId, cancel).ConfigureAwait(false);
            //var indexBefore = ((JValue)content1["Index"]).Value<int>();
            content1["Index"] = 142;
            await content1.SaveAsync(cancel).ConfigureAwait(false);

            var content2 = await repository.LoadContentAsync(contentId, cancel).ConfigureAwait(false);
            var indexAfter = ((JValue)content2["Index"]).Value<int>();

            Assert.AreEqual(142, indexAfter);
        }

        [TestMethod]
        public async Task Content_Delete_Instance()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var count = 3;
            var paths = new string[count];
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                var content = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
                await content.SaveAsync().ConfigureAwait(false);
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            var contentToDelete = await repository.LoadContentAsync(paths[1], _cancel).ConfigureAwait(false);
            await contentToDelete.DeleteAsync().ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(await repository.LoadContentAsync(paths[0], _cancel).ConfigureAwait(false));
            Assert.IsNull(await repository.LoadContentAsync(paths[1], _cancel).ConfigureAwait(false));
            Assert.IsNotNull(await repository.LoadContentAsync(paths[2], _cancel).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task Content_Delete_ByPath()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var count = 3;
            var paths = new string[count];
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                var content = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
                await content.SaveAsync().ConfigureAwait(false);
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await repository.DeleteContentAsync(paths[1], true, _cancel).ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(await repository.LoadContentAsync(paths[0], _cancel).ConfigureAwait(false));
            Assert.IsNull(await repository.LoadContentAsync(paths[1], _cancel).ConfigureAwait(false));
            Assert.IsNotNull(await repository.LoadContentAsync(paths[2], _cancel).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_Delete_ById()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var count = 3;
            var paths = new string[count];
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                var content = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
                await content.SaveAsync().ConfigureAwait(false);
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await repository.DeleteContentAsync(ids[1], true, _cancel).ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(await repository.LoadContentAsync(paths[0], _cancel).ConfigureAwait(false));
            Assert.IsNull(await repository.LoadContentAsync(paths[1], _cancel).ConfigureAwait(false));
            Assert.IsNotNull(await repository.LoadContentAsync(paths[2], _cancel).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_DeleteBatch_ByPaths()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var count = 5;
            var paths = new string[count];
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                var content = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
                await content.SaveAsync().ConfigureAwait(false);
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await repository.DeleteContentAsync(paths, true, _cancel).ConfigureAwait(false);

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await repository.LoadContentAsync(path, _cancel).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_DeleteBatch_ByIds()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var count = 5;
            var paths = new string[count];
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                var content = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
                await content.SaveAsync().ConfigureAwait(false);
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await repository.DeleteContentAsync(ids, true, _cancel).ConfigureAwait(false);

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await repository.LoadContentAsync(path, _cancel).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_DeleteBatch_ByIdsAndPaths()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var count = 5;
            var paths = new string[count];
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                var content = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
                await content.SaveAsync().ConfigureAwait(false);
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await repository.DeleteContentAsync(new object[] {ids[0], paths[1], ids[2], paths[3], ids[4]}, 
                true, _cancel).ConfigureAwait(false);

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await repository.LoadContentAsync(path, _cancel).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_HasPermission()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var content = await repository.LoadContentAsync(2, _cancel).ConfigureAwait(false);

            // ACTION
            var result = await content.HasPermissionAsync(new []{Permission.Open, Permission.Approve},
                Constants.User.AdminPath, _cancel).ConfigureAwait(false);

            // ASSERT
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task Content_Field_UrlParameters()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var folder = repository.CreateContent("/Root", "SystemFolder", Guid.NewGuid().ToString());
            await folder.SaveAsync(_cancel).ConfigureAwait(false);

            const string url = "https://example.com?a=b&c=d";
            var content = repository.CreateContent(folder.Path, "Link", Guid.NewGuid().ToString());
            content["Url"] = url;
            await content.SaveAsync(_cancel);

            dynamic reloaded = await repository.LoadContentAsync(content.Id, _cancel);

            Assert.AreEqual(url, (string)reloaded.Url);
        }

        [TestMethod]
        public async Task Content_ResponseFieldNames()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            // load a small set of fields
            var userPartial = await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/IMS/BuiltIn/Portal/Admin",
                Select = new[] {"Id", "Name", "Path", "AllRoles" }
            }, _cancel);

            Assert.AreEqual(4, userPartial.FieldNames.Length);
            Assert.IsTrue(userPartial.FieldNames.Contains("AllRoles"));
            Assert.IsFalse(userPartial.FieldNames.Contains("Password"));

            // load all fields
            var user = await repository.LoadContentAsync("/Root/IMS/BuiltIn/Portal/Admin", _cancel);

            Assert.IsTrue(user.FieldNames.Length > 80);
            Assert.IsTrue(user.FieldNames.Contains("AllRoles"));
            Assert.IsTrue(user.FieldNames.Contains("Password"));

            // create new content
            var newContent = repository.CreateContent("/Root/System", "SystemFolder", Guid.NewGuid().ToString());

            Assert.AreEqual(0, newContent.FieldNames.Length);

            // save it: field name list should be updated with all fields
            await newContent.SaveAsync(_cancel);

            Assert.IsTrue(newContent.FieldNames.Length > 60);
        }

        [TestMethod]
        public async Task Content_Exists()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var exists1 = await repository.IsContentExistsAsync("/Root", _cancel);
            Assert.IsTrue(exists1);

            var exists2 = await repository.IsContentExistsAsync("/Root/asdf", _cancel);
            Assert.IsFalse(exists2);
        }

        private static Task<ServerContext> GetServerAsync()
        {
            var services = new ServiceCollection();

            services.AddLogging()
                .AddSenseNetRepository(options =>
                {
                    options.Url = "https://localhost:44362";
                });

            var provider = services.BuildServiceProvider();
            var scf = provider.GetService<IServerContextFactory>();
            return scf.GetServerAsync();
        }
    }
}
