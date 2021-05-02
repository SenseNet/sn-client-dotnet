﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.TestsForDocs.Infrastructure;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Collaboration : ClientIntegrationTestBase
    {
        /* ====================================================================================== Versioning */

        [TestMethod]
        [Description("Enable versioning")]
        public async Task Docs_Collaboration_Versioning_EnableVersioning()
        {
            //UNDONE:- Feature request: use a textual language element instead of an integer array
            try
            {
                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT");
                content["InheritableVersioningMode"] = new[] {3};
                await content.SaveAsync();

                // ASSERT
                var loaded = await Content.LoadAsync("/Root/Content/IT");
                Assert.AreEqual(3, ((JArray)loaded["InheritableVersioningMode"]).Single().Value<int>());
            }
            finally
            {
                var content = await Content.LoadAsync("/Root/Content/IT");
                content["InheritableVersioningMode"] = new[] { 0 };
                await content.SaveAsync();
            }
        }
        [TestMethod]
        [Description("Get current version of a content")]
        public async Task Docs_Collaboration_Versioning_GetCurrentVersion()
        {
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                dynamic content = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                var version = content.Version;
                Console.WriteLine(version);

                // ASSERT
                var message = Console.GetStringBuilder().ToString();
                Assert.AreEqual("V1.0.A", message.Trim());
                Assert.AreEqual("V1.0.A", content.Version.ToString());
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Get a specific version of a content")]
        public async Task Docs_Collaboration_Versioning_GetSpecificVersion()
        {
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var req = new ODataRequest(ClientContext.Current.Server)
                {
                    Path = "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx",
                    Version = "V1.0.A"
                };
                dynamic content = await Content.LoadAsync(req);
                Console.WriteLine(content.Version);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Checkout a content for edit")]
        public async Task Docs_Collaboration_Versioning_CheckOut()
        {
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseJsonAsync(method: HttpMethod.Post, requestData: new ODataRequest
                {
                    IsCollectionRequest = false,
                    Path = "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx",
                    ActionName = "Checkout",
                });
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Check-in a content")]
        public async Task Docs_Collaboration_Versioning_CheckIn()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: The server returned an error (HttpStatus: InternalServerError): Currently this action is not allowed on this content.
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseJsonAsync(method: HttpMethod.Post, requestData: new ODataRequest
                {
                    IsCollectionRequest = false,
                    Path = "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx",
                    ActionName = "CheckIn",
                    Parameters = { { "checkInComments", "Adding new contract" } }
                });
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("How to know if a content is locked")]
        public async Task Docs_Collaboration_Versioning_IsLocked()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: 'Newtonsoft.Json.Linq.JValue' does not contain a definition for 'Name'
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                dynamic content = await Content.LoadAsync(new ODataRequest
                {
                    IsCollectionRequest = false,
                    Path = "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx",
                    Expand = new List<string> { "CheckedOutTo" },
                    Select = new List<string> { "Locked", "CheckedOutTo/Name" },
                });
                var locked = content.Locked;
                var lockedBy = content.CheckedOutTo.Name;
                Console.WriteLine($"Locked: {locked}, LockedBy: {lockedBy}");

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Publish a new major version")]
        public async Task Docs_Collaboration_Versioning_Publish()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: The server returned an error (HttpStatus: InternalServerError): Currently this action is not allowed on this content.
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseJsonAsync(method: HttpMethod.Post, requestData: new ODataRequest
                {
                    IsCollectionRequest = false,
                    Path = "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx",
                    ActionName = "Publish",
                });
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Undo changes")]
        public async Task Docs_Collaboration_Versioning_Undo()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: The server returned an error (HttpStatus: InternalServerError): Currently this action is not allowed on this content.
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseJsonAsync(method: HttpMethod.Post, requestData: new ODataRequest
                {
                    IsCollectionRequest = false,
                    Path = "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx",
                    ActionName = "UndoCheckOut",
                });
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Force undo changes")]
        public async Task Docs_Collaboration_Versioning_ForceUndo()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: The server returned an error (HttpStatus: InternalServerError): Currently this action is not allowed on this content.
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseJsonAsync(method: HttpMethod.Post, requestData: new ODataRequest
                {
                    IsCollectionRequest = false,
                    Path = "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx",
                    ActionName = "ForceUndoCheckOut",
                });
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Take lock over")]
        public async Task Docs_Collaboration_Versioning_TakeLockOver()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: The server returned an error (HttpStatus: InternalServerError): User not found by the parameter: 12345
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var body = @"models=[{""user"": ""12345""}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "TakeLockOver", HttpMethod.Post, body);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Get version history of a content")]
        public async Task Docs_Collaboration_Versioning_VersionHistory()
        {
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "Versions", HttpMethod.Get);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Restore an old version")]
        public async Task Docs_Collaboration_Versioning_RestoreOldVersion()
        {
            // ALIGN
            // ReSharper disable once InconsistentNaming
            await using var Console = new StringWriter();
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File");
            try
            {
                // ACTION for doc
                var body = @"models=[{""version"": ""V1.0.A""}]";
                var result = await RESTCaller.GetResponseStringAsync(
                    "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "RestoreVersion", HttpMethod.Post, body);
                Console.WriteLine(result);

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        /* ====================================================================================== Approval */

    }
}
