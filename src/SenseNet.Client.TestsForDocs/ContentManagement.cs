using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class ContentManagement : ClientIntegrationTestBase
    {
        /* ====================================================================================== Create */

        [TestMethod]
        [Description("")]
        public async Task Docs_ContentManagement_Create_Folder()
        {
            Content c = null;
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/Content/IT", "Folder", "My new folder");
                await content.SaveAsync();

                // ASSERT
                c = content;
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/IT/My new folder", content.Path);
                Assert.AreEqual("Folder", content["Type"].ToString());
            }
            finally
            {
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Create a workspace")]
        public async Task Docs_ContentManagement_Create_Workspace()
        {
            Content c = null;
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/Content", "Workspace", "My workspace");
                await content.SaveAsync();

                // ASSERT
                c = content;
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/My workspace", content.Path);
                Assert.AreEqual("Workspace", content["Type"].ToString());
            }
            finally
            {
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Create a document library")]
        public async Task Docs_ContentManagement_Create_DocumentLibrary()
        {
            Content c = null;
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/Content/IT", "DocumentLibrary", "My Doclib");
                await content.SaveAsync();

                // ASSERT
                c = content;
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/IT/My Doclib", content.Path);
                Assert.AreEqual("DocumentLibrary", content["Type"].ToString());
            }
            finally
            {
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        [TestMethod]
        [Description("Create a user")]
        public async Task Docs_ContentManagement_Create_User()
        {
            Content c = null;
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/IMS/Public", "User", "alba");
                content["LoginName"] = "alba";
                content["Enable"] = true;
                await content.SaveAsync();

                // ASSERT
                c = content;
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/IMS/Public/alba", content.Path);
                Assert.AreEqual("User", content["Type"].ToString());
            }
            finally
            {
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        [TestMethod]
        [Description("")]
        public async Task Docs_ContentManagement_Create_ByTemplate()
        {
            //UNDONE:- the test is not implemented well if the content template is missing.
            Content c = null;
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/Content/IT", "EventList", "My Calendar",
                    "/Root/ContentTemplates/DemoWorkspace/Demo_Workspace/Calendar");
                content["DisplayName"] = "Calendar";
                content["Index"] = 2;
                await content.SaveAsync();

                // ASSERT
                c = content;
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/IT/My Calendar", content.Path);
                Assert.AreEqual("EventList", content["Type"].ToString());
            }
            finally
            {
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        /* ====================================================================================== Update */

        /* ====================================================================================== Delete */

        /* ====================================================================================== Upload */

        /* ====================================================================================== Copy or move */

        /* ====================================================================================== Allowed Child Types */

        /* ====================================================================================== Trash */

        /* ====================================================================================== List Fields */

    }
}
