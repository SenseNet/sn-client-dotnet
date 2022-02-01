using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.Security;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class ContentTests
    {
        [ClassInitialize]
        public static void ClassInitializer(TestContext _)
        {
            Initializer.InitializeServer();
        }

        [TestMethod]
        public async Task Content_Create()
        {
            var content = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
            await content.SaveAsync().ConfigureAwait(false);

            content = await Content.LoadAsync(content.Id).ConfigureAwait(false);
            Assert.IsNotNull(content, "Content was not created.");
        }
        [TestMethod]
        public async Task Content_Modify()
        {
            ClientContext.Current.Server.IsTrusted = true;

            var content0 = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
            content0["Index"] = 42;
            await content0.SaveAsync().ConfigureAwait(false);
            var contentId = content0.Id;

            var content1 = await Content.LoadAsync(contentId).ConfigureAwait(false);
            //var indexBefore = ((JValue)content1["Index"]).Value<int>();
            content1["Index"] = 142;
            await content1.SaveAsync().ConfigureAwait(false);

            var content2 = await Content.LoadAsync(contentId).ConfigureAwait(false);
            var indexAfter = ((JValue)content2["Index"]).Value<int>();

            Assert.AreEqual(142, indexAfter);
        }

        [TestMethod]
        public async Task Content_Delete_Instance()
        {
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
            var contentToDelete = await Content.LoadAsync(paths[1]).ConfigureAwait(false);
            await contentToDelete.DeleteAsync().ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(await Content.LoadAsync(paths[0]).ConfigureAwait(false));
            Assert.IsNull(await Content.LoadAsync(paths[1]).ConfigureAwait(false));
            Assert.IsNotNull(await Content.LoadAsync(paths[2]).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task Content_Delete_ByPath()
        {
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
            await Content.DeleteAsync(paths[1], true, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(await Content.LoadAsync(paths[0]).ConfigureAwait(false));
            Assert.IsNull(await Content.LoadAsync(paths[1]).ConfigureAwait(false));
            Assert.IsNotNull(await Content.LoadAsync(paths[2]).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_Delete_ById()
        {
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
            await Content.DeleteAsync(ids[1], true, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(await Content.LoadAsync(paths[0]).ConfigureAwait(false));
            Assert.IsNull(await Content.LoadAsync(paths[1]).ConfigureAwait(false));
            Assert.IsNotNull(await Content.LoadAsync(paths[2]).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_DeleteBatch_ByPaths()
        {
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
            await Content.DeleteAsync(paths, true, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await Content.LoadAsync(path).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_DeleteBatch_ByIds()
        {
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
            await Content.DeleteAsync(ids, true, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await Content.LoadAsync(path).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_DeleteBatch_ByIdsAndPaths()
        {
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
            await Content.DeleteAsync(new object[] {ids[0], paths[1], ids[2], paths[3], ids[4]}, 
                true, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await Content.LoadAsync(path).ConfigureAwait(false));
        }
        [TestMethod]
        public async Task Content_HasPermission()
        {
            var content = await Content.LoadAsync(2).ConfigureAwait(false);

            // ACTION
            var result = await content.HasPermissionAsync(new []{Permission.Open, Permission.Approve},
                Constants.User.AdminPath).ConfigureAwait(false);

            // ASSERT
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task Content_Field_UrlParameters()
        {
            var folder = Content.CreateNew("/Root", "SystemFolder", Guid.NewGuid().ToString());
            await folder.SaveAsync().ConfigureAwait(false);

            const string url = "https://example.com?a=b&c=d";
            var content = Content.CreateNew(folder.Path, "Link", Guid.NewGuid().ToString());
            content["Url"] = url;
            await content.SaveAsync();

            dynamic reloaded = await Content.LoadAsync(content.Id);

            Assert.AreEqual(url, (string)reloaded.Url);
        }

        [TestMethod]
        public async Task Content_ResponseFieldNames()
        {
            // load a small set of fields
            var userPartial = await Content.LoadAsync(new ODataRequest
            {
                Path = "/Root/IMS/BuiltIn/Portal/Admin",
                Select = new[] {"Id", "Name", "Path", "AllRoles" }
            });

            Assert.AreEqual(4, userPartial.FieldNames.Length);
            Assert.IsTrue(userPartial.FieldNames.Contains("AllRoles"));
            Assert.IsFalse(userPartial.FieldNames.Contains("Password"));

            // load all fields
            var user = await Content.LoadAsync("/Root/IMS/BuiltIn/Portal/Admin");

            Assert.IsTrue(user.FieldNames.Length > 80);
            Assert.IsTrue(user.FieldNames.Contains("AllRoles"));
            Assert.IsTrue(user.FieldNames.Contains("Password"));

            // create new content
            var newContent = Content.CreateNew("/Root/System", "SystemFolder", Guid.NewGuid().ToString());

            Assert.AreEqual(0, newContent.FieldNames.Length);

            // save it: field name list should be updated with all fields
            await newContent.SaveAsync();

            Assert.IsTrue(newContent.FieldNames.Length > 60);
        }

        [TestMethod]
        public async Task Content_Exists()
        {
            var server = await GetServerAsync();

            var exists1 = await Content.ExistsAsync("/Root", server);
            Assert.IsTrue(exists1);

            var exists2 = await Content.ExistsAsync("/Root/asdf", server);
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
