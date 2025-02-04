using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Diagnostics;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class UsersAndGroups : ClientIntegrationTestBase
    {
        private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(1000)).Token;
        // ReSharper disable once InconsistentNaming
        IRepository repository =>
            GetRepositoryCollection()
                .GetRepositoryAsync("local", cancel).GetAwaiter().GetResult();

        /* ====================================================================================== Main */

        /// <tab category="users-and-groups" article="users-and-groups" example="createUser" />
        [TestMethod]
        [Description("Creating users")]
        public async Task Docs2_UsersAndGroups_Main_CreateUser()
        {
            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var content = repository.CreateContent("/Root/IMS/Public", "User", "jsmith");
                content["LoginName"] = "jsmith";
                content["Email"] = "jsmith@example.com";
                content["Password"] = "I8rRp2c9P0HJZENZcvlz";
                content["FullName"] = "John Smith";
                content["Enabled"] = true;
                await content.SaveAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
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

        /// <tab category="users-and-groups" article="users-and-groups" example="disableUser" />
        [TestMethod]
        [Description("Disable a user")]
        public async Task Docs2_UsersAndGroups_Main_DisableUser()
        {
            var jsmith = repository.CreateContent("/Root/IMS/Public", "User", "jsmith");
            jsmith["LoginName"] = "jsmith";
            jsmith["Email"] = "jsmith@example.com";
            jsmith["Password"] = "I8rRp2c9P0HJZENZcvlz";
            jsmith["FullName"] = "John Smith";
            jsmith["Enabled"] = true;
            await jsmith.SaveAsync(cancel);

            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var user = await repository.LoadContentAsync<User>("/Root/IMS/Public/jsmith", cancel);
                user.Enabled = false;
                await user.SaveAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                PATCH https://localhost:44362/OData.svc/Root/IMS/Public/jsmith
                models=[{"Enabled":false}]
                */

                // ASSERT
                var loaded = await repository.LoadContentAsync<User>("/Root/IMS/Public/jsmith", cancel);
                Assert.IsFalse(loaded.Enabled);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/IMS/Public/jsmith", true, cancel);
            }
        }

        /// <tab category="users-and-groups" article="users-and-groups" example="createRole" />
        [TestMethod]
        [Description("Creating roles (groups)")]
        public async Task Docs2_UsersAndGroups_Main_CreateGroup()
        {

            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var jsmith = await repository.LoadContentAsync("/Root/IMS/Public/jjohnson", cancel);
                var etaylor = await repository.LoadContentAsync("/Root/IMS/Public/etaylor", cancel);
                var drivers = repository.CreateContent<Group>("/Root/IMS/Public", null, "Drivers"); 
                drivers.Members = new[] { jsmith, etaylor };
                await drivers.SaveAsync(cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/IMS('Public')
                models=[{
                  "__ContentType":"Group",
                  "Name":"Drivers",
                  "Members":[
                    "/Root/IMS/Public/etaylor",
                    "/Root/IMS/Public/jjohnson"
                  ]
                }] 
                */

                // ASSERT
                var group = await repository.LoadContentAsync<Group>(new LoadContentRequest
                {
                    Path = "/Root/IMS/Public/Drivers",
                    Expand =new []{"Members"},
                    Select = new []{"Id", "Path", "Type", "Members/Id", "Members/Name", "Members/Type" }
                }, cancel);
                Assert.IsNotNull(group.Members);
                var members = group.Members.OrderBy(g => g.Name).ToArray();
                Assert.AreEqual("etaylor, jjohnson", string.Join(", ", members.Select(m => m.Name)));
                Assert.AreEqual("User", members.Select(m => m.Type).Distinct().Single());
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/IMS/Public/Drivers", true, cancel);
            }
        }

        /* ====================================================================================== Group Membership */

        /// <tab category="users-and-groups" article="memberships" example="loadMembers" />
        [TestMethod]
        [Description("Load members of a group")]
        public async Task Docs2_UsersAndGroups_GroupMembership_Load()
        {
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            var group = await repository.LoadContentAsync<Group>(new LoadContentRequest
            {
                Path = "/Root/IMS/Public/Editors",
                Expand = new[] { "Members" },
                Select = new[] { "Id", "Path", "Type", "Members/Id", "Members/Path", "Members/Type", "Members/LoginName" }
            }, cancel);
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root/IMS/Public('Editors')
            ?metadata=no&$expand=Members&$select=Id,Path,Type,Members/Id,Members/Path,Members/Type,Members/LoginName
            */

            // ASSERT
            Assert.IsNotNull(group.Members);
            var members = group.Members.OrderBy(g => g.Name).ToArray();
            Assert.AreEqual("etaylor, jjohnson", string.Join(", ", members.Select(m => ((User)m).LoginName)));
        }

        /// <tab category="users-and-groups" article="memberships" example="addMember" />
        [TestMethod]
        [Description("Add members to a group")]
        public async Task Docs2_UsersAndGroups_GroupMembership_Add()
        {
            var jsmith = await repository.LoadContentAsync("/Root/IMS/Public/jjohnson", cancel);
            var etaylor = await repository.LoadContentAsync("/Root/IMS/Public/etaylor", cancel);
            await repository.InvokeActionAsync(new OperationRequest
            {
                Path = "/Root/IMS/Public/Editors",
                OperationName = "RemoveMembers",
                PostData = new{contentIds = new[] {etaylor.Id, jsmith.Id}}
            }, cancel);

            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            await repository.InvokeActionAsync(new OperationRequest
            {
                Path = "/Root/IMS/Public/Editors",
                OperationName = "AddMembers",
                PostData = new { contentIds = new[] { etaylor.Id, jsmith.Id } }
            }, cancel);
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");
            /* RAW REQUEST:
            POST https://localhost:44362/OData.svc/Root/IMS/Public('Editors')/AddMembers
            models=[{"contentIds":[1157,1158]}]
            */

            // ASSERT
            var group = await repository.LoadContentAsync<Group>(new LoadContentRequest
            {
                Path = "/Root/IMS/Public/Editors",
                Expand = new[] { "Members" },
                Select = new[] { "Id", "Path", "Type", "Members/Id", "Members/Name", "Members/Type" }
            }, cancel);
            Assert.IsNotNull(group.Members);
            var members = group.Members.OrderBy(g => g.Name).ToArray();
            Assert.AreEqual("etaylor, jjohnson", string.Join(", ", members.Select(m => ((User)m).Name)));
        }

        /// <tab category="users-and-groups" article="memberships" example="removeMember" />
        [TestMethod]
        [Description("Remove members from a group")]
        public async Task Docs2_UsersAndGroups_GroupMembership_Remove()
        {
            var jsmith = await repository.LoadContentAsync("/Root/IMS/Public/jjohnson", cancel);
            var etaylor = await repository.LoadContentAsync("/Root/IMS/Public/etaylor", cancel);
            try
            {
                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/IMS/Public/Editors",
                    OperationName = "RemoveMembers",
                    PostData = new { contentIds = new[] { etaylor.Id, jsmith.Id } }
                }, cancel);
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                POST https://localhost:44362/OData.svc/Root/IMS/Public('Editors')/RemoveMembers
                models=[{"contentIds":[1157,1158]}]
                */

                // ASSERT
                var group = await repository.LoadContentAsync<Group>(new LoadContentRequest
                {
                    Path = "/Root/IMS/Public/Editors",
                    Expand = new[] {"Members"},
                    Select = new[] {"Id", "Path", "Type", "Members/Id", "Members/Name", "Members/Type"}
                }, cancel);
                Assert.IsNotNull(group.Members);
                Assert.IsFalse(group.Members.Any());
            }
            finally
            {
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/IMS/Public/Editors",
                    OperationName = "AddMembers",
                    PostData = new { contentIds = new[] { etaylor.Id, jsmith.Id } }
                }, cancel);
            }
        }

        /// <tab category="users-and-groups" article="memberships" example="allRoles" />
        [TestMethod]
        [Description("Get all group memberships (roles) of a user")]
        public async Task Docs2_UsersAndGroups_GroupMembership_GetRolesOfUser()
        {
            var editors = await repository.LoadContentAsync("/Root/IMS/Public/Editors", cancel);
            try
            {
                await repository.InvokeActionAsync(
                    new OperationRequest
                    {
                        Path = "/Root/IMS/Builtin/Portal/Developers", OperationName = "AddMembers",
                        PostData = new {contentIds = new[] {editors.Id}}
                    }, cancel);

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var user = await repository.LoadContentAsync<User>(new LoadContentRequest
                {
                    Path = "/Root/IMS/Public/jjohnson",
                    Expand = new[] { "AllRoles" },
                    Select = new[] { "Id", "Path", "Type", "AllRoles/Id", "AllRoles/Path", "AllRoles/Type", "AllRoles/Name" }
                }, cancel);
                var roles = user.AllRoles?
                    .Select(role => role.Name)
                    .ToArray();
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root/IMS/Public('jjohnson')
                ?metadata=no&$expand=AllRoles&$select=Id,Path,Type,AllRoles/Id,AllRoles/Path,AllRoles/Type,AllRoles/Name
                */

                // ASSERT
                Assert.IsNotNull(roles);
                Assert.IsTrue(roles.Length > 1);
                Assert.IsTrue(roles.Contains("Editors"));
                Assert.IsTrue(roles.Contains("Developers"));
            }
            finally
            {
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/IMS/Builtin/Portal/Developers",
                    OperationName = "RemoveMembers",
                    PostData = new { contentIds = new[] { editors.Id } }
                }, cancel);
            }
        }

        /// <tab category="users-and-groups" article="memberships" example="directRoles" />
        [TestMethod]
        [Description("Get the list of groups where the user is directly added as a member")]
        public async Task Docs2_UsersAndGroups_GroupMembership_GetDirectRolesOfUser()
        {
            var editors = await repository.LoadContentAsync("/Root/IMS/Public/Editors", cancel);
            try
            {
                await repository.InvokeActionAsync(
                    new OperationRequest
                    {
                        Path = "/Root/IMS/Builtin/Portal/Developers",
                        OperationName = "AddMembers",
                        PostData = new { contentIds = new[] { editors.Id } }
                    }, cancel);

                SnTrace.Test.Write(">>>> ACT");
                /*<doc>*/
                var user = await repository.LoadContentAsync<User>(new LoadContentRequest
                {
                    Path = "/Root/IMS/Public/jjohnson",
                    Expand = new[] { "DirectRoles" },
                    Select = new[] { "Id", "Path", "Type", "DirectRoles/Id", "DirectRoles/Path", "DirectRoles/Type", "DirectRoles/Name" }
                }, cancel);
                var roles = user.DirectRoles?
                    .Select(role => role.Name)
                    .ToArray();
                /*</doc>*/
                SnTrace.Test.Write(">>>> ACT END");
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root/IMS/Public('jjohnson')?metadata=no&$expand=DirectRoles
                &$select=Id,Path,Type,DirectRoles/Id,DirectRoles/Path,DirectRoles/Type,DirectRoles/Name
                */

                // ASSERT
                Assert.IsNotNull(roles);
                Assert.IsTrue(roles.Length == 1);
                Assert.IsTrue(roles.Contains("Editors"));
            }
            finally
            {
                await repository.InvokeActionAsync(new OperationRequest
                {
                    Path = "/Root/IMS/Builtin/Portal/Developers",
                    OperationName = "RemoveMembers",
                    PostData = new { contentIds = new[] { editors.Id } }
                }, cancel);
            }
        }
    }
}
