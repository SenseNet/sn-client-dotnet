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
using Newtonsoft.Json.Linq;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class ContentManagement : ClientIntegrationTestBase
    {
        private class MyContent : Content { public MyContent(IRestCaller rc, ILogger<Content> l) : base(rc, l) { } }
        // ReSharper disable once InconsistentNaming
        private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(1000)).Token;
        // ReSharper disable once InconsistentNaming
        IRepository repository =>
            GetRepositoryCollection(services => { services.RegisterGlobalContentType<MyContent>(); })
                .GetRepositoryAsync("local", cancel).GetAwaiter().GetResult();

        /* ====================================================================================== Create */

        /// <tab category="content-management" article="create" example="create" />
        [TestMethod]
        public async Task Docs2_ContentManagement_Create_Folder()
        {
            try
            {
                /*<doc>*/
                var content = repository.CreateContent("/Root/Content/Cars", "Folder", "New cars");
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content('Cars')
                models=[{"Name":"New cars","__ContentType":"Folder"}] 
                */

                // ASSERT
                var folder = content as Folder;
                Assert.IsNotNull(folder);
                Assert.IsTrue(folder.Id > 0);
                Assert.AreEqual("/Root/Content/Cars/New cars", folder.Path);
                Assert.AreEqual("Folder", content["Type"].ToString());
                Assert.AreEqual("Folder", folder.Type);
                folder = await repository.LoadContentAsync<Folder>("/Root/Content/Cars/New cars", cancel);
                Assert.IsNotNull(folder);
                Assert.IsTrue(folder.Id > 0);
            }
            finally
            {
                var c = await repository.LoadContentAsync("/Root/Content/Cars/New cars", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /// <tab category="content-management" article="create" example="createWs" />
        [TestMethod]
        [Description("Create a workspace")]
        public async Task Docs2_ContentManagement_Create_Workspace()
        {
            try
            {
                /*<doc>*/
                var content = repository.CreateContent("/Root/Content", "Workspace", "My workspace");
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root('Content')
                models=[{"Name":"My workspace","__ContentType":"Workspace"}] 
                */

                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/My workspace", content.Path);
                Assert.AreEqual("Workspace", content["Type"].ToString());
            }
            finally
            {
                var c = await repository.LoadContentAsync("/Root/Content/My workspace", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /// <tab category="content-management" article="create" example="createDocLib" />
        [TestMethod]
        [Description("Create a document library")]
        public async Task Docs2_ContentManagement_Create_DocumentLibrary()
        {
            try
            {
                /*<doc>*/
                var content = repository.CreateContent("/Root/Content", "DocumentLibrary", "My Documents");
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root('Content')
                models=[{"Name":"My Documents","__ContentType":"DocumentLibrary"}] 
                */
                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/My Documents", content.Path);
                Assert.AreEqual("DocumentLibrary", content["Type"].ToString());
            }
            finally
            {
                var c = await repository.LoadContentAsync("/Root/Content/My Documents", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /// <tab category="content-management" article="create" example="createUser" />
        [TestMethod]
        [Description("Create a user")]
        public async Task Docs2_ContentManagement_Create_User()
        {
            try
            {
                /*<doc>*/
                var content = repository.CreateContent("/Root/IMS/Public", "User", "jsmith");
                content["LoginName"] = "jsmith";
                content["Email"] = "jsmith@example.com";
                content["Password"] = "I8rRp2c9P0HJZENZcvlz";
                content["FullName"] = "John Smith";
                content["Enabled"] = true;
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/IMS('Public')
                models=[{
                  "Name":"jsmith",
                  "__ContentType":"User",
                  "LoginName":"jsmith",
                  "Email":"jsmith@example.com",
                  "Password":"I8rRp2c9P0HJZENZcvlz",
                  "FullName":"John Smith",
                  "Enabled":true
                }] 
                */

                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/IMS/Public/jsmith", content.Path);
                Assert.AreEqual("User", content["Type"].ToString());
                var user = await repository.LoadContentAsync<User>(content.Id, cancel);
                Assert.AreEqual("/Root/IMS/Public/jsmith", user.Path);
                Assert.AreEqual("jsmith", user.LoginName);
                Assert.AreEqual("jsmith@example.com", user.Email);
                Assert.AreEqual(null, user.Password);
                Assert.AreEqual("John Smith", user.FullName);
                Assert.AreEqual(true, user.Enabled);
            }
            finally
            {
                var c = await repository.LoadContentAsync("/Root/IMS/Public/jsmith", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /// <tab category="content-management" article="create" example="createByTemplate" />
        //UNDONE:Docs2: - the test is not implemented well if the content template is missing.
        [TestMethod]
        [Description("Creating a content by template")]
        public async Task Docs2_ContentManagement_Create_ByTemplate()
        {
            try
            {
                /*<doc>*/
                var content = repository.CreateContentByTemplate("/Root/Content", "EventList", "My Calendar",
                    "/Root/ContentTemplates/DemoWorkspace/Demo_Workspace/Calendar");
                content["DisplayName"] = "Calendar";
                content["Index"] = 2;
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root('Content')
                models=[{
                  "Name":"My Calendar",
                  "__ContentType":"EventList",
                  "__ContentTemplate":"/Root/ContentTemplates/DemoWorkspace/Demo_Workspace/Calendar",
                  "DisplayName":"Calendar",
                  "Index":2
                }] 
                */

                // ASSERT
                Assert.IsNotNull(content);
                Assert.AreEqual("/Root/Content/My Calendar", content.Path);
                Assert.AreEqual("EventList", content["Type"].ToString());
            }
            finally
            {
                var c = await repository.LoadContentAsync("/Root/Content/My Calendar", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /* ====================================================================================== Update */

        /// <tab category="content-management" article="update" example="updatePatch" />
        [TestMethod]
        [Description("Modifying a field of an entity")]
        public async Task Docs2_ContentManagement_Update_OneField()
        {
            var temp = await repository.LoadContentAsync("/Root/Content/Cars/AAXX123", cancel);
            var originalColor = temp["Color"].ToString();

            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Cars/AAXX123", cancel);
                content["Color"] = "Rosso Corsa";
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Content/Cars/AAXX123
                models=[{"Color":"Rosso Corsa"}]
                */

                // ASSERT
                dynamic loaded = await repository.LoadContentAsync("/Root/Content/Cars/AAXX123", cancel);
                Assert.AreEqual("Rosso Corsa", loaded["Color"].ToString());
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content/Cars/AAXX123", cancel);
                content["Color"] = originalColor;
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="content-management" article="update" example="updateMultipleFields" />
        [TestMethod]
        [Description("Modifying multiple fields at once")]
        public async Task Docs2_ContentManagement_Update_MultipleFields()
        {
            var temp = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
            var originalModel = temp["Model"].ToString();
            var originalColor = temp["Color"].ToString();

            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                content["Model"] = "126p";
                content["Color"] = "Dark red";
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Content/Cars/OT1234
                models=[{
                  "Model":"126p",
                  "Color":"Dark red"
                }]
                */

                // ASSERT
                dynamic loaded = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                Assert.AreEqual("126p", loaded["Model"].ToString());
                Assert.AreEqual("Dark red", loaded["Color"].ToString());
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                content["Model"] = originalModel;
                content["Color"] = originalColor;
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="content-management" article="update" example="updateDate" />
        [TestMethod]
        [Description("Update the value of a date field")]
        public async Task Docs2_ContentManagement_Update_DateField()
        {
            var temp = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
            var originalStartingDate = ((JValue) temp["StartingDate"]).Value<DateTime>();

            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                content["StartingDate"] = DateTime.Parse("1986-11-21");
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Content/Cars/OT1234
                models=[{"StartingDate":"1986-11-21T00:00:00"}] 
                */

                // ASSERT
                dynamic loaded = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                Assert.AreEqual("1986. 11. 21. 0:00:00", loaded["StartingDate"].ToString());
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                content["StartingDate"] = originalStartingDate;
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="content-management" article="update" example="updateChoice" />
        [TestMethod]
        [Description("Update a choice field")]
        public async Task Docs2_ContentManagement_Update_ChoiceField()
        {
            var temp = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
            var originalStyle = ((JArray) temp["Style"]).Select(x => x.ToString()).ToArray();
            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                content["Style"] = new[] { "Roadster" };
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Content/Cars/OT1234
                models=[{"Style":["Roadster"]}]
                */

                // ASSERT
                content = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                var  eventTypeValues = ((IEnumerable<object>) content["Style"]).Select(x => x.ToString()).ToArray();
                Assert.AreEqual(1, eventTypeValues.Length);
                Assert.IsTrue(eventTypeValues.Contains("Roadster"));
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                content["Style"] = originalStyle;
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="content-management" article="update" example="updateReference" />
        [TestMethod]
        [Description("Update the value of a reference field 1")]
        public async Task Docs2_ContentManagement_Update_SingleReference()
        {
            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content", cancel);
                content["Manager"] = 12345; // Id of the referenced content
                /*</doc>*/
                content["Manager"] = 1;
                /*<doc>*/
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Content
                models=[{"Manager":1}] 
                */

                // ASSERT
                dynamic loaded = await repository.LoadContentAsync(new LoadContentRequest
                {
                    Path = "/Root/Content",
                    Expand = new []{"Manager"}
                }, cancel);
                Assert.AreEqual("1", loaded.Manager.Id.ToString());
            }
            finally
            {
            }
        }

        /// <tab category="content-management" article="update" example="updateReferenceMultiple" />
        [TestMethod]
        [Description("Update the value of a reference field 2")]
        public async Task Docs2_ContentManagement_Update_MultiReference()
        {
            try
            {
                /*<doc>*/
                var user1 = await repository.LoadContentAsync("/Root/IMS/Public/etaylor", cancel);
                var user2 = await repository.LoadContentAsync("/Root/IMS/Public/jjohnson", cancel);
                var editors = await repository.LoadContentAsync<Group>("/Root/IMS/Public/Editors", cancel);
                editors.Members = new[] { user1, user2};
                await editors.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/content(1399)
                models=[{"Members":["/Root/IMS/Public/etaylor", "/Root/IMS/Public/jjohnson"]}]
                */

                // ASSERT
                var loaded = await repository.LoadContentAsync<Group>(new LoadContentRequest
                {
                    Path = "/Root/IMS/Public/Editors",
                    Expand = new[] {"Members"},
                    Select = new[] { "Id", "Name", "Type", "Path", "Members/Id", "Members/Name", "Members/Type", "Members/Path" },
                }, cancel);
                var members = loaded.Members?.OrderBy(x=>x.Name).ToArray();
                Assert.AreEqual(2, members.Length);
                Assert.AreEqual("etaylor, jjohnson", string.Join(", ", members.Select(x => x.Name)));
            }
            finally
            {
            }
        }

        /// <tab category="content-management" article="update" example="updatePut" />
        //UNDONE:Docs2:-- Do not use this API. Choose any other solution for this problem.
        [TestMethod]
        [Description("Setting (resetting) all fields of an entity")]
        public async Task Docs2_ContentManagement_Update_ResetAllAndSetOneField()
        {
            var backup = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
            try
            {
                /*<doc>*/
                var postData = new Dictionary<string, object>
                {
                    {"DisplayName", "Fiat 126"},
                    {"Color", "Yellow"}
                };
                var content = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                await content.ResetAsync(postData, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PUT https://localhost:44362/OData.svc/Root/Content/Cars('OT1234')
                models=[{
                  "DisplayName":"Fiat 126",
                  "Color":"Yellow"
                }]
                */

                // ASSERT
                var loaded = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                Assert.AreEqual("OT1234", loaded.Name);
                Assert.AreEqual("Fiat 126", loaded.DisplayName);
                Assert.AreEqual("Yellow", loaded["Color"].ToString());
                Assert.AreEqual(string.Empty, loaded["Model"].ToString());
                Assert.AreEqual("[]", loaded["Style"].ToString());
                Assert.AreEqual("0001-01-01 00:00:00", ((JValue) loaded["StartingDate"]).Value<DateTime>().ToString("yyyy-MM-dd HH:mm:ss"));
                Assert.AreEqual(string.Empty, loaded["EngineSize"].ToString());
                Assert.AreEqual(string.Empty, loaded["Power"].ToString());
                Assert.AreEqual("0", loaded["Price"].ToString());
            }
            finally
            {
                var reloaded = await repository.LoadContentAsync("/Root/Content/Cars/OT1234", cancel);
                reloaded["DisplayName"] = backup["DisplayName"];
                reloaded["Make"] = backup["Make"];
                reloaded["Model"] = backup["Model"];
                reloaded["Style"] = backup["Style"];
                reloaded["StartingDate"] = backup["StartingDate"];
                reloaded["Color"] = backup["Color"];
                reloaded["EngineSize"] = backup["EngineSize"];
                reloaded["Power"] = backup["Power"];
                reloaded["Price"] = backup["Price"];
                await reloaded.SaveAsync(cancel);
            }
        }

        /* ====================================================================================== Delete */

        /// <tab category = "content-management" article="delete" example="deleteContent" />
        [TestMethod]
        [Description("Update a choice field")]
        public async Task Docs2_ContentManagement_Delete_Single()
        {
            await EnsureContentAsync("/Root/Content/Cars/AAXY123", "Car", repository, cancel);

            /*<doc>*/
            await repository.DeleteContentAsync("/Root/Content/Cars/AAXY123", true, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            POST https://localhost:44362/OData.svc/('Root')/DeleteBatch
            models=[{
              "permanent":true,
              "paths":["/Root/Content/Cars/AAXY123"]}] 
            */

            // ASSERT
            var content = await repository.LoadContentAsync("/Root/Content/Cars/AAXY123", cancel);
            Assert.IsNull(content);
        }

        /// <tab category="content-management" article="delete" example="deleteMultipleContent" />
        [TestMethod]
        [Description("Delete multiple content at once")]
        public async Task Docs2_ContentManagement_Delete_Multiple()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/Cars/AAXY123", "Car", repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/AAXY852", "Car", repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/AAXY246", "Car", repository, cancel);

            /*<doc>*/
            await repository.DeleteContentAsync(new[]
            {
                "/Root/Content/Cars/AAXY123",
                "/Root/Content/Cars/AAXY852",
                "/Root/Content/Cars/AAXY246"
            }, true, cancel);
            /*</doc>*/
            /* RAW REQUEST:[17468] SnTrace: 12 Test Pf:1    >>>> RAW REQUEST:
            POST https://localhost:44362/OData.svc/('Root')/DeleteBatch?metadata=no
            models=[{
              "permanent":true,
              "paths":[
                "/Root/Content/Cars/AAXY123",
                "/Root/Content/Cars/AAXY852",
                "/Root/Content/Cars/AAXY246"
              ]
            }] 
            */

            // ASSERT
            Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/Cars/AAXY123", cancel));
            Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/Cars/AAXY852", cancel));
            Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/Cars/AAXY246", cancel));
        }

        /// <tab category="content-management" article="delete" example="moveTotheTrash" />
        [TestMethod]
        [Description("Move items to the trash")]
        public async Task Docs2_ContentManagement_Delete_ToTrash()
        {
            try
            {
                // ALIGN
                await EnsureContentAsync("/Root/Content/Cars/AAXY123", "Car", repository, cancel);

                /*<doc>*/
                await repository.DeleteContentAsync("/Root/Content/Cars/AAXY123", false, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/('Root')/DeleteBatch
                models=[{
                  "permanent":false,
                  "paths":["/Root/Content/Cars/AAXY123"]
                }] 
                */

                // ASSERT
                var content = await repository.LoadContentAsync("/Root/Content/Cars/AAXY123", cancel);
                Assert.IsNull(content);
            }
            finally
            {
                await EmptyTrash(repository, cancel);
            }
        }

        /* ====================================================================================== Upload */

        /// <tab category="content-management" article="upload" example="uploadFile" />
        //UNDONE:Docs2: the test is not implemented well because the doc-action contains local filesystem path.
        [TestMethod]
        [Description("Upload a file")]
        public async Task Docs2_ContentManagement_Upload_File()
        {
            try
            {
                /*<doc>*/
                await using var fileStream = new FileStream(@"D:\Examples\MyFile.txt", FileMode.Open);
                await repository.UploadAsync(
                    request: new UploadRequest
                    {
                        ParentPath = "/Root/Content/Documents",
                        ContentName = "MyFile.txt",
                        ContentType = "File"
                    },
                    fileStream,
                    cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content('Documents')/Upload

                -----------------------------8dc86bc2e2dc8b5
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=FileName
                
                MyFile.txt
                -----------------------------8dc86bc2e2dc8b5
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=ContentType
                
                File
                -----------------------------8dc86bc2e2dc8b5
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=PropertyName
                
                Binary
                -----------------------------8dc86bc2e2dc8b5
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=UseChunk
                
                False
                -----------------------------8dc86bc2e2dc8b5
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=Overwrite
                
                True
                -----------------------------8dc86bc2e2dc8b5
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=FileLength
                
                21
                -----------------------------8dc86bc2e2dc8b5
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=ChunkToken
                
                0**True*True
                -----------------------------8dc86bc2e2dc8b5
                Content-Disposition: form-data; name="files[]"; filename=MyFile.txt; filename*=utf-8''MyFile.txt
                
                Content of MyFile.txt
                -----------------------------8dc86bc2e2dc8b5--
                */

                // ASSERT
                string? downloadedText = null;
                await repository.DownloadAsync(
                    request: new DownloadRequest { Path = "/Root/Content/Documents/MyFile.txt" },
                    responseProcessor: async (stream, properties) =>
                    {
                        using var reader = new StreamReader(stream);
                        downloadedText = await reader.ReadToEndAsync();
                    },
                    cancel);
                Assert.AreEqual("Content of MyFile.txt", downloadedText);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/MyFile.txt", true, cancel);
            }
        }

        /// <tab category="content-management" article="upload" example="uploadRawText" />
        [TestMethod]
        [Description("Create a file with raw text")]
        public async Task Docs2_ContentManagement_Upload_RawText()
        {
            try
            {
                /*<doc>*/
                var fileText = "**** file text data ****";
                await repository.UploadAsync(request: new UploadRequest
                {
                    ParentPath = "/Root/Content/Documents",
                    ContentName = "MyFile.txt",
                    ContentType = "File"
                },
                fileText,
                cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content('Documents')/Upload?metadata=no
                models=[{
                  "FileName":"MyFile.txt",
                  "ContentType":"File",
                  "PropertyName":"Binary",
                  "UseChunk":false,
                  "Overwrite":true,
                  "FileLength":24,
                  "FileText":"**** file text data ****"
                }] 
                */

                // ASSERT
                string? downloadedText = null;
                await repository.DownloadAsync(
                    request: new DownloadRequest { Path = "/Root/Content/Documents/MyFile.txt" },
                    responseProcessor: async (stream, properties) =>
                    {
                        using var reader = new StreamReader(stream);
                        downloadedText = await reader.ReadToEndAsync();
                    },
                    cancel);
                Assert.AreEqual(fileText, downloadedText);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/MyFile.txt", true, cancel);
            }
        }

        /// <tab category="content-management" article="upload" example="updateCTD" />
        [TestMethod]
        [Description("Update a CTD")]
        public async Task Docs2_ContentManagement_Upload_UpdateCtd()
        {
            try
            {
                /*<doc>*/
                var ctd = @"<?xml version='1.0' encoding='utf-8'?>
                    <ContentType name=""MyContentType"" parentType=""GenericContent""
                          handler=""SenseNet.ContentRepository.GenericContent""
                          xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
                      <DisplayName>MyContentType</DisplayName>
                      <Fields></Fields>
                    </ContentType>
                    ";
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
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/System/Schema/ContentTypes('GenericContent')/Upload

                -----------------------------8dc86cabae328e4
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=FileName

                MyContentType
                -----------------------------8dc86cabae328e4
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=ContentType

                ContentType
                -----------------------------8dc86cabae328e4
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=PropertyName

                Binary
                -----------------------------8dc86cabae328e4
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=UseChunk

                False
                -----------------------------8dc86cabae328e4
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=Overwrite

                True
                -----------------------------8dc86cabae328e4
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=FileLength

                476
                -----------------------------8dc86cabae328e4
                Content-Type: text/plain; charset=utf-8
                Content-Disposition: form-data; name=ChunkToken

                0**True*True
                -----------------------------8dc86cabae328e4
                Content-Disposition: form-data; name="files[]"; filename=MyContentType; filename*=utf-8''MyContentType

                <?xml version='1.0' encoding='utf-8'?>
                <ContentType name="MyContentType" parentType="GenericContent"
                      handler="SenseNet.ContentRepository.GenericContent"
                      xmlns="http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition">
                  <DisplayName>MyContentType</DisplayName>
                  <Fields></Fields>
                </ContentType>

                -----------------------------8dc86cabae328e4--
                */

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
                Assert.IsTrue(downloadedText.Contains("<ContentType name=\"MyContentType\" parentType=\"GenericContent\""));
                Assert.IsTrue(downloadedText.Contains("<DisplayName>MyContentType</DisplayName>"));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/System/Schema/ContentTypes/GenericContent/MyContentType",
                    true, cancel);
            }
        }

        /// <tab category="content-management" article="upload" example="updateSettings" />
        [TestMethod]
        [Description("Update a Settings file")]
        public async Task Docs2_ContentManagement_Upload_UpdateSettings()
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
            /* RAW REQUEST:
            POST https://localhost:44362/OData.svc/Root/System('Settings')/Upload?metadata=no
            models=[{
              "FileName":"MyCustom.settings",
              "ContentType":"Settings",
              "PropertyName":"Binary",
              "UseChunk":false,
              "Overwrite":true,
              "FileLength":13,
              "FileText":"{Key:'Value'}"
            }] 
            */

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
        public void Docs2_ContentManagement_Upload_WholeFile()
        {
            /*<doc>*/
            // The SenseNet.Client does not implement this feature on the .NET platform..
            /*</doc>*/
        }

        /// <tab category="content-management" article="upload" example="uploadStructure" />
        [TestMethod]
        [Description("Upload a structure")]
        public void Docs2_ContentManagement_Upload_Structure()
        {
            /*<doc>*/
            // The SenseNet.Client does not implement this feature on the .NET platform..
            /*</doc>*/
        }

        /// <tab category="content-management" article="upload" example="uploadResume" />
        [TestMethod]
        [Description("Interrupted uploads")]
        public void Docs2_ContentManagement_Upload_Interrupted()
        {
            /*<doc>*/
            // The SenseNet.Client does not implement this feature on the .NET platform..
            /*</doc>*/
        }

        /* ====================================================================================== Copy or move */

        /// <tab category="content-management" article="copy-move" example="copyContent" />
        [TestMethod]
        [Description("Copy a single content")]
        public async Task Docs2_ContentManagement_Copy_Single()
        {
            try
            {
                /*<doc>*/
                var result = await repository.InvokeActionAsync<string>(new OperationRequest
                {
                    Path = "/Root",
                    OperationName = "CopyBatch",
                    PostData = new
                    {
                        targetPath = "/Root/Content/Cars/Backup",
                        paths = new[]{"/Root/Content/Cars/AAKE452"}
                    }
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/('Root')/CopyBatch
                {
                  "targetPath":"/Root/Content/Cars/Backup",
                  "paths":[
                    "/Root/Content/Cars/AAKE452"
                  ]
                }
                */

                // ASSERT
                Assert.IsFalse(result.Contains("exceptiontype"), "The result has error: " + result);
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/Backup/AAKE452", cancel));
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/AAKE452", cancel));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Cars/Backup/AAKE452", true, cancel);
            }
        }

        /// <tab category="content-management" article="copy-move" example="copyMultiple" />
        [TestMethod]
        [Description("Copy multiple content")]
        public async Task Docs2_ContentManagement_Copy_Multiple()
        {
            try
            {
                /*<doc>*/
                var result = await repository.InvokeActionAsync<string>(new OperationRequest
                {
                    Path = "/Root",
                    OperationName = "CopyBatch",
                    PostData = new
                    {
                        targetPath = "/Root/Content/Cars/Backup",
                        paths = new[]
                        {
                            "/Root/Content/Cars/AAKE452",
                            "/Root/Content/Cars/KLT1159"
                        }
                    }
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/('Root')/CopyBatch
                {
                  "targetPath":"/Root/Content/Cars/Backup",
                  "paths":[
                    "/Root/Content/Cars/AAKE452",
                    "/Root/Content/Cars/KLT1159"
                  ]
                }
                */

                // ASSERT
                Assert.IsFalse(result.Contains("exceptiontype"), "The result has error: " + result);
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/Backup/AAKE452", cancel));
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/AAKE452", cancel));
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/Backup/KLT1159", cancel));
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/KLT1159", cancel));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Cars/Backup/AAKE452", true, cancel);
                await repository.DeleteContentAsync("/Root/Content/Cars/Backup/KLT1159", true, cancel);
            }
        }

        /// <tab category="content-management" article="copy-move" example="moveContent" />
        [TestMethod]
        [Description("Move a single content")]
        public async Task Docs2_ContentManagement_Move_Single()
        {
            try
            {
                /*<doc>*/
                var result = await repository.InvokeActionAsync<string>(new OperationRequest
                {
                    Path = "/Root",
                    OperationName = "MoveBatch",
                    PostData = new
                    {
                        targetPath = "/Root/Content/Cars/out-of-order",
                        paths = new[] {"/Root/Content/Cars/AAKE452"}
                    }
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/('Root')/MoveBatch
                {
                  "targetPath":"/Root/Content/Cars/out-of-order",
                  "paths":[
                    "/Root/Content/Cars/AAKE452"
                  ]
                } 
                */

                // ASSERT
                Assert.IsFalse(result.Contains("exceptiontype"), "The result has error: " + result);
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/out-of-order/AAKE452", cancel));
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/Cars/AAKE452", cancel));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Cars/out-of-order/AAKE452", true, cancel);
                await EnsureContentAsync("/Root/Content/Cars/AAKE452", "Car", c =>
                {
                    c["DisplayName"] = "Mazda 6";
                    c["Make"] = "Mazda";
                    c["Model"] = "6";
                    c["Style"] = "Coupe";
                    c["StartingDate"] = DateTime.Parse("2021-08-28");
                    c["Color"] = "Red";
                    c["EngineSize"] = "1800 ccm";
                    c["Power"] = "130 hp";
                    c["Price"] = 7_850_000;
                }, repository, cancel);
            }
        }

        /// <tab category="content-management" article="copy-move" example="moveMultiple" />
        [TestMethod]
        [Description("Move multiple content")]
        public async Task Docs2_ContentManagement_Move_Multiple()
        {
            try
            {
                /*<doc>*/
                var result = await repository.InvokeActionAsync<string>(new OperationRequest
                {
                    Path = "/Root",
                    OperationName = "MoveBatch",
                    PostData = new
                    {
                        targetPath = "/Root/Content/Cars/out-of-order",
                        paths = new[]
                        {
                            "/Root/Content/Cars/AAKE452",
                            "/Root/Content/Cars/KLT1159"
                        }
                    }
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/('Root')/MoveBatch
                {
                  "targetPath":"/Root/Content/Cars/out-of-order",
                  "paths":[
                    "/Root/Content/Cars/AAKE452",
                    "/Root/Content/Cars/KLT1159"
                  ]
                } 
                */

                // ASSERT
                Assert.IsFalse(result.Contains("exceptiontype"), "The result has error: " + result);
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/out-of-order/AAKE452", cancel));
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/Cars/AAKE452", cancel));
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Cars/out-of-order/KLT1159", cancel));
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/Cars/KLT1159", cancel));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Cars/out-of-order/AAKE452", true, cancel);
                await repository.DeleteContentAsync("/Root/Content/Cars/out-of-order/KLT1159", true, cancel);
                await EnsureContentAsync("/Root/Content/Cars/AAKE452", "Car", c =>
                {
                    c["DisplayName"] = "Mazda 6";
                    c["Make"] = "Mazda";
                    c["Model"] = "6";
                    c["Style"] = "Coupe";
                    c["StartingDate"] = DateTime.Parse("2021-08-28");
                    c["Color"] = "Red";
                    c["EngineSize"] = "1800 ccm";
                    c["Power"] = "130 hp";
                    c["Price"] = 7_850_000;
                }, repository, cancel);
                await EnsureContentAsync("/Root/Content/Cars/KLT1159", "Car", c =>
                {
                    c["DisplayName"] = "Renault Thalia";
                    c["Make"] = "Renault";
                    c["Model"] = "Thalia";
                    c["Style"] = "Coupe";
                    c["StartingDate"] = DateTime.Parse("2013-09-11");
                    c["Color"] = "Green";
                    c["EngineSize"] = "1400 ccm";
                    c["Power"] = "105 hp";
                    c["Price"] = 4_930_000;
                }, repository, cancel);
            }
        }

        /* ====================================================================================== Allowed Child Types */

        /// <tab category="content-management" article="allowed-childtypes" example="effectivelyAllowed" />
        [TestMethod]
        [Description("Get child types allowed effectively on a content")]
        public async Task Docs2_ContentManagement_AllowedChildTypes_GetEffective()
        {
            /*<doc>*/
            var result = await repository.InvokeFunctionAsync<string>(new OperationRequest
            {
                Path = "Root/Content",
                OperationName = "EffectiveAllowedChildTypes"
            }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root('Content')/EffectiveAllowedChildTypes
            */

            // ASSERT
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Car"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/File"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/EventList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace"));
        }

        /// <tab category="content-management" article="allowed-childtypes" example="allowedChildTypes" />
        [TestMethod]
        [Description("Get child types allowed on a content")]
        public async Task Docs2_ContentManagement_AllowedChildTypes_Get()
        {
            /*<doc>*/
            var result = await repository.InvokeFunctionAsync<string>(new OperationRequest
            {
                Path = "Root/Content",
                OperationName = "AllowedChildTypes"
            }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root('Content')/AllowedChildTypes
            */

            // ASSERT
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Car"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/File"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/EventList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace"));
        }

        /// <tab category="content-management" article="allowed-childtypes" example="allowedChildTypesFromCTD" />
        [TestMethod]
        [Description("Get allowed child types set in the content type definition")]
        public async Task Docs2_ContentManagement_AllowedChildTypes_GetFromCtd()
        {
            /*<doc>*/
            var result = await repository.InvokeFunctionAsync<string>(new OperationRequest
            {
                Path = "Root/Content",
                OperationName = "GetAllowedChildTypesFromCTD"
            }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root('Content')/GetAllowedChildTypesFromCTD
            */

            // ASSERT
            Assert.IsFalse(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Car"));
            Assert.IsFalse(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/File"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder"));
            Assert.IsFalse(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/EventList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList"));
            Assert.IsTrue(result.Contains("/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace"));
        }

        /// <tab category="content-management" article="allowed-childtypes" example="updateAllowedChildTypes" />
        [TestMethod]
        [Description("Update list of allowed child types on a content")]
        public async Task Docs2_ContentManagement_AllowedChildTypes_Update()
        {
            var result = await repository.InvokeFunctionAsync<string>(new OperationRequest
                    {Path = "Root/Content", OperationName = "AllowedChildTypes", Select = new[] {"Name"}}, cancel);
            var backup = JObject.Parse(result)["d"]["results"].ToArray().Select(x => x["Name"].ToString()).ToArray();

            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content", cancel);
                content["AllowedChildTypes"] = new[] { "ImageLibrary", "DocumentLibrary", "TaskList" };
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Content
                models=[{
                  "AllowedChildTypes":[
                    "ImageLibrary",
                    "DocumentLibrary",
                    "TaskList"
                  ]
                }] 
                */

                // ASSERT
                result = await repository.InvokeFunctionAsync<string>(new OperationRequest
                    { Path = "Root/Content", OperationName = "AllowedChildTypes", Select = new[] { "Name" } }, cancel);
                var allowedTypes = JObject.Parse(result)["d"]["results"].ToArray().Select(x => x["Name"].ToString()).ToArray();
                Assert.AreEqual(3, allowedTypes.Length);
                Assert.AreEqual("DocumentLibrary,ImageLibrary,TaskList",
                    string.Join(",", allowedTypes.OrderBy(x=>x)));
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content", cancel);
                content["AllowedChildTypes"] = backup;
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="content-management" article="allowed-childtypes" example="addTypes" />
        [TestMethod]
        [Description("Add a type to the allowed childtypes")]
        public async Task Docs2_ContentManagement_AllowedChildTypes_Add()
        {
            var result = await repository.InvokeFunctionAsync<string>(new OperationRequest
                { Path = "Root/Content", OperationName = "AllowedChildTypes", Select = new[] { "Name" } }, cancel);
            var backup = JObject.Parse(result)["d"]["results"].ToArray().Select(x => x["Name"].ToString()).ToArray();

            try
            {
                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Content",
                    OperationName = "AddAllowedChildTypes",
                    PostData = new {contentTypes = new[] {"Image", "CalendarEvent", "Task"}}
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root('Content')/AddAllowedChildTypes
                {"contentTypes":["Image","CalendarEvent","Task"]} 
                */

                // ASSERT
                result = await repository.InvokeFunctionAsync<string>(new OperationRequest
                    { Path = "Root/Content", OperationName = "AllowedChildTypes", Select = new[] { "Name" } }, cancel);
                var allowedTypes = JObject.Parse(result)["d"]["results"].ToArray().Select(x => x["Name"].ToString()).ToArray();
                Assert.IsTrue(allowedTypes.Length > backup.Length);
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content", cancel);
                content["AllowedChildTypes"] = backup;
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="content-management" article="allowed-childtypes" example="removeTypes" />
        [TestMethod]
        [Description("Remove a type from the allowed childtypes")]
        public async Task Docs2_ContentManagement_AllowedChildTypes_Remove()
        {
            var result = await repository.InvokeFunctionAsync<string>(new OperationRequest
                { Path = "Root/Content", OperationName = "AllowedChildTypes", Select = new[] { "Name" } }, cancel);
            var backup = JObject.Parse(result)["d"]["results"].ToArray().Select(x => x["Name"].ToString()).ToArray();

            try
            {
                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Content",
                    OperationName = "RemoveAllowedChildTypes",
                    PostData = new { contentTypes = new[] { "Car", "File" } }
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root('Content')/RemoveAllowedChildTypes
                {"contentTypes":["Car","File"]} 
                */

                // ASSERT
                result = await repository.InvokeFunctionAsync<string>(new OperationRequest
                    { Path = "Root/Content", OperationName = "AllowedChildTypes", Select = new[] { "Name" } }, cancel);
                var allowedTypes = JObject.Parse(result)["d"]["results"].ToArray().Select(x => x["Name"].ToString()).ToArray();
                Assert.IsTrue(allowedTypes.Length < backup.Length);
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content", cancel);
                content["AllowedChildTypes"] = backup;
                await content.SaveAsync(cancel);
            }

        }

        /// <tab category="content-management" article="allowed-childtypes" example="checkAllowedTypes" />
        //UNDONE:Docs2: Missing assertion.
        [TestMethod]
        [Description("Check allowed childtypes")]
        public async Task Docs2_ContentManagement_AllowedChildTypes_Check()
        {
            /*<doc>*/
            await repository.InvokeFunctionAsync<string>(new OperationRequest
            {
                Path = "/Root/Content",
                OperationName = "CheckAllowedChildTypesOfFolders"
            }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root('Content')/CheckAllowedChildTypesOfFolders
            */

            // ASSERT
        }

        /* ====================================================================================== Trash */

        /// <tab category="content-management" article="trash" example="disableTrashGlobally" />
        [TestMethod]
        [Description("Switch Trash on and off globally")]
        public async Task Docs2_ContentManagement_Trash_Off()
        {
            TrashBin trash;
            try
            {
                /*<doc>*/
                trash = await repository.LoadContentAsync<TrashBin>("/Root/Trash", cancel);
                trash.IsActive = false;
                await trash.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Trash
                models=[{"IsActive":false}]
                */

                // ASSERT
                trash = await repository.LoadContentAsync<TrashBin>("/Root/Trash", cancel);
                Assert.IsFalse(trash.IsActive);
            }
            finally
            {
                trash = await repository.LoadContentAsync<TrashBin>("/Root/Trash", cancel);
                trash.IsActive = true;
                await trash.SaveAsync(cancel);
            }
        }

        /// <tab category="content-management" article="trash" example="disableTrashOnAContent" />
        [TestMethod]
        [Description("Disable Trash for a specific container")]
        public async Task Docs2_ContentManagement_Trash_Disable()
        {
            Content content;
            try
            {
                /*<doc>*/
                content = await repository.LoadContentAsync("/Root/Content/Cars/AAXX123", cancel);
                content["TrashDisabled"] = false;
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Content/Cars/AAXX123
                models=[{"TrashDisabled":false}]
                */

                //ASSERT
                content = await repository.LoadContentAsync("/Root/Content/Cars/AAXX123", cancel);
                Assert.AreEqual("false", content["TrashDisabled"].ToString()?.ToLowerInvariant());
            }
            finally
            {
                content = await repository.LoadContentAsync("/Root/Content/Cars/AAXX123", cancel);
                content["TrashDisabled"] = true;
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="content-management" article="trash" example="trashOptions" />
        [TestMethod]
        [Description("Configure trash options")]
        public async Task Docs2_ContentManagement_Trash_Options()
        {
            TrashBin trash;
            try
            {
                /*<doc>*/
                trash = await repository.LoadContentAsync<TrashBin>("/Root/Trash", cancel);
                trash.SizeQuota = 20;
                trash.BagCapacity = 200;
                trash.MinRetentionTime = 14;
                await trash.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Trash
                models=[{
                  "MinRetentionTime":14,
                  "SizeQuota":20,
                  "BagCapacity":200
                }]
                */

                // ASSERT
                trash = await repository.LoadContentAsync<TrashBin>("/Root/Trash", cancel);
                Assert.AreEqual(20, trash.SizeQuota);
                Assert.AreEqual(200, trash.BagCapacity);
                Assert.AreEqual(14, trash.MinRetentionTime);
            }
            finally
            {
                trash = await repository.LoadContentAsync<TrashBin>("/Root/Trash", cancel);
                trash.SizeQuota = 0;
                trash.BagCapacity = 100;
                trash.MinRetentionTime = 0;
                await trash.SaveAsync(cancel);
            }

        }

        /// <tab category="content-management" article="trash" example="restoreFromTrash" />
        [TestMethod]
        [Description("Restore items from the trash")]
        public async Task Docs2_ContentManagement_Trash_Restore()
        {
            var path = "/Root/Content/TestFolder";
            Content content = await EnsureContentAsync(path, "Folder", repository, cancel);
            await repository.DeleteContentAsync(path, false, cancel);
            var trashBags = await repository.LoadCollectionAsync(
                new LoadCollectionRequest {Path = "/Root/Trash"}, cancel);
            var bag = trashBags.First();
            bag.Name = "TrashBag-2024061101254";
            await bag.SaveAsync(cancel);
            try
            {
                Assert.IsFalse(await repository.IsContentExistsAsync(path, cancel));

                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Trash/TrashBag-2024061101254",
                    OperationName = "Restore"
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Trash('TrashBag-2024061101254')/Restore
                */

                //ASSERT
                Assert.IsTrue(await repository.IsContentExistsAsync(path, cancel));
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Trash/TrashBag-2020061101254", cancel));
            }
            finally
            {
                await repository.DeleteContentAsync(path, true, cancel);
                await EmptyTrash(repository, cancel);
            }
        }


        /// <tab category="content-management" article="trash" example="restoreToAnotherDestination" />
        [TestMethod]
        [Description("Restore items to different destination")]
        public async Task Docs2_ContentManagement_Trash_RestoreToDifferentDestination()
        {
            Content content = await EnsureContentAsync("/Root/Content/TestFolder", "Folder", repository, cancel);
            Content target = await EnsureContentAsync("/Root/Content/Target", "Folder", repository, cancel);
            await repository.DeleteContentAsync("/Root/Content/TestFolder", false, cancel);
            var trashBags = await repository.LoadCollectionAsync(
                new LoadCollectionRequest { Path = "/Root/Trash" }, cancel);
            var bag = trashBags.First();
            bag.Name = "TrashBag-2024061101254";
            await bag.SaveAsync(cancel);
            try
            {
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/TestFolder", cancel));

                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Trash/TrashBag-2024061101254",
                    OperationName = "Restore",
                    PostData = new { destination = "/Root/Content/Target" }
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Trash('TrashBag-2024061101254')/Restore
                {"destination":"/Root/Content/Target"} 
                */

                //ASSERT
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/Target/TestFolder", cancel));
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Trash/TrashBag-2020061101254", cancel));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/TestFolder", true, cancel);
                await repository.DeleteContentAsync("/Root/Content/Target", true, cancel);
                await EmptyTrash(repository, cancel);
            }
        }

        /// <tab category="content-management" article="trash" example="restoreWithNewName" />
        [TestMethod]
        [Description("Restore items with a new name")]
        public async Task Docs2_ContentManagement_Trash_RestoreAndRenameIncremental()
        {
            await EnsureContentAsync("/Root/Content/TestFolder", "Folder", repository, cancel);
            await repository.DeleteContentAsync("/Root/Content/TestFolder", false, cancel);
            var trashBags = await repository.LoadCollectionAsync(
                new LoadCollectionRequest { Path = "/Root/Trash" }, cancel);
            var bag = trashBags.First();
            bag.Name = "TrashBag-2024061101254";
            await bag.SaveAsync(cancel);
            await EnsureContentAsync("/Root/Content/TestFolder", "Folder", repository, cancel);
            try
            {
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/TestFolder", cancel));

                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Trash/TrashBag-2024061101254",
                    OperationName = "Restore",
                    PostData = new { newname = true }
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Trash('TrashBag-2024061101254')/Restore
                {"newname":true}
                */

                //ASSERT
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/TestFolder", cancel));
                Assert.IsTrue(await repository.IsContentExistsAsync("/Root/Content/TestFolder(1)", cancel));
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Trash/TrashBag-2020061101254", cancel));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/TestFolder", true, cancel);
                await repository.DeleteContentAsync("/Root/Content/TestFolder(1)", true, cancel);
                await repository.DeleteContentAsync("/Root/Content/Target", true, cancel);
                await EmptyTrash(repository, cancel);
            }

        }

        /// <tab category="content-management" article="trash" example="deleteFromTrash" />
        [TestMethod]
        [Description("Delete items from the trash")]
        public async Task Docs2_ContentManagement_Trash_DeleteFromTrash()
        {
            try
            {
                Content content = await EnsureContentAsync("/Root/Content/TestFolder", "Folder", repository, cancel);
                await repository.DeleteContentAsync("/Root/Content/TestFolder", false, cancel);
                var trashBags = await repository.LoadCollectionAsync(
                    new LoadCollectionRequest {Path = "/Root/Trash"}, cancel);
                var bag = trashBags.First();
                bag.Name = "TrashBag-2024061101254";
                await bag.SaveAsync(cancel);

                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/TestFolder", cancel));

                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Trash/TrashBag-2024061101254",
                    OperationName = "Delete"
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Trash('TrashBag-2024061101254')/Delete
                */

                //ASSERT
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Content/Target/TestFolder", cancel));
                Assert.IsFalse(await repository.IsContentExistsAsync("/Root/Trash/TrashBag-2020061101254", cancel));
            }
            finally
            {
                await EmptyTrash(repository, cancel);
            }
        }

        /* ====================================================================================== List Fields */

        /// <tab category="content-management" article="list-fields" example="selectByListField" />
        [TestMethod]
        [Description("Browsing list fields")]
        public async Task Docs2_ContentManagement_ListFields_Select()
        {
            Assert.Inconclusive();

            /*<doc>*/
            /*</doc>*/
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

        /// <tab category="content-management" article="list-fields" example="metadata" />
        [TestMethod]
        [Description("List expando fields defined on a specified list")]
        public async Task Docs2_ContentManagement_ListFields_Metadata()
        {
            Assert.Inconclusive();

            /*<doc>*/
            /*</doc>*/
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", repository, cancel);

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

        /// <tab category="content-management" article="list-fields" example="addField" />
        [TestMethod]
        [Description("Add a new list field")]
        public async Task Docs2_ContentManagement_ListFields_Add()
        {
            Assert.Inconclusive();

            // ALIGN
            await EnsureContentAsync("/Root/Content/Document_Library", "DocumentLibrary", repository, cancel);
            try
            {
                /*<doc>*/
                var result = await repository.GetResponseStringAsync(new ODataRequest
                {
                    Path =  "/Root/Content/Document_Library",
                    PostData = new
                    {
                        __ContentType = "IntegerFieldSetting",
                        Name = "MyField1",
                        DisplayName = "My Field 1",
                        Compulsory = true,
                        MinValue = 10
                    }
                }, HttpMethod.Post, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content('Document_Library')
                models=[{
                  "__ContentType":"IntegerFieldSetting",
                  "Name":"MyField1",
                  "DisplayName":"My Field 1",
                  "Compulsory":true,
                  "MinValue":10
                }] 
                */

                // ASSERT
                await EnsureContentAsync("/Root/Content/Document_Library/Folder1", "Folder", c =>
                {
                    c["#MyField"] = 789;
                }, repository, cancel);
                var body = @"models=[{""#MyField1"":1234}]";
                var xx = await RESTCaller.GetResponseStringAsync("/Root/Content/Document_Library/Folder1", null,
                    HttpMethod.Patch, body, repository.Server);
                ////var x = repository.GetResponseStringAsync(new ODataRequest
                ////{
                ////    Path = "/Root/Content/Document_Library",
                ////    PostData = new{}
                ////}, HttpMethod.Patch, cancel);
                //var content = await repository.LoadContentAsync("/Root/Content/Document_Library", cancel);
                //content["%23MyField1"] = 456;
                //await content.SaveAsync(cancel);
                Assert.Inconclusive();
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Document_Library", true, cancel);
            }
        }

        /// <tab category="content-management" article="list-fields" example="editFieldVirtualChildPatch" />
        [TestMethod]
        [Description("Edit expando fields 1")]
        public async Task Docs2_ContentManagement_ListFields_Edit1()
        {
            Assert.Inconclusive();

            /*<doc>*/
            /*</doc>*/
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", repository, cancel);
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

        /// <tab category="content-management" article="list-fields" example="editFieldVirtualChildPut" />
        [TestMethod]
        [Description("Edit expando fields 2")]
        public async Task Docs2_ContentManagement_ListFields_Edit2()
        {
            Assert.Inconclusive();

            /*<doc>*/
            /*</doc>*/
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", repository, cancel);
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

        /// <tab category="content-management" article="list-fields" example="editFieldWithAction" />
        [TestMethod]
        [Description("Edit expando fields 3")]
        public async Task Docs2_ContentManagement_ListFields_Edit3()
        {
            Assert.Inconclusive();

            /*<doc>*/
            /*</doc>*/
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

        /// <tab category="content-management" article="list-fields" example="removeFieldVirtualChild" />
        [TestMethod]
        [Description("Remove a list field 1")]
        public async Task Docs2_ContentManagement_ListFields_Remove1()
        {
            Assert.Inconclusive();

            /*<doc>*/
            /*</doc>*/
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", repository, cancel);
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

        /// <tab category="content-management" article="list-fields" example="removeFieldAction" />
        [TestMethod]
        [Description("Remove a list field 2")]
        public async Task Docs2_ContentManagement_ListFields_Remove2()
        {
            Assert.Inconclusive();

            /*<doc>*/
            /*</doc>*/
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
