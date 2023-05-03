using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class ContentManagement : ClientIntegrationTestBase
    {
        private class MyContent : Content { public MyContent(IRestCaller rc, ILogger<Content> l) : base(rc, l) { } }
        // ReSharper disable once InconsistentNaming
        private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        // ReSharper disable once InconsistentNaming
        IRepository repository =>
            GetRepositoryCollection(services => { services.RegisterGlobalContentType<MyContent>(); })
                .GetRepositoryAsync("local", cancel).GetAwaiter().GetResult();

        /* ====================================================================================== Create */

        [TestMethod]
        [Description("")]
        public async Task Docs_ContentManagement_Create_Folder()
        {
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/Content/IT", "Folder", "My new folder");
                await content.SaveAsync();

                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/IT/My new folder", content.Path);
                Assert.AreEqual("Folder", content["Type"].ToString());
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/My new folder");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Create a workspace")]
        public async Task Docs_ContentManagement_Create_Workspace()
        {
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/Content", "Workspace", "My workspace");
                await content.SaveAsync();

                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/My workspace", content.Path);
                Assert.AreEqual("Workspace", content["Type"].ToString());
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/My workspace");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Create a document library")]
        public async Task Docs_ContentManagement_Create_DocumentLibrary()
        {
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/Content/IT", "DocumentLibrary", "My Doclib");
                await content.SaveAsync();

                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/IT/My Doclib", content.Path);
                Assert.AreEqual("DocumentLibrary", content["Type"].ToString());
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/My Doclib");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        [TestMethod]
        [Description("Create a user")]
        public async Task Docs_ContentManagement_Create_User()
        {
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/IMS/Public", "User", "alba");
                content["LoginName"] = "alba";
                content["Enable"] = true;
                await content.SaveAsync();

                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/IMS/Public/alba", content.Path);
                Assert.AreEqual("User", content["Type"].ToString());
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/IMS/Public/alba");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        [TestMethod]
        [Description("Creating a content by template")]
        public async Task Docs_ContentManagement_Create_ByTemplate()
        {
            //UNDONE:- the test is not implemented well if the content template is missing.
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/Content/IT", "EventList", "My Calendar",
                    "/Root/ContentTemplates/DemoWorkspace/Demo_Workspace/Calendar");
                content["DisplayName"] = "Calendar";
                content["Index"] = 2;
                await content.SaveAsync();

                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/IT/My Calendar", content.Path);
                Assert.AreEqual("EventList", content["Type"].ToString());
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/My Calendar");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        /* ====================================================================================== Update */

        [TestMethod]
        [Description("Modifying a field of an entity")]
        public async Task Docs_ContentManagement_Update_OneField()
        {
            // ALIGN
            var c = await Content.LoadAsync("/Root/Content/IT");
            c["Index"] = 0;
            await c.SaveAsync();

            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT");
            content["Index"] = 142;
            await content.SaveAsync();

            // ASSERT
            dynamic loaded = await Content.LoadAsync("/Root/Content/IT");
            Assert.AreEqual(142, (int)loaded.Index);
        }
        [TestMethod]
        [Description("Modifying multiple fields at once")]
        public async Task Docs_ContentManagement_Update_MultipleFields()
        {
            try
            {
                // ALIGN
                var c = await Content.LoadAsync("/Root/Content/IT");
                if (c == null)
                {
                    c = await Content.LoadAsync("/Root/Content/NewName");
                    if (c != null)
                    {
                        c["Name"] = "IT";
                        c["Index"] = 0;
                        await c.SaveAsync();
                    }
                }
                else
                {
                    c["Index"] = 0;
                    await c.SaveAsync();
                }

                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT");
                content["Name"] = "NewName";
                content["Index"] = 142;
                await content.SaveAsync();

                // ASSERT
                Assert.IsNull(await Content.LoadAsync("/Root/Content/IT"));
                dynamic loaded = await Content.LoadAsync("/Root/Content/NewName");
                Assert.AreEqual(142, (int)loaded.Index);
            }
            finally
            {
                var content = await Content.LoadAsync("/Root/Content/NewName");
                if (content != null)
                {
                    content["Name"] = "IT";
                    content["Index"] = 142;
                    await content.SaveAsync();
                }
            }
        }
        [TestMethod]
        [Description("Update the value of a date field")]
        public async Task Docs_ContentManagement_Update_DateField()
        {
            try
            {
                // ALIGN
                var c = Content.CreateNew("/Root/Content/IT", "EventList", "Calendar");
                await c.SaveAsync();
                c = Content.CreateNew("/Root/Content/IT/Calendar", "CalendarEvent", "Release");
                c["StartDate"] = new DateTime(2000, 1, 1, 0, 0, 0);
                await c.SaveAsync();

                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT/Calendar/Release");
                content["StartDate"] = new DateTime(2020, 3, 4, 9, 30, 0);
                await content.SaveAsync();

                // ASSERT
                dynamic loaded = await Content.LoadAsync("/Root/Content/IT/Calendar/Release");
                Assert.AreEqual(new DateTime(2020, 3, 4, 9, 30, 0), (DateTime)loaded.StartDate);
            }
            finally
            {
                var c  = await Content.LoadAsync("/Root/Content/IT/Calendar");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Update a choice field")]
        public async Task Docs_ContentManagement_Update_ChoiceField()
        {
            try
            {
                // ALIGN
                var c = Content.CreateNew("/Root/Content/IT", "EventList", "Calendar");
                await c.SaveAsync();
                c = Content.CreateNew("/Root/Content/IT/Calendar", "CalendarEvent", "Release");
                c["StartDate"] = new DateTime(2000, 1, 1, 0, 0, 0);
                await c.SaveAsync();

                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT/Calendar/Release");
                content["EventType"] = new[] { "Demo", "Meeting" };
                await content.SaveAsync();

                // ASSERT
                content = await Content.LoadAsync("/Root/Content/IT/Calendar/Release");
                var  eventTypeValues = ((IEnumerable<object>) content["EventType"]).Select(x => x.ToString()).ToArray();
                Assert.AreEqual(2, eventTypeValues.Length);
                Assert.IsTrue(eventTypeValues.Contains("Demo"));
                Assert.IsTrue(eventTypeValues.Contains("Meeting"));
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Calendar");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Update the value of a reference field 1")]
        public async Task Docs_ContentManagement_Update_SingleReference()
        {
            try
            {
                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT");
                content["Manager"] = 12345;
                await content.SaveAsync();

                // ASSERT
                //UNDONE:-- BUG: SaveAsync need to throw an exception.
                Assert.Inconclusive();
            }
            finally
            {
            }
        }
        [TestMethod]
        [Description("Update the value of a reference field 2")]
        public async Task Docs_ContentManagement_Update_MultiReference()
        {
            try
            {
                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT");
                content["Customers"] = new[] { "/Root/Customer1", "/Root/Customer2" };
                await content.SaveAsync();

                // ASSERT
                //UNDONE:-- BUG: SaveAsync need to throw an exception.
                Assert.Inconclusive();
            }
            finally
            {
            }
        }
        [TestMethod]
        [Description("Setting (resetting) all fields of an entity")]
        public void Docs_ContentManagement_Update_ResetAllAndSetOneField()
        {
            //UNDONE: Do not use this API. Choose any other solution for this problem.
            /*
            // ACTION for doc
            var postData = new Dictionary<string, object>
                { {"Manager", "/Root/IMS/Public/businesscat"} };
            await RESTCaller.PutContentAsync("/Root/Content/IT", postData);
            */

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Delete */

        [TestMethod]
        [Description("Update a choice field")]
        public async Task Docs_ContentManagement_Delete_Single()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");

            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary");
            await content.DeleteAsync();

            // ASSERT
            content = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary");
            Assert.IsNull(content);
        }
        [TestMethod]
        [Description("Delete multiple content at once")]
        public async Task Docs_ContentManagement_Delete_Multiple()
        {
            //UNDONE:- Missing batch delete operation (delete multiple content at once) Content.Delete(params int[] idsToDelete) + Content.Delete(params string[] pathsToDelete)
            // ACTION for doc
            // There is no multiple-delete operation yet.

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Move items to the trash")]
        public async Task Docs_ContentManagement_Delete_ToTrash()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago/100Pages.docx", "File");
            Assert.IsNotNull(await Content.LoadAsync("/Root/Content/IT/Document_Library/Chicago/100Pages.docx"));

            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT/Document_Library/Chicago/100Pages.docx");
            await content.DeleteAsync(false);

            // ASSERT
            Assert.IsNull(await Content.LoadAsync("/Root/Content/IT/Document_Library/Chicago/100Pages.docx"));
        }

        /* ====================================================================================== Upload */

        /// <tab category="content-management" article="upload" example="uploadFile" />
        [TestMethod]
        [Description("Upload a file")]
        public async Task Docs_ContentManagement_Upload_File()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            //UNDONE:- the test is not implemented well because the doc-action contains local filesystem path.
            // ACTION for doc
            /*<doc>*/
            // Both requests are performed in the background
            await using var fileStream = new FileStream(@"D:\Examples\MyFile.txt", FileMode.Open);
            await repository.UploadAsync(
                request: new UploadRequest
                {
                    ParentPath = "/Root/Content/IT/Document_Library",
                    ContentName = "MyFile.txt",
                    ContentType = "File"
                },
                fileStream,
                cancel);
            /*</doc>*/

            // ASSERT
            string? downloadedText = null;
            await repository.DownloadAsync(
                request: new DownloadRequest {Path = "/Root/Content/IT/Document_Library/MyFile.txt"},
                responseProcessor: async (stream, properties) =>
                {
                    using var reader = new StreamReader(stream);
                    downloadedText = await reader.ReadToEndAsync();
                },
                cancel);
            Assert.IsNotNull(downloadedText);
        }

        /// <tab category="content-management" article="upload" example="uploadRawText" />
        [TestMethod]
        [Description("Create a file with raw text")]
        public async Task Docs_ContentManagement_Upload_RawText()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            /*<doc>*/
            var fileText = " *** file text data ***";
            await repository.UploadAsync(request: new UploadRequest
                {
                    ParentPath = "/Root/Content/IT/Document_Library",
                    ContentName = "MyFile.txt",
                    ContentType = "File"
                },
                fileText,
                cancel);
            /*</doc>*/

            // ASSERT
            string? downloadedText = null;
            await repository.DownloadAsync(
                request: new DownloadRequest { Path = "/Root/Content/IT/Document_Library/MyFile.txt" },
                responseProcessor: async (stream, properties) =>
                {
                    using var reader = new StreamReader(stream);
                    downloadedText = await reader.ReadToEndAsync();
                },
                cancel);
            Assert.AreEqual(fileText, downloadedText);
        }

        /// <tab category="content-management" article="upload" example="updateCTD" />
        [TestMethod]
        [Description("Update a CTD")]
        public async Task Docs_ContentManagement_Upload_UpdateCtd()
        {
            var description = "Description " + Guid.NewGuid();
            var ctd = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""MyContentType"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <DisplayName>MyContentType</DisplayName>
  <Description>{description}</Description>
  <Fields></Fields>
</ContentType>
";
            /*<doc>*/
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ctd));
            await repository.UploadAsync(
                request: new UploadRequest
                {
                    ParentPath = "/Root/System/Schema/ContentTypes/GenericContent",
                    ContentName = "MyContentType",
                    ContentType = "ContentType"
                },
                stream,
                cancel);
            /*</doc>*/

            // ASSERT
            string? downloadedText = null;
            await repository.DownloadAsync(
                request: new DownloadRequest { Path = "/Root/System/Schema/ContentTypes/GenericContent/MyContentType" },
                responseProcessor: async (stream, properties) =>
                {
                    using var reader = new StreamReader(stream);
                    downloadedText = await reader.ReadToEndAsync();
                },
                cancel);
            Assert.IsNotNull(downloadedText);
            Assert.IsTrue(downloadedText.Contains($"<Description>{description}</Description>"));
        }

        /// <tab category="content-management" article="upload" example="updateSettings" />
        [TestMethod]
        [Description("Update a Settings file")]
        public async Task Docs_ContentManagement_Upload_UpdateSettings()
        {
            /*<doc>*/
            await repository.UploadAsync(
                request: new UploadRequest
                {
                    ParentPath = "/Root/System/Settings",
                    ContentName = "MyCustom.settings",
                    ContentType = "Settings"
                },
                fileText: "{Key:'Value'}",
                cancel);
            /*</doc>*/

            // ASSERT
            string? downloadedText = null;
            await repository.DownloadAsync(
                request: new DownloadRequest { Path = "/Root/System/Settings/MyCustom.settings" },
                responseProcessor: async (stream, properties) =>
                {
                    using var reader = new StreamReader(stream);
                    downloadedText = await reader.ReadToEndAsync();
                },
                cancel);
            Assert.AreEqual("{Key:'Value'}", downloadedText);
        }

        /// <tab category="content-management" article="upload" example="uploadFileNoChunks" />
        [TestMethod]
        [Description("Upload whole files instead of chunks")]
        public async Task Docs_ContentManagement_Upload_WholeFile()
        {
            //UNDONE:- The Client.Net does not implements this feature.
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive("The Client.Net does not implements this feature.");
        }

        /// <tab category="content-management" article="upload" example="uploadStructure" />
        [TestMethod]
        [Description("Upload a structure")]
        public async Task Docs_ContentManagement_Upload_Structure()
        {
            //UNDONE:- The Client.Net does not implements this feature.
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive("The Client.Net does not implements this feature.");
        }

        /// <tab category="content-management" article="upload" example="uploadResume" />
        [TestMethod]
        [Description("Interrupted uploads")]
        public async Task Docs_ContentManagement_Upload_Interrupted()
        {
            //UNDONE:- The Client.Net does not implements this feature.
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive("The Client.Net does not implements this feature.");
        }

        /* ====================================================================================== Copy or move */

        [TestMethod]
        [Description("Copy a single content")]
        public async Task Docs_ContentManagement_Copy_Single()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var body = @"models=[{""targetPath"": ""/Root/Content/IT/Document_Library/Munich"",
            ""paths"": [""/Root/Content/IT/Document_Library/Chicago/100Pages.pdf""]}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root", "CopyBatch", HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Copy multiple content")]
        public async Task Docs_ContentManagement_Copy_Multiple()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var body = @"models=[{""targetPath"": ""/Root/Content/IT/Document_Library/Munich"",
            ""paths"": [""/Root/Content/IT/Document_Library/Chicago/100Pages.pdf"",
                        ""/Root/Content/IT/Document_Library/Chicago/400Pages.pdf""]}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root", "CopyBatch", HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Move a single content")]
        public async Task Docs_ContentManagement_Move_Single()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Munich", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago/100Pages.pdf", "File");

            try
            {
                // ACTION for doc
                var body = @"models=[{""targetPath"": ""/Root/Content/IT/Document_Library/Munich"",
                ""paths"": [""/Root/Content/IT/Document_Library/Chicago/100Pages.pdf""]}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root", "MoveBatch", HttpMethod.Post, body);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Munich");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Move multiple content")]
        public async Task Docs_ContentManagement_Move_Multiple()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Munich", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago/100Pages.pdf", "File");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago/400Pages.pdf", "File");
            try
            {
                // ACTION for doc
                var body = @"models=[{""targetPath"": ""/Root/Content/IT/Document_Library/Munich"",
                        ""paths"": [""/Root/Content/IT/Document_Library/Chicago/100Pages.pdf"",
                        ""/Root/Content/IT/Document_Library/Chicago/400Pages.pdf""]}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root", "MoveBatch", HttpMethod.Post, body);
                Console.WriteLine(result);

                // ASSERT
                Assert.IsNull(await Content.LoadAsync("/Root/Content/IT/Document_Library/Chicago/100Pages.pdf"));
                Assert.IsNull(await Content.LoadAsync("/Root/Content/IT/Document_Library/Chicago/400Pages.pdf"));
                Assert.IsNotNull(await Content.LoadAsync("/Root/Content/IT/Document_Library/Munich/100Pages.pdf"));
                Assert.IsNotNull(await Content.LoadAsync("/Root/Content/IT/Document_Library/Munich/400Pages.pdf"));
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Munich");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        /* ====================================================================================== Allowed Child Types */

        [TestMethod]
        [Description("Get child types allowed effectively on a content")]
        public async Task Docs_ContentManagement_AllowedChildTypes_GetEffective()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(
                "Root/Content/IT", "EffectiveAllowedChildTypes");
            Console.WriteLine(result);

            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Get child types allowed on a content")]
        public async Task Docs_ContentManagement_AllowedChildTypes_Get()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(
                "Root/Content/IT", "AllowedChildTypes");
            Console.WriteLine(result);

            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Get allowed child types set in the content type definition")]
        public async Task Docs_ContentManagement_AllowedChildTypes_GetFromCtd()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(
                "Root/Content/IT", "GetAllowedChildTypesFromCTD");
            Console.WriteLine(result);

            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Update list of allowed child types on a content")]
        public async Task Docs_ContentManagement_AllowedChildTypes_Update()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            //UNDONE:- the test is not implemented well because the doc-action updates a content that is used in another tests.
            /*
            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT");
            content["AllowedChildTypes"] = new[] { "ImageLibrary", "DocumentLibrary", "TaskList" };
            await content.SaveAsync();
            */
            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Add a type to the allowed childtypes")]
        public async Task Docs_ContentManagement_AllowedChildTypes_Add()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            //UNDONE:- the test is not implemented well because the doc-action updates a content that is used in another tests.
            /*
            // ACTION for doc
            var body = @"models=[{""contentTypes"": [""Task"", ""Image""]}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "AddAllowedChildTypes", HttpMethod.Post, body);
            Console.WriteLine(result);
            */
            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Remove a type from the allowed childtypes")]
        public async Task Docs_ContentManagement_AllowedChildTypes_Remove()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            //UNDONE:- the test is not implemented well because the doc-action updates a content that is used in another tests.
            /*
            // ACTION for doc
            var body = @"models=[{""contentTypes"": [""Task"", ""Image""]}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "RemoveAllowedChildTypes", HttpMethod.Post, body);
            Console.WriteLine(result);
            */
            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Check allowed childtypes")]
        public async Task Docs_ContentManagement_AllowedChildTypes_Check()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "CheckAllowedChildTypesOfFolders");
            Console.WriteLine(result);

            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }

        /* ====================================================================================== Trash */

        [TestMethod]
        [Description("Switch Trash on and off globally")]
        public async Task Docs_ContentManagement_Trash_Off()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            // ACTION for doc
            var body = @"models=[{""IsActive"": false}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Trash", null, HttpMethod.Patch, body);
            Console.WriteLine(result);

            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Disable Trash for a specific container")]
        public async Task Docs_ContentManagement_Trash_Disable()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            // ACTION for doc
            var body = @"models=[{""TrashDisabled"": true}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", null, HttpMethod.Patch, body);
            Console.WriteLine(result);

            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Configure trash options")]
        public async Task Docs_ContentManagement_Trash_Options()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            // ACTION for doc
            var body = @"models=[{""SizeQuota"": 20, ""BagCapacity"": 100, ""MinRetentionTime"": 14}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Trash", null, HttpMethod.Patch, body);
            Console.WriteLine(result);

            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Restore items from the trash")]
        public async Task Docs_ContentManagement_Trash_Restore()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            //UNDONE:- the test is not implemented well because the trashbag is missing.
            /*
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Trash/TrashBag-20200622231439", "Restore", HttpMethod.Post);
            Console.WriteLine(result);
            */
            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Restore items to different destination")]
        public async Task Docs_ContentManagement_Trash_RestoreToDifferentDestination()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            //UNDONE:- the test is not implemented well because the trashbag is missing.
            /*
            // ACTION for doc
            var body = @"models=[{""destination"": ""/Root/target""}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Trash/TrashBag-20200622232234", "Restore", HttpMethod.Post, body);
            Console.WriteLine(result);
            */
            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Restore items with a new name")]
        public async Task Docs_ContentManagement_Trash_RestreAndRenameIncremental()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            //UNDONE:- the test is not implemented well because the trashbag is missing.
            /*
            // ACTION for doc
            var body = @"models=[{""newname"": true}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Trash/TrashBag-20200622233412", "Restore", HttpMethod.Post, body);
            Console.WriteLine(result);
            */
            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Delete items from the trash")]
        public async Task Docs_ContentManagement_Trash_DeleteFromTrash()
        {
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();

            //UNDONE:- the test is not implemented well because the trashbag is missing.
            /*
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Trash/TrashBag-20200622234314", "Delete", HttpMethod.Post);
            Console.WriteLine(result);
            */
            // ASSERT
            var message = Console.GetStringBuilder().ToString();
            Assert.Inconclusive();
        }

        /* ====================================================================================== List Fields */

        [TestMethod]
        [Description("Browsing list fields")]
        public async Task Docs_ContentManagement_ListFields_Select()
        {
            // ACTION for doc
            var result = await RESTCaller.GetResponseJsonAsync(new ODataRequest
            {
                IsCollectionRequest = false,
                Path = "/Root/Content/IT",
                Select = new[] { "%23CustomField" }, // "%23": url encoded "#"
            });
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("List expando fields defined on a specified list")]
        public async Task Docs_ContentManagement_ListFields_Metadata()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library", "$metadata", HttpMethod.Get);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                await Content.DeleteAsync("/Root/Content/IT/Document_Library", true, CancellationToken.None);
            }
        }
        [TestMethod]
        [Description("Add a new list field")]
        public async Task Docs_ContentManagement_ListFields_Add()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            try
            {
                // ACTION for doc
                var body = @"models=[{
                    ""__ContentType"": ""IntegerFieldSetting"",
                    ""Name"": ""MyField1"",
                    ""DisplayName"": ""My Field 1"",
                    ""Compulsory"": true,
                    ""MinValue"": 10}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library", null, HttpMethod.Post, body);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                await Content.DeleteAsync("/Root/Content/IT/Document_Library", true, CancellationToken.None);
            }
        }
        [TestMethod]
        [Description("Edit expando fields 1")]
        public async Task Docs_ContentManagement_ListFields_Edit1()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            try
            {
                // ACTION for doc
                var body = @"models=[{""MinValue"": 5, ""MaxValue"": 20}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library/MyField1", null, HttpMethod.Patch, body);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                await Content.DeleteAsync("/Root/Content/IT/Document_Library", true, CancellationToken.None);
            }
        }
        [TestMethod]
        [Description("Edit expando fields 2")]
        public async Task Docs_ContentManagement_ListFields_Edit2()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            try
            {
                // ACTION for doc
                var body = @"models=[{""MinValue"": 5, ""DisplayName"": ""My field 2""}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library/MyField1", null, HttpMethod.Put, body);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                await Content.DeleteAsync("/Root/Content/IT/Document_Library", true, CancellationToken.None);
            }
        }
        [TestMethod]
        [Description("Edit expando fields 3")]
        public async Task Docs_ContentManagement_ListFields_Edit3()
        {
            //UNDONE:- The server returned an error (HttpStatus: InternalServerError): Operation not found: EditField(Name,MinValue,MaxValue)
            /*
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            try
            {
                // ACTION for doc
                var body = @"models=[{""Name"": ""MyField1"", ""MinValue"": 3, ""MaxValue"": 19}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library", "EditField", HttpMethod.Post, body);
                Console.WriteLine(result);
                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                await Content.DeleteAsync("/Root/Content/IT/Document_Library", true, CancellationToken.None);
            }
            */
        }
        [TestMethod]
        [Description("Remove a list field 1")]
        public async Task Docs_ContentManagement_ListFields_Remove1()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library/MyField1", null, HttpMethod.Delete, null);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                await Content.DeleteAsync("/Root/Content/IT/Document_Library", true, CancellationToken.None);
            }
        }
        [TestMethod]
        [Description("Remove a list field 2")]
        public async Task Docs_ContentManagement_ListFields_Remove2()
        {
            //UNDONE:- The server returned an error (HttpStatus: InternalServerError): Operation not found: DeleteField(Name)
            /*
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            try
            {
                // ACTION for doc
                var body = @"models=[{""Name"": ""MyField1""}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library", "DeleteField", HttpMethod.Post, body);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                await Content.DeleteAsync("/Root/Content/IT/Document_Library", true, CancellationToken.None);
            }
            */
        }

    }
}
