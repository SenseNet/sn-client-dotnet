using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class UsersAndGroups : ClientIntegrationTestBase
    {
        /* ====================================================================================== Main */

        [TestMethod]
        [Description("Creating users")]
        public async Task Docs_UsersAndGroups_Main_CreateUser()
        {
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/IMS/Public", "User", "alba");
                content["LoginName"] = "alba";
                content["Email"] = "alba@sensenet.com";
                content["Password"] = "alba";
                content["Enabled"] = true;
                await content.SaveAsync();

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/IMS/Public/alba");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Disable a user")]
        public async Task Docs_UsersAndGroups_Main_DisableUser()
        {
            // ALIGN
            var c = Content.CreateNew("/Root/IMS/Public", "User", "editormanatee");
            c["LoginName"] = "editormanatee";
            c["Email"] = "editormanatee@sensenet.com";
            c["Password"] = "editormanatee";
            c["Enabled"] = true;
            await c.SaveAsync();
            try
            {
                // ACTION for doc
                var content = await Content.LoadAsync("/Root/IMS/Public/editormanatee");
                content["Enabled"] = false;
                await content.SaveAsync();

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                c = await Content.LoadAsync("/Root/IMS/Public/editormanatee");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }
        [TestMethod]
        [Description("Creating roles (groups)")]
        public async Task Docs_UsersAndGroups_Main_CreateGroup()
        {
            try
            {
                // ACTION for doc
                var content = Content.CreateNew("/Root/IMS/Public", "Group", "Publishers");
                content["Members"] = new[] { 1155, 1156 };
                await content.SaveAsync();

                // ASSERT
                Assert.Inconclusive();
            }
            finally
            {
                var c = await Content.LoadAsync("/Root/IMS/Public/Publishers");
                if (c != null)
                    await c.DeleteAsync(true);
            }
        }

        /* ====================================================================================== Login */

        /* ====================================================================================== Logout */

        /* ====================================================================================== Group Membership */

        [TestMethod]
        [Description("Load members of a group")]
        public async Task Docs_UsersAndGroups_GroupMembership_Load()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: Cannot perform runtime binding on a null reference
            // ACTION for doc
            dynamic developers = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/IMS/Public/developers",
                IsCollectionRequest = false,
                Expand = new[] { "Members" },
                Select = new[] { "Members/LoginName" }
            });
            foreach (dynamic content in developers.Members)
            {
                Console.WriteLine(content.LoginName);
            }

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Add members to a group")]
        public async Task Docs_UsersAndGroups_GroupMembership_Add()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: Content not found
            // ACTION for doc
            var body = @"models=[{""contentIds"": [ 1155, 1157 ]}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/IMS/Public/developers", "AddMembers", HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Remove members from a group")]
        public async Task Docs_UsersAndGroups_GroupMembership_Remove()
        {
            Assert.Inconclusive();
            //UNDONE:---- ERROR: Content not found
            // ACTION for doc
            var body = @"models=[{""contentIds"": [ 1157 ]}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/IMS/Public/developers", "RemoveMembers", HttpMethod.Post, body);
            Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Get all group memberships (roles) of a user")]
        public async Task Docs_UsersAndGroups_GroupMembership_GetRolesOfUser()
        {
            // ACTION for doc
            //UNDONE: Missing doc and test. REST: GET /OData.svc/Root/IMS/Public('devdog')?$select=AllRoles&$expand=AllRoles

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Get the list of groups where the user is directly added as a member")]
        public async Task Docs_UsersAndGroups_GroupMembership_GetDirectRolesOfUser()
        {
            // ACTION for doc
            //UNDONE: Missing doc and test. REST: GET /OData.svc/Root/IMS/Public('devdog')?$select=DirectRoles&$expand=DirectRoles

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Get the list of workspaces where the given user is member")]
        public async Task Docs_UsersAndGroups_GroupMembership_GetWorkspacesOfUser()
        {
            // ACTION for doc
            //UNDONE: Missing doc and test. REST: GET /OData.svc/Root/Content?query=%2BType%3AGroup %2BMembers%3A{{Id%3A1163}} .AUTOFILTERS%3AOFF&$select=Workspace/DisplayName&$expand=Workspace

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Registration */

        /* ====================================================================================== Change password */

        /* ====================================================================================== Forgotten password */

    }
}
