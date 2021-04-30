using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Diagnostics;

namespace SenseNet.Client.TestsForDocs.Infrastructure
{
    [TestClass]
    public class ClientIntegrationTestBase
    {
        public static readonly string Url = "https://localhost:44362";

        [AssemblyInitialize]
        public static void InititalizeAllTests(TestContext testContext)
        {
            ClientContext.Current.AddServer(new ServerContext
            {
                Url = Url,
                Username = "builtin\\admin",
                Password = "admin"
            });

            EnsureBasicStructureAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            SnTrace.Custom.Enabled = true;
            SnTrace.Test.Enabled = true;
        }
        private static async Task EnsureBasicStructureAsync()
        {
            var c = await Content.LoadAsync("/Root/Content");
            if (c == null)
            {
                c = Content.CreateNew("/Root", "Folder", "Content");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content", "Workspace", "IT");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT", "DocumentLibrary", "Document_Library");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Chicago");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT/Document_Library", "Folder", "Chicago");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT/Document_Library", "Folder", "Calgary");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT/Document_Library/Calgary", "File", "BusinessPlan.docx");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Munich");
            if (c == null)
            {
                c = Content.CreateNew("/Root/Content/IT/Document_Library", "Folder", "Munich");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/IMS/Public");
            if (c == null)
            {
                c = Content.CreateNew("/Root/IMS", "Domain", "Public");
                await c.SaveAsync();
            }
            c = await Content.LoadAsync("/Root/IMS/Public/Editors");
            if (c == null)
            {
                c = Content.CreateNew("/Root/IMS/Public", "Group", "Editors");
                await c.SaveAsync();
            }
        }

        [TestInitialize]
        public void InitializeTest()
        {
            var ctx = ClientContext.Current;
            ctx.RemoveServers(ctx.Servers);
            ctx.AddServer(new ServerContext
            {
                Url = Url,
                Username = "builtin\\admin",
                Password = "admin"
            });
        }
    }
}
