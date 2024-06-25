using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Diagnostics;
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
        public async Task Docs2_Collaboration_Versioning_EnableVersioning()
        {
            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
                content.InheritableVersioningMode = VersioningMode.MajorAndMinor;
                await content.SaveAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_GetCurrentVersion()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync(new LoadContentRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    Select = new []{"Id", "Type", "Path", "Version"}
                }, cancel);
                var version = content.Version;
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_GetSpecificVersion()
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
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var request = new LoadContentRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    Version = "V2.0.A"
                };
                var content = await repository.LoadContentAsync(request, cancel);
                var version = content.Version;
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_CheckOut()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.CheckOutAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_CheckIn()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                var doc = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await doc.CheckOutAsync(cancel);
                doc = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                Assert.AreEqual("V2.0.L", doc.Version);

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.CheckInAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_IsLocked()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            try
            {
                var content1 = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content1.CheckOutAsync(cancel);

                SnTrace.Test.Write(">>>> ACT");
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
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')?$expand=CheckedOutTo&$select=Locked,CheckedOutTo/Name
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
        public async Task Docs2_Collaboration_Versioning_Publish()
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

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.PublishAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_Undo()
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

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.UndoCheckOutAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_ForceUndo()
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

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.ForceUndoCheckOutAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_TakeLockOver()
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

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    OperationName = "TakeLockOver",
                    PostData = new {user = "/Root/IMS/BuiltIn/Portal/PublicAdmin"}
                }, cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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
        public async Task Docs2_Collaboration_Versioning_VersionHistory()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
            var document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
            var documentId = document.Id;
            document.VersioningMode = VersioningMode.MajorAndMinor;
            await document.SaveAsync(cancel);
            try
            {
                document = await repository.LoadContentAsync(documentId, cancel);

                await document.CheckOutAsync(cancel);
                await document.CheckInAsync(cancel);
                await document.CheckOutAsync(cancel);
                await document.CheckInAsync(cancel);
                await document.PublishAsync(cancel);
                await document.CheckOutAsync(cancel);
                await document.CheckInAsync(cancel);
                await document.CheckOutAsync(cancel);
                await document.CheckInAsync(cancel);
                await document.CheckOutAsync(cancel);
                await document.CheckInAsync(cancel);
                await document.PublishAsync(cancel);
                await document.CheckOutAsync(cancel);
                await document.CheckInAsync(cancel);
                await document.CheckOutAsync(cancel);
                await document.CheckInAsync(cancel);

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var result = await repository.InvokeContentCollectionFunctionAsync<Content>(new OperationRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    OperationName = "Versions",
                }, cancel);
                var versions = result
                    .Select(content => content.Version)
                    .ToArray(); // e.g. ["V1.0.A", "V1.1.D", "V2.0.A"]
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root/Content/Documents('BusinessPlan.docx')/Versions
                */

                // ASSERT
                Assert.AreEqual("V1.0.A, V1.1.D, V1.2.D, V2.0.A, V2.1.D, V2.2.D, V3.0.A, V3.1.D, V3.2.D",
                    string.Join(", ", versions));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="versioning" example="recallVersion" />
        [TestMethod]
        [Description("Restore an old version")]
        public async Task Docs2_Collaboration_Versioning_RestoreOldVersion()
        {
            await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", c =>
            {
                c.Description = "first version";
            }, repository, cancel);
            var document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
            var documentId = document.Id;
            document.VersioningMode = VersioningMode.MajorAndMinor;
            document.Description = "second version";
            await document.SaveAsync(cancel); // V1.1.D
            try
            {
                await document.CheckOutAsync(cancel); // V1.2.L
                document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                document.Description = "third version";
                await document.CheckInAsync(cancel); // V1.2.D
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.D", document.Version);

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.RestoreVersionAsync("V1.0.A", cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/content(1930)/RestoreVersion
                models=[{"version":"V1.0.A"}] 
                */

                // ASSERT
                content = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("first version", content.Description);
                Assert.AreEqual("V1.3.D", content.Version);
                var result = await repository.InvokeContentCollectionFunctionAsync<Content>(new OperationRequest
                {
                    Path = "/Root/Content/Documents/BusinessPlan.docx",
                    OperationName = "Versions",
                }, cancel);
                var versions = result
                    .Select(content => content.Version)
                    .ToArray(); // e.g. ["V1.0.A", "V1.1.D", "V2.0.A"]
                Assert.AreEqual("V1.0.A, V1.1.D, V1.2.D, V1.3.D", string.Join(", ", versions));
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /* ====================================================================================== Approval */

        /// <tab category="collaboration" article="approval" example="enableApproval" />
        [TestMethod]
        [Description("Enable simple approval")]
        public async Task Docs2_Collaboration_Approval_Enable()
        {
            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
                content.InheritableApprovingMode = ApprovingEnabled.Yes;
                content.InheritableVersioningMode = VersioningMode.MajorAndMinor;
                await content.SaveAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/Content/Documents
                models=[{
                  "InheritableVersioningMode":["3"],
                  "InheritableApprovingMode":["2"]
                }] 
                */

                // ASSERT
                var loaded = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
                Assert.AreEqual(ApprovingEnabled.Yes, loaded.InheritableApprovingMode);
                Assert.AreEqual(VersioningMode.MajorAndMinor, loaded.InheritableVersioningMode);
            }
            finally
            {
                var content = await repository.LoadContentAsync("/Root/Content/Documents", cancel);
                content.InheritableApprovingMode = ApprovingEnabled.Inherited;
                content.InheritableVersioningMode = VersioningMode.Inherited;
                await content.SaveAsync(cancel);
            }
        }

        /// <tab category="collaboration" article="approval" example="approve" />
        [TestMethod]
        [Description("Approve a content")]
        public async Task Docs2_Collaboration_Approval_Approve()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
                var document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                var documentId = document.Id;
                document.ApprovingMode = ApprovingEnabled.Yes;
                document.VersioningMode = VersioningMode.MajorAndMinor;
                await document.SaveAsync(cancel);
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.1.D", document.Version);
                await document.CheckOutAsync(cancel);
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.L", document.Version);
                await document.CheckInAsync(cancel);
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.D", document.Version);
                await document.PublishAsync(cancel);
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.P", document.Version);

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.ApproveAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content/Documents/('BusinessPlan.docx')/Approve
                */

                // ASSERT
                var loaded = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V2.0.A", loaded.Version);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /// <tab category="collaboration" article="approval" example="reject" />
        [TestMethod]
        [Description("Reject a content")]
        public async Task Docs2_Collaboration_Approval_Reject()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/Documents/BusinessPlan.docx", "File", repository, cancel);
                var document = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                var documentId = document.Id;
                document.ApprovingMode = ApprovingEnabled.Yes;
                document.VersioningMode = VersioningMode.MajorAndMinor;
                await document.SaveAsync(cancel);
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.1.D", document.Version);
                await document.CheckOutAsync(cancel);
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.L", document.Version);
                await document.CheckInAsync(cancel);
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.D", document.Version);
                await document.PublishAsync(cancel);
                document = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.P", document.Version);

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = await repository.LoadContentAsync("/Root/Content/Documents/BusinessPlan.docx", cancel);
                await content.RejectAsync("Not acceptable. The document is not complete.", cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content/Documents/('BusinessPlan.docx')/Reject
                models=[{"rejectReason":"Not acceptable. The document is not complete."}] 
                */

                // ASSERT
                var loaded = await repository.LoadContentAsync(documentId, cancel);
                Assert.AreEqual("V1.2.R", loaded.Version);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Documents/BusinessPlan.docx", true, cancel);
            }
        }

        /* ====================================================================================== Saved queries */

        /// <tab category="collaboration" article="saved-queries" example="saveQuery" />
        [TestMethod]
        [Description("Save a query")]
        public async Task Docs2_Collaboration_SavedQueries_SavePublic()
        {
            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Content/Cars",
                    OperationName = "SaveQuery",
                    PostData = new
                    {
                        Query = "+TypeIs:Car +InTree:/Root/Content/Cars",
                        DisplayName = "All cars",
                        QueryType = "Public"
                    }
                }, cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content('Cars')/SaveQuery
                models=[{
                  "Query":"+TypeIs:Car +InTree:/Root/Content/Cars",
                  "DisplayName":"All cars",
                  "QueryType":"Public"
                }]
                */

                // ASSERT
                var savedQueries = await repository.LoadCollectionAsync(new LoadCollectionRequest
                    {Path= "/Root/Content/Queries" }, cancel);
                var savedQuery = savedQueries.FirstOrDefault();
                Assert.IsNotNull(savedQuery);
                Assert.AreEqual("+TypeIs:Car +InTree:/Root/Content/Cars", savedQuery["Query"].ToString());
                Assert.AreEqual("All cars", savedQuery["DisplayName"].ToString());
                Assert.AreEqual("[\r\n  \"Public\"\r\n]", savedQuery["QueryType"].ToString());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Queries", true, cancel);
            }
        }

        /// <tab category="collaboration" article="saved-queries" example="savePrivateQuery" />
        //TODO: Causes error if the user profile cannot be created.
        // Switch on this on the3 server:
        // {
        //   "sensenet": {
        //     "identityManagement": {
        //       "UserProfilesEnabled":  true 
        //     } 
        [TestMethod]
        [Description("Save a private query")]
        public async Task Docs2_Collaboration_SavedQueries_SavePrivate()
        {
            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Content/Cars",
                    OperationName = "SaveQuery",
                    PostData = new
                    {
                        Query = "+TypeIs:Car +InTree:/Root/Content/Cars",
                        DisplayName = "My query",
                        QueryType = "Private"
                    }
                }, cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/Content('Cars')/SaveQuery
                models=[{
                  "Query":"+TypeIs:Car +InTree:/Root/Content/Cars",
                  "DisplayName":"My query",
                  "QueryType":"Private"
                }]
                */

                // ASSERT
                var savedQueries = await repository.LoadCollectionAsync(new LoadCollectionRequest
                    { Path = "/Root/Profiles/BuiltIn/Admin/Queries" }, cancel);
                var savedQuery = savedQueries.FirstOrDefault();
                Assert.IsNotNull(savedQuery);
                Assert.AreEqual("+TypeIs:Car +InTree:/Root/Content/Cars", savedQuery["Query"].ToString());
                Assert.AreEqual("My query", savedQuery["DisplayName"].ToString());
                Assert.AreEqual("[\r\n  \"Private\"\r\n]", savedQuery["QueryType"].ToString());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Profiles/BuiltIn/Admin/Queries", true, cancel);
            }
        }

        /// <tab category="collaboration" article="saved-queries" example="getSavedQueries" />
        [TestMethod]
        [Description("Get saved queries")]
        public async Task Docs2_Collaboration_SavedQueries_Get()
        {
            try
            {
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/Content/Cars",
                    OperationName = "SaveQuery",
                    PostData = new
                    {
                        Query = "+TypeIs:Car +InTree:/Root/Content/Cars",
                        DisplayName = "All cars",
                        QueryType = "Public"
                    }
                }, cancel);

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var savedQueries = await repository.InvokeContentCollectionFunctionAsync<SnQuery>(new OperationRequest
                {
                    Path = "/Root/Content/Cars",
                    OperationName = "GetQueries",
                    Parameters = { { "onlyPublic", "true" } }
                }, cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root/Content('Cars')/GetQueries?onlyPublic=true
                */

                // ASSERT
                var savedQuery = savedQueries.FirstOrDefault();
                Assert.IsNotNull(savedQuery);
                Assert.AreEqual("+TypeIs:Car +InTree:/Root/Content/Cars", savedQuery["Query"].ToString());
                Assert.AreEqual("All cars", savedQuery["DisplayName"].ToString());
                Assert.AreEqual("[\r\n  \"Public\"\r\n]", savedQuery["QueryType"].ToString());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Queries", true, cancel);
            }

        }
    }
}
