using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Security;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Diagnostics;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Permissions : ClientIntegrationTestBase
    {
        private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(1000)).Token;
        // ReSharper disable once InconsistentNaming
        IRepository repository =>
            GetRepositoryCollection()
                .GetRepositoryAsync("local", cancel).GetAwaiter().GetResult();

        /* ====================================================================================== Main */

        /// <tab category="permissions" article="permissions" example="getPermissionEntries" />
        [TestMethod]
        [Description("Get permission entries of a content")]
        public async Task Docs_Permissions_Main_GetPermissions_CurrentUser()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            //UNDONE:- Feature request: Content.GetPermissionAsync
            /*
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync("/Root/Content/IT", "GetPermissions");

            // ASSERT
            Assert.Inconclusive();
            */
        }

        /// <tab category="permissions" article="permissions" example="getAcl" />
        [TestMethod]
        [Description("Get full access control list of a content")]
        public async Task Docs_Permissions_Main_GetAcl()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            //UNDONE:- Feature request: Content.GetAcl()
            // ACTION for doc
            //UNDONE: Missing doc and test. REST: GET /OData.svc/Root/Content('IT')/GetAcl

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="permissions" article="permissions" example="getPermissionEntry" />
        [TestMethod]
        [Description("Get a permissions entry of a specific user or group")]
        public async Task Docs_Permissions_Main_GetPermissions_Specific()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            //UNDONE:- Feature request: Content.GetPermissionAsync(identity)
            /*
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
            */
        }

        /// <tab category="permissions" article="permissions" example="hasPermission" />
        [TestMethod]
        [Description("Check user access")]
        public async Task Docs_Permissions_Main_CheckPermission()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            // ACTION for doc
            var content = await Content.LoadAsync("/Root/Content/IT").ConfigureAwait(false);
            var hasPermission = await content.HasPermissionAsync(new[] { "Open" }, null, cancel);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="permissions" article="permissions" example="hasPermissionUser" />
        [TestMethod]
        [Description("How can I check why a user cannot access a content?")]
        public async Task Docs_Permissions_Main_HasPermission_Open()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


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
                var hasPermission = await content.HasPermissionAsync(new[] { "Open" }, "/Root/IMS/Public/devdog", cancel);

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

        /// <tab category="permissions" article="permissions" example="canSave" />
        [TestMethod]
        [Description("How can I check why a user cannot save a content?")]
        public async Task Docs_Permissions_Main_HasPermission_Save()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


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
                var hasPermission = await content.HasPermissionAsync(new[] { "Open,Save" }, "/Root/IMS/Public/devdog", cancel);

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

        /// <tab category="permissions" article="permissions" example="canSeePermissions" />
        [TestMethod]
        [Description("Check if I can see the permission settings")]
        public async Task Docs_Permissions_Main_HasPermission_See()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


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
                var hasPermission = await content.HasPermissionAsync(new[] { "SeePermissions" }, "/Root/IMS/Public/devdog", cancel);

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

        /// <tab category="permissions" article="permission-management" example="allowSave" />
        [TestMethod]
        [Description("Allow a user to save a content")]
        public async Task Docs_Permissions_Management_AllowUser()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            //UNDONE:- Feature request: content.SetPermissionAsync(...)
            Assert.Inconclusive();
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
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest, repository, cancel);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="permissions" article="permission-management" example="allowApproveForAGroup" />
        [TestMethod]
        [Description("Allow a group (role) to approve content in a document library")]
        public async Task Docs_Permissions_Management_AllowGroup()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


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
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest, repository, cancel);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="permissions" article="permission-management" example="denyDelete" />
        [TestMethod]
        [Description("Prohibit a user from deleting content from a folder")]
        public async Task Docs_Permissions_Management_Deny()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


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
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest, repository, cancel);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="permissions" article="permission-management" example="breakInheritance" />
        [TestMethod]
        [Description("Break inheritance")]
        public async Task Docs_Permissions_Management_Break()
        {
            //UNDONE:Docs2: not implemented
            //Assert.Inconclusive();

            await EnsureContentAsync("/Root/Content/xxx", "SystemFolder", repository, cancel);
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            var content = await repository.LoadContentAsync("/Root/Content/xxx", cancel);
            await content.BreakInheritanceAsync(cancel);
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");

            await repository.DeleteContentAsync("/Root/Content/xxx", true, cancel);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="permissions" article="permission-management" example="localOnly" />
        [TestMethod]
        [Description("Local only")]
        public async Task Docs_Permissions_Management_LocalOnly()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


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
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest, repository, cancel);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="permissions" article="permission-management" example="customPermission" />
        [TestMethod]
        [Description("Using custom permissions")]
        public async Task Docs_Permissions_Management_Custom()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


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
            await SecurityManager.SetPermissionsAsync(content.Id, permissionRequest, repository, cancel);

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Complex Permission Queries */

        /// <tab category="permissions" article="permission-queries" example="getRelatedIdentities" />
        [TestMethod]
        [Description("Get all identities connected to a content")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedIdentities()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            //UNDONE:- Feature request: PermissionQueries methods
            Assert.Inconclusive();
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
            */
        }

        /// <tab category="permissions" article="permission-queries" example="getRelatedPermissions" />
        [TestMethod]
        [Description("Count number of permissions settings per identity")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedPermissions()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
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
            */
        }

        /// <tab category="permissions" article="permission-queries" example="getRelatedItems" />
        [TestMethod]
        [Description("Get content with permission settings for a specific identity")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedItems()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            // ACTION for doc
            /*
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
            */
        }

        /// <tab category="permissions" article="permission-queries" example="getRelatedIdentitiesByPermissions" />
        [TestMethod]
        [Description("Get identities related to a permission in a subtree")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedIdentitiesByPermissions()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
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
            */
        }

        /// <tab category="permissions" article="permission-queries" example="getRelatedItemsOneLevel" />
        [TestMethod]
        [Description("Get contents related to a permission in a container")]
        public async Task Docs_Permissions_PermissionQueries_GetRelatedItemsOneLevel()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
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
            */
        }

        /// <tab category="permissions" article="permission-queries" example="getAllowedUsers" />
        [TestMethod]
        [Description("Get list of users allowed to do something")]
        public async Task Docs_Permissions_PermissionQueries_GetAllowedUsers()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
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
            */
        }

        /// <tab category="permissions" article="permission-queries" example="getParentGroups" />
        [TestMethod]
        [Description("List of group memberships of a user")]
        public async Task Docs_Permissions_PermissionQueries_GetParentGroups()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
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
            */
        }
    }
}
