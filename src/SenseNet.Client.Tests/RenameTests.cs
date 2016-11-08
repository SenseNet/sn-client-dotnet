using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class RenameTests
    {
        private static readonly string ROOTPATH = "/Root/_RenameTests";

        [TestMethod]
        public async Task Rename_Folder_01()
        {
            await Tools.EnsurePathAsync(ROOTPATH);

            var parent = Content.CreateNew(ROOTPATH, "Folder", "Parent-" + Guid.NewGuid());
            await parent.SaveAsync();

            parent.Name = parent.Name + "-Renamed";
            await parent.SaveAsync();

            var child = Content.CreateNew(parent.Path, "Folder", "Child");
            await child.SaveAsync();

            parent.Name = parent.Name + "-Renamed2";
            await parent.SaveAsync();

            child = await Content.LoadAsync(child.Id);

            Assert.AreEqual(parent.Path + "/" + child.Name, child.Path);
        }

        [ClassInitialize]
        public static void Cleanup(TestContext context)
        {
            var root = Content.LoadAsync(ROOTPATH).Result;
            if (root != null)
                root.DeleteAsync().Wait();
        }
    }
}
