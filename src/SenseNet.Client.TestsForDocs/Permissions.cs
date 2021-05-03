using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Security;
using SenseNet.Client.TestsForDocs.Infrastructure;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Permissions : ClientIntegrationTestBase
    {
        /* ====================================================================================== Main */

        [TestMethod]
        [Description("Get permission entries of a content")]
        public async Task Docs_Permissions_Main_GetPermissions_CurrentUser()
        {
            //UNDONE:- Feature request: Content.GetPermissionAsync
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync("/Root/Content/IT", "GetPermissions");

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Get full access control list of a content")]
        public async Task Docs_Permissions_Main_GetAcl()
        {
            //UNDONE:- Feature request: Content.GetAcl()
            // ACTION for doc
            //UNDONE: Missing doc and test. REST: GET /OData.svc/Root/Content('IT')/GetAcl

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Get a permissions entry of a specific user or group")]
        public async Task Docs_Permissions_Main_GetPermissions_Specific()
        {
            //UNDONE:- Feature request: Content.GetPermissionAsync(identity)
            // ACTION for doc
            var req = new ODataRequest
            {
                Path = "/Root/Content/IT",
                ActionName = "GetPermissions",
                Parameters = { { "identity", "/Root/IMS/Public/Editors" } }
            };
            var result = await RESTCaller.GetResponseStringAsync(req);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Check user access")]
        public async Task Docs_Permissions_Main_CheckPermission()
        {
            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT").ConfigureAwait(false);
            var hasPermission = await content.HasPermissionAsync(new[] { "Open" });

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("How can I check why a user cannot access a content?")]
        public async Task Docs_Permissions_Main_HasPermission_Open()
        {
            // ALIGN
            var user = Content.CreateNew("/Root/IMS/Public", "User", "devdog");
            user["LoginName"] = "devdog";
            user["Email"] = "devdog@sensenet.com";
            user["Password"] = "devdog";
            user["Enabled"] = true;
            await user.SaveAsync();
            try
            {
                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT").ConfigureAwait(false);
                var hasPermission = await content.HasPermissionAsync(new[] { "Open" }, "/Root/IMS/Public/devdog");

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                user = await Content.LoadAsync("/Root/IMS/Public/devdog");
                if (user != null)
                    await user.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("How can I check why a user cannot save a content?")]
        public async Task Docs_Permissions_Main_HasPermission_Save()
        {
            //UNDONE: BUG: HasPermissionAsync method does not thrown any exception if the target user does not exist.
            // ALIGN
            var user = Content.CreateNew("/Root/IMS/Public", "User", "devdog");
            user["LoginName"] = "devdog";
            user["Email"] = "devdog@sensenet.com";
            user["Password"] = "devdog";
            user["Enabled"] = true;
            await user.SaveAsync();
            try
            {
                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT").ConfigureAwait(false);
                var hasPermission = await content.HasPermissionAsync(new[] { "Open,Save" }, "/Root/IMS/Public/devdog");

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                user = await Content.LoadAsync("/Root/IMS/Public/devdog");
                if (user != null)
                    await user.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Check if I can see the permission settings")]
        public async Task Docs_Permissions_Main_HasPermission_See()
        {
            // ALIGN
            var user = Content.CreateNew("/Root/IMS/Public", "User", "devdog");
            user["LoginName"] = "devdog";
            user["Email"] = "devdog@sensenet.com";
            user["Password"] = "devdog";
            user["Enabled"] = true;
            await user.SaveAsync();
            try
            {
                // ACTION for doc
                var content = await Content.LoadAsync("/Root/Content/IT").ConfigureAwait(false);
                var hasPermission = await content.HasPermissionAsync(new[] { "SeePermissions" }, "/Root/IMS/Public/devdog");

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                user = await Content.LoadAsync("/Root/IMS/Public/devdog");
                if (user != null)
                    await user.DeleteAsync(true);
            }
        }

        /* ====================================================================================== Permission Management */

        [TestMethod]
        [Description("Allow a user to save a content")]
        public async Task Docs_Permissions_Management_AllowUser()
        {
            //UNDONE:- Feature request: content.SetPermissionAsync(...)
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var permissionRequest = new[]
            {
                new SetPermissionRequest
                {
                    Identity = "/Root/IMS/BuiltIn/Portal/Visitor",
                    Save = PermissionValue.Allow,
                }
            };
            var content = await Content.LoadAsync("/Root/Content/IT");
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Allow a group (role) to approve content in a document library")]
        public async Task Docs_Permissions_Management_AllowGroup()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT/Document_Library");
            var permissionRequest = new[]
            {
                new SetPermissionRequest
                {
                    Identity = "/Root/IMS/Public/Editors",
                    Approve = PermissionValue.Allow,
                }
            };
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Prohibit a user from deleting content from a folder")]
        public async Task Docs_Permissions_Management_Deny()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT/Document_Library");
            var permissionRequest = new[]
            {
                new SetPermissionRequest
                {
                    Identity = "/Root/IMS/Public/Editors",
                    Delete = PermissionValue.Deny,
                }
            };
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Break inheritance")]
        public async Task Docs_Permissions_Management_Break()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT/Document_Library");
            await content.BreakInheritanceAsync();

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Local only")]
        public async Task Docs_Permissions_Management_LocalOnly()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT/Document_Library");
            var permissionRequest = new[]
            {
                new SetPermissionRequest
                {
                    Identity = "/Root/IMS/Public/Editors",
                    LocalOnly = true,
                    AddNew = PermissionValue.Allow,
                }
            };
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Using custom permissions")]
        public async Task Docs_Permissions_Management_Custom()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT/Document_Library");
            var permissionRequest = new[]
            {
                new SetPermissionRequest
                {
                    Identity = "/Root/IMS/Public/Editors",
                    Custom01 = PermissionValue.Allow,
                }
            };
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest);

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Complex Permission Queries */

        [TestMethod]
        [Description("Get all identities connected to a content")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedIdentities()
        {
            //UNDONE:- Feature request: PermissionQueries methods
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                ActionName = "GetRelatedIdentities",
                Select = new[] { "Id", "Path", "Type" },
                Parameters =
                {
                    { "permissionLevel", "AllowedOrDenied" },
                    { "identityKind", "Groups" }
                }
            });
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedPermissions()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var body = @"models=[{
                ""permissionLevel"": ""AllowedOrDenied"",
                ""memberPath"": ""/Root/IMS/Public/Editors"",
                ""includedTypes"": null,
                ""explicitOnly"": true}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "GetRelatedPermissions", HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedItems()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var req = new ODataRequest
            {
                Path = "/Root/Content/IT",
                ActionName = "GetRelatedItems",
                Select = new[] { "Id", "Path", "Type" },
            };
            var body = @"models=[{
                ""permissionLevel"": ""AllowedOrDenied"",
                ""memberPath"": ""/Root/IMS/Public/Editors"",
                ""permissions"": [""Save""],
                ""explicitOnly"": true}]";
            var result = await RESTCaller.GetResponseStringAsync(req, HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedIdentitiesByPermissions()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var req = new ODataRequest
            {
                Path = "/Root/Content/IT",
                ActionName = "GetRelatedIdentitiesByPermissions",
                Select = new[] { "Id", "Path", "Type" },
            };
            var body = @"models=[{
                ""permissionLevel"": ""AllowedOrDenied"",
                ""identityKind"": ""Groups"",
                ""permissions"": [""Save""]}]";
            var result = await RESTCaller.GetResponseStringAsync(req, HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedItemsOneLevel()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var req = new ODataRequest
            {
                Path = "/Root/Content/IT",
                ActionName = "GetRelatedItemsOneLevel",
                Select = new[] { "Id", "Path", "Type" },
            };
            var body = @"models=[{
                ""permissionLevel"": ""AllowedOrDenied"",
                ""memberPath"": ""/Root/IMS/Public/Editors"",
                ""permissions"": [""Open""]}]";
            var result = await RESTCaller.GetResponseStringAsync(req, HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("")]
        public async Task Docs_Permissions_PermissionQueries_GetAllowedUsers()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library/Chicago/BusinessPlan.docx",
                ActionName = "GetAllowedUsers",
                Select = new[] { "Id", "Path", "Type" },
                Parameters = { { "permissions", "Open" } }
            });
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("")]
        public async Task Docs_Permissions_PermissionQueries_GetParentGroups()
        {
            Assert.Inconclusive();
            //UNDONE: this test has not run yet
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(new ODataRequest
            {
                Path = "/Root/IMS/Public/businesscat",
                ActionName = "GetParentGroups",
                Select = new[] { "Id", "Path", "Type" },
                Parameters = { { "directOnly", "true" } }
            });
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
    }
}
