using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace SenseNet.Client.Tests
{
    internal static class Extensions
    {
        public static ConfiguredTaskAwaitable C(this Task task)
        {
            return task.ConfigureAwait(false);
        }
        public static ConfiguredTaskAwaitable<T> C<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false);
        }
    }

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
            await content.SaveAsync().C();

            content = await Content.LoadAsync(content.Id).C();
            Assert.IsNotNull(content, "Content was not created.");
        }
        [TestMethod]
        public async Task Content_Modify()
        {
            ClientContext.Current.Server.IsTrusted = true;

            var content0 = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
            content0["Index"] = 42;
            await content0.SaveAsync().C();
            var contentId = content0.Id;

            var content1 = await Content.LoadAsync(contentId).C();
            //var indexBefore = ((JValue)content1["Index"]).Value<int>();
            content1["Index"] = 142;
            await content1.SaveAsync().C();

            var content2 = await Content.LoadAsync(contentId).C();
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
                await content.SaveAsync().C();
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            var contentToDelete = await Content.LoadAsync(paths[1]).C();
            await contentToDelete.DeleteAsync().C();

            // ASSERT
            Assert.IsNotNull(await Content.LoadAsync(paths[0]).C());
            Assert.IsNull(await Content.LoadAsync(paths[1]).C());
            Assert.IsNotNull(await Content.LoadAsync(paths[2]).C());
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
                await content.SaveAsync().C();
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await Content.DeleteAsync(paths[1], true, CancellationToken.None).C();

            // ASSERT
            Assert.IsNotNull(await Content.LoadAsync(paths[0]).C());
            Assert.IsNull(await Content.LoadAsync(paths[1]).C());
            Assert.IsNotNull(await Content.LoadAsync(paths[2]).C());
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
                await content.SaveAsync().C();
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await Content.DeleteAsync(ids[1], true, CancellationToken.None).C();

            // ASSERT
            Assert.IsNotNull(await Content.LoadAsync(paths[0]).C());
            Assert.IsNull(await Content.LoadAsync(paths[1]).C());
            Assert.IsNotNull(await Content.LoadAsync(paths[2]).C());
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
                await content.SaveAsync().C();
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await Content.DeleteAsync(paths, true, CancellationToken.None).C();

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await Content.LoadAsync(path).C());
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
                await content.SaveAsync().C();
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await Content.DeleteAsync(ids, true, CancellationToken.None).C();

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await Content.LoadAsync(path).C());
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
                await content.SaveAsync().C();
                paths[i] = content.Path;
                ids[i] = content.Id;
            }

            // ACTION
            await Content.DeleteAsync(new object[] {ids[0], paths[1], ids[2], paths[3], ids[4]}, 
                true, CancellationToken.None).C();

            // ASSERT
            foreach (var path in paths)
                Assert.IsNull(await Content.LoadAsync(path).C());
        }
    }
}
