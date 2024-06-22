﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Collaboration : ClientIntegrationTestBase
    {
        private class MyContent : Content { public MyContent(IRestCaller rc, ILogger<Content> l) : base(rc, l) { } }
        // ReSharper disable once InconsistentNaming
        private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        // ReSharper disable once InconsistentNaming
        IRepository repository =>
            GetRepositoryCollection(services => { services.RegisterGlobalContentType<MyContent>(); })
                .GetRepositoryAsync("local", cancel).GetAwaiter().GetResult();

        /* ====================================================================================== Versioning */

        /// <tab category="collaboration" article="versioning" example="enableVersioning" />
        [TestMethod]
        [Description("Enable versioning")]
        public async Task Docs_Collaboration_Versioning_EnableVersioning()
        {
            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
                content.InheritableVersioningMode = VersioningMode.MajorAndMinor;
                await content.SaveAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc//Root/Content/('Documents')
                models=[{"InheritableVersioningMode":["3"]}] 
                */

                // ASSERT
                var loaded = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
                Assert.AreEqual(3, ((JArray)loaded["InheritableVersioningMode"]).Single().Value<int>());
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
                content["InheritableVersioningMode"] = new[] { 0 };
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="versionNumber" />
        [TestMethod]
        [Description("Get current version of a content")]
        public async Task Docs_Collaboration_Versioning_GetCurrentVersion()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync(new LoadContentRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    Select = new []{"Id", "Type", "Path", "Version"}
                }, cancel);
                var version = content.Version;
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')?metadata=no&$select=Id,Type,Path,Version
                */

                // ASSERT
                Assert.AreEqual("V1.0.A", version);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="specificVersion" />
        [TestMethod]
        [Description("Get a specific version of a content")]
        public async Task Docs_Collaboration_Versioning_GetSpecificVersion()
        {
            var documents = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
            documents.InheritableVersioningMode = VersioningMode.MajorOnly;
            await documents.SaveAsync(cancel);
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            var doc = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
            await doc.CheckOutAsync(cancel);
            await doc.CheckInAsync(cancel);
            await doc.CheckOutAsync(cancel);
            await doc.CheckInAsync(cancel);
            var docLastVersion = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
            Assert.AreEqual("V3.0.A", docLastVersion.Version);

            try
            {
                /*<doc>*/
                var request = new LoadContentRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    Version = "V2.0.A"
                };
                var content = await repository.LoadContentAsync(request, cancel);
                var version = content.Version;
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')?version=V2.0.A
                */

                // ASSERT
                Assert.AreEqual("V2.0.A", content.Version);
            }
            finally
            {
                documents = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
                documents["InheritableVersioningMode"] = new[] { 0 };
                await documents.SaveAsync(cancel);
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="checkout" />
        [TestMethod]
        [Description("Checkout a content for edit")]
        public async Task Docs_Collaboration_Versioning_CheckOut()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.CheckOutAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')/CheckOut
                */

                // ASSERT
                content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V2.0.L", content["Version"].ToString());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="checkin" />
        [TestMethod]
        [Description("Check-in a content")]
        public async Task Docs_Collaboration_Versioning_CheckIn()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                var doc = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await doc.CheckOutAsync(cancel);
                doc = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V2.0.L", doc.Version);

                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.CheckInAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')/CheckIn
                */

                // ASSERT
                content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V1.0.A", content["Version"].ToString());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="locked" />
        [TestMethod]
        [Description("How to know if a content is locked")]
        public async Task Docs_Collaboration_Versioning_IsLocked()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                var content1 = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content1.CheckOutAsync(cancel);

                /*<doc>*/
                var content = await repository.LoadContentAsync(new LoadContentRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    Expand = new List<string> { "CheckedOutTo" },
                    Select = new List<string> { "Locked", "CheckedOutTo/Name" },
                }, cancel);
                var locked = content.Locked;
                var lockedBy = content.CheckedOutTo?.Name;
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')?
                $expand=CheckedOutTo&$select=Locked,CheckedOutTo/Name
                */

                // ASSERT
                Assert.AreEqual(true, locked);
                Assert.AreEqual("Admin", lockedBy);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="publish" />
        [TestMethod]
        [Description("Publish a new major version")]
        public async Task Docs_Collaboration_Versioning_Publish()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            var document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
            document.VersioningMode = VersioningMode.MajorAndMinor;
            await document.SaveAsync(cancel);
            try
            {
                await document.CheckOutAsync(cancel);
                await document.CheckInAsync(cancel);
                document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V1.2.D", document.Version);

                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.PublishAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')/Publish
                */

                // ASSERT
                content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V2.0.A", content["Version"].ToString());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="undoChanges" />
        [TestMethod]
        [Description("Undo changes")]
        public async Task Docs_Collaboration_Versioning_Undo()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            var document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
            document.VersioningMode = VersioningMode.MajorAndMinor;
            await document.SaveAsync(cancel);
            try
            {
                document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V1.1.D", document.Version);
                await document.CheckOutAsync(cancel);
                document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V1.2.L", document.Version);

                // ACTION for doc
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.UndoCheckOutAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')/UndoCheckOut
                */

                // ASSERT
                content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V1.1.D", content["Version"].ToString());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="forceUndoChanges" />
        [TestMethod]
        [Description("Force undo changes")]
        public async Task Docs_Collaboration_Versioning_ForceUndo()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            var document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
            var documentId = document.Id;
            document.VersioningMode = VersioningMode.MajorAndMinor;
            await document.SaveAsync(cancel);
            var user = await repository.LoadContentAsync("/Root/IMS/BuiltIn/Portal/PublicAdmin", cancel);
            try
            {
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.1.D", document.Version);
                await document.CheckOutAsync(cancel); // V1.2.L
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.L", document.Version);

                await repository.GetResponseStringAsync(new ODataRequest(repository.Server)
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    ActionName = "TakeLockOver",
                    PostData = new { user = user.Id.ToString() }
                }, HttpMethod.Post, cancel);

                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.ForceUndoCheckOutAsync(cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')/ForceUndoCheckOut
                */

                // ASSERT
                content = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.1.D", content["Version"].ToString());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="takeLockOver" />
        [TestMethod]
        [Description("Take lock over")]
        public async Task Docs_Collaboration_Versioning_TakeLockOver()
        {
            Content document;
            int documentId = 0;
            try
            {
                await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
                document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                documentId = document.Id;
                document.VersioningMode = VersioningMode.MajorAndMinor;
                await document.SaveAsync(cancel);
                var user = await repository.LoadContentAsync("/Root/IMS/BuiltIn/Portal/PublicAdmin", cancel);

                document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await document.CheckOutAsync(cancel);

                /*<doc>*/
//var request = new ODataRequest(repository.Server)
//{
//    Path = "/Root/Content/Documents/BusinessPlan.docx",
//    ActionName = "TakeLockOver",
//    PostData = new {user = "/Root/IMS/BuiltIn/Portal/PublicAdmin" }
//};
//var result = await repository.GetResponseStringAsync(request, HttpMethod.Post, cancel);

                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    OperationName = "TakeLockOver",
                    PostData = new {user = "/Root/IMS/BuiltIn/Portal/PublicAdmin"}
                }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')/TakeLockOver
                models=[{"user":"/Root/IMS/BuiltIn/Portal/PublicAdmin"}] 
                */

                // ASSERT
                var request = new ODataRequest(repository.Server)
                {
                    ContentId = documentId,
                    Expand = new []{ "CheckedOutTo" },
                    Select = new []{ "CheckedOutTo/Name" }
                };
                var expected = @"{
  ""d"": {
    ""CheckedOutTo"": {
      ""Name"": ""PublicAdmin""
    }
  }
}";
                var result = await repository.GetResponseStringAsync(request, HttpMethod.Get, cancel);
                Assert.AreEqual(expected, result);
            }
            finally
            {
                document = await repository.LoadContentAsync(documentId, cancel);
                if (document != null)
                {
                    if (document.Version?.EndsWith("L") ?? false)
                    {
                        var result = await repository.GetResponseStringAsync(new ODataRequest(repository.Server)
                        {
                            ContentId = documentId,
                            ActionName = "TakeLockOver",
                            PostData = new { user = "1" }
                        }, HttpMethod.Post, cancel);
                    }

                    await document.DeleteAsync(true, cancel);
                }
            }
        }

        /// <tab category="collaboration" article="versioning" example="versionHistory" />
        [TestMethod]
        [Description("Get version history of a content")]
        public async Task Docs_Collaboration_Versioning_VersionHistory()
        {
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", repository, cancel);
            await EnsureContentAsync("/Root/Content/Documents", "Folder", repository, cancel);
            var doc = await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            var docId = doc.Id;
            try
            {
                doc = await repository.LoadContentAsync(docId, cancel);
                doc["VersioningMode"] = new[] { 3 };
                await doc.SaveAsync(cancel);

                await doc.CheckOutAsync(cancel);
                await doc.CheckInAsync(cancel);
                await doc.CheckOutAsync(cancel);
                await doc.CheckInAsync(cancel);
                await doc.PublishAsync(cancel);
                await doc.CheckOutAsync(cancel);
                await doc.CheckInAsync(cancel);
                await doc.CheckOutAsync(cancel);
                await doc.CheckInAsync(cancel);
                await doc.CheckOutAsync(cancel);
                await doc.CheckInAsync(cancel);
                await doc.PublishAsync(cancel);
                await doc.CheckOutAsync(cancel);
                await doc.CheckInAsync(cancel);
                await doc.CheckOutAsync(cancel);
                await doc.CheckInAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var request = new ODataRequest(repository.Server)
                {
                    /*</doc>*/Select = new []{ "Version" },
                    /*<doc>*/Path = "/Root/Content/Documents/BusinessPlan.docx",
                    ActionName = "Versions",
                };
                var result = await repository.GetResponseStringAsync(request, HttpMethod.Get, cancel);
                Console.WriteLine(result);
                /*</doc>*/

                // ASSERT
                var expected = @"{
  ""d"": {
    ""__count"": 9,
    ""results"": [
      {
        ""Version"": ""V1.0.A""
      },
      {
        ""Version"": ""V1.1.D""
      },
      {
        ""Version"": ""V1.2.D""
      },
      {
        ""Version"": ""V2.0.A""
      },
      {
        ""Version"": ""V2.1.D""
      },
      {
        ""Version"": ""V2.2.D""
      },
      {
        ""Version"": ""V3.0.A""
      },
      {
        ""Version"": ""V3.1.D""
      },
      {
        ""Version"": ""V3.2.D""
      }
    ]
  }
}
";
                var message = Console.GetStringBuilder().ToString();
                Assert.AreEqual(expected, message);
            }
            finally
            {
                var c = await repository
                    .LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="recallVersion" />
        [TestMethod]
        [Description("Restore an old version")]
        public async Task Docs_Collaboration_Versioning_RestoreOldVersion()
        {
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", repository, cancel);
            await EnsureContentAsync("/Root/Content/Documents", "Folder", repository, cancel);
            var doc = await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            var docId = doc.Id;
            try
            {
                doc["VersioningMode"] = new[] { 3 };
                await doc.SaveAsync(cancel); // V1.1.D

                await doc.CheckOutAsync(cancel); // V1.2.L
                await doc.CheckInAsync(cancel); // V1.2.D

                // ACTION for doc
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.RestoreVersionAsync("V1.0.A", cancel);
                /*</doc>*/

                // ASSERT
                content = await repository.LoadContentAsync(docId, cancel);
                Assert.AreEqual("V1.0.A", content["Version"].ToString());
            }
            finally
            {
                var c = await repository
                    .LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /* ====================================================================================== Approval */

        /// 
        [TestMethod]
        [Description("Enable simple approval")]
        public async Task Docs_Collaboration_Approval_Enable()
        {
            //UNDONE:- Feature request: use a textual language element instead of an integer array for InheritableApprovingMode
            //UNDONE:- Feature request: use a textual language element instead of an integer array for InheritableVersioningMode
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            try
            {
                // ACTION for doc
                var content = await repository.LoadContentAsync("/Root/Content/IT", cancel);
                content["InheritableApprovingMode"] = new[] { 2 };
                content["InheritableVersioningMode"] = new[] { 3 };
                await content.SaveAsync(cancel);

                // ASSERT
                var message = Console.GetStringBuilder().ToString();
                Assert.Inconclusive();
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content/IT", cancel);
                content["InheritableApprovingMode"] = new[] { 0 };
                content["InheritableVersioningMode"] = new[] { 0 };
                await content.SaveAsync(cancel);
            }
        }

        /// 
        [TestMethod]
        [Description("Approve a content")]
        public async Task Docs_Collaboration_Approval_Approve()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: The server returned an error (HttpStatus: InternalServerError): Currently this action is not allowed on this content.
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", repository, cancel);
            await EnsureContentAsync("/Root/Content/Documents", "Folder", repository, cancel);
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseJsonAsync(method: HttpMethod.Post, requestData: new ODataRequest
                {
                    IsCollectionRequest = false,
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    ActionName = "Approve",
                });
                Console.WriteLine(result);

                // ASSERT
                var message = Console.GetStringBuilder().ToString();
                Assert.Inconclusive();
            }
            finally
            {
                var c = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /// 
        [TestMethod]
        [Description("Reject a content")]
        public async Task Docs_Collaboration_Approval_Reject()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: The server returned an error (HttpStatus: InternalServerError): Currently this action is not allowed on this content.
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", repository, cancel);
            await EnsureContentAsync("/Root/Content/Documents", "Folder", repository, cancel);
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                // ACTION for doc
                var body = @"models=[{""rejectReason"": ""Reject reason""}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/Documents/BusinessPlan.docx", "Reject", HttpMethod.Post, body);
                Console.WriteLine(result);

                // ASSERT
                var message = Console.GetStringBuilder().ToString();
                Assert.Inconclusive();
            }
            finally
            {
                var c = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /* ====================================================================================== Saved queries */

        /// 
        [TestMethod]
        [Description("Save a query")]
        public async Task Docs_Collaboration_SavedQueries_SavePublic()
        {
            // ACTION for doc
            var body = @"models=[{
                ""query"": ""+TypeIs:File +InTree:/Root/Content/IT"",
                ""displayName"": ""Public query"",
                ""queryType"": ""Public""}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT/Document_Library", "SaveQuery", HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Save a private query")]
        public async Task Docs_Collaboration_SavedQueries_SavePrivate()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: The server returned an error (HttpStatus: InternalServerError): User profile could not be created.
            // ACTION for doc
            var body = @"models=[{
                ""query"": ""+TypeIs:File +InTree:/Root/Content/IT"",
                ""displayName"": ""My query"",
                ""queryType"": ""Private""}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT/Document_Library", "SaveQuery", HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Get saved queries")]
        public async Task Docs_Collaboration_SavedQueries_Get()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: (every second run if the test filter is: 'Docs_Collaboration_') Invalid response. Request: https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')/GetQueries?metadata=no&onlyPublic=true. Response: 
            // ACTION for doc
            var result = await RESTCaller.GetResponseJsonAsync(new ODataRequest
            {
                IsCollectionRequest = false,
                Path = "/Root/Content/Documents/BusinessPlan.docx",
                ActionName = "GetQueries",
                Parameters = { { "onlyPublic", "true" } }
            });
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
    }
}
