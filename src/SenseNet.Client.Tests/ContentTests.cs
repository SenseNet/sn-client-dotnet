using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

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
            await content.SaveAsync();

            content = await Content.LoadAsync(content.Id);
            Assert.IsNotNull(content, "Content was not created.");
        }
        [TestMethod]
        public async Task Content_Modify()
        {
            ClientContext.Current.Server.IsTrusted = true;

            var content0 = Content.CreateNew("/Root", "Folder", Guid.NewGuid().ToString());
            content0["Index"] = 42;
            await content0.SaveAsync();
            var contentId = content0.Id;

            var content1 = await Content.LoadAsync(contentId);
            //var indexBefore = ((JValue)content1["Index"]).Value<int>();
            content1["Index"] = 142;
            await content1.SaveAsync();

            var content2 = await Content.LoadAsync(contentId);
            var indexAfter = ((JValue)content2["Index"]).Value<int>();

            Assert.AreEqual(142, indexAfter);
        }
    }
}
