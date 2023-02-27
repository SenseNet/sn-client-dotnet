using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests.LegacyIntegrationTests
{
    [TestClass]
    public class RenameTests
    {
        private static readonly string ROOTPATH = "/Root/_RenameTests";

        [TestMethod]
        public async Task Rename_Folder_01()
        {
            await Tools.EnsurePathAsync(ROOTPATH).ConfigureAwait(false);

            var parent = Content.CreateNew(ROOTPATH, "Folder", "Parent-" + Guid.NewGuid());
            await parent.SaveAsync().ConfigureAwait(false);

            parent.Name = parent.Name + "-Renamed";
            await parent.SaveAsync().ConfigureAwait(false);

            var child = Content.CreateNew(parent.Path, "Folder", "Child");
            await child.SaveAsync().ConfigureAwait(false);

            parent.Name = parent.Name + "-Renamed2";
            await parent.SaveAsync().ConfigureAwait(false);

            child = await Content.LoadAsync(child.Id).ConfigureAwait(false);

            Assert.AreEqual(parent.Path + "/" + child.Name, child.Path);
        }

        [ClassInitialize]
        public static void Cleanup(TestContext context)
        {
            Initializer.InitializeServer();

            var root = Content.LoadAsync(ROOTPATH).Result;
            root?.DeleteAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
