using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.Security;

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
    }
}
