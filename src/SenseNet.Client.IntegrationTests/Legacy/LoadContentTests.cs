using Newtonsoft.Json.Linq;

namespace SenseNet.Client.IntegrationTests.Legacy
{
    [TestClass]
    public class LoadContentTests : IntegrationTestBase
    {
        private CancellationToken _cancel = new CancellationTokenSource().Token;

        [ClassInitialize]
        public static void ClassInitializer(TestContext context)
        {
            Initializer.InitializeServer(context);
        }

        [TestMethod]
        public async Task Load_NotFound()
        {
            // load a non-existing content: this should not throw an exception
            var content = await Content.LoadAsync("/Root/ThisPathDoesNotExist").ConfigureAwait(false);

            Assert.IsNull(content);
        }

        [TestMethod]
        public async Task LoadById()
        {
            var content = await Content.LoadAsync(Constants.User.AdminId).ConfigureAwait(false);

            Assert.IsNotNull(content);
            Assert.AreEqual(Constants.User.AdminId, content.Id);
        }

        [TestMethod]
        public async Task LoadReferences_Default()
        {
            var admins = await Content.LoadReferencesAsync(Constants.Group.AdministratorsPath, "Members").ConfigureAwait(false);
            var admin = admins.FirstOrDefault();

            Assert.IsNotNull(admin);
            Assert.AreEqual(Constants.User.AdminId, admin.Id);
            Assert.IsNotNull(admin["DisplayName"]); // all fields should be present in the result
        }

        [TestMethod]
        public async Task LoadReferences_WithSelect()
        {
            // ------------------------------------ load by path

            var admins = await Content.LoadReferencesAsync(Constants.Group.AdministratorsPath, "Members", new[] { "Id", "Path" })
                .ConfigureAwait(false);
            var admin = admins.FirstOrDefault();

            Assert.IsNotNull(admin);
            Assert.AreEqual(Constants.User.AdminId, admin.Id);
            Assert.IsNull(admin["DisplayName"]); // because we did not select this field

            // ------------------------------------ load by id

            var adminsGroup = await Content.LoadAsync(Constants.Group.AdministratorsPath).ConfigureAwait(false);
            admins = await Content.LoadReferencesAsync(adminsGroup.Id, "Members", new[] { "Id", "Path" }).ConfigureAwait(false);
            admin = admins.FirstOrDefault();

            Assert.IsNotNull(admin);
            Assert.AreEqual(Constants.User.AdminId, admin.Id);
            Assert.IsNull(admin["DisplayName"]); // because we did not select this field
        }

        [TestMethod]
        public async Task LoadReferences_DynamicProperty()
        {
            dynamic adminGroup = await Content.LoadAsync(new ODataRequest
            {
                Path = Constants.Group.AdministratorsPath,
                Expand = new[] { "Members" },
                Select = new[] { "Id", "Name", "Members/Id", "Members/Path", "Members/Type", "Members/CreationDate" },
                SiteUrl = ServerContext.GetUrl(null)
            })
            .ConfigureAwait(false);

            IEnumerable<dynamic> members = adminGroup.Members;
            Assert.IsNotNull(members);

            // Use items as dynamic, without converting them to Content.
            // This is OK if you do not need the Content class' functionality.
            dynamic admin = members.FirstOrDefault();
            Assert.IsNotNull(admin);

            int id = admin.Id;
            string path = admin.Path;
            DateTime cd = admin.CreationDate;

            Assert.AreEqual(Constants.User.AdminId, id);
            Assert.AreEqual(Constants.User.AdminPath, path);
        }

        [TestMethod]
        public async Task LoadReferences_DynamicProperty_Enumerable()
        {
            dynamic adminGroup = await Content.LoadAsync(new ODataRequest
            {
                Path = Constants.Group.AdministratorsPath,
                Expand = new[] { "Members" },
                Select = new[] { "Id", "Name", "Members/Id", "Members/Name", "Members/Path", "Members/Type", "Members/CreationDate", "Members/Index" },
                SiteUrl = ServerContext.GetUrl(null)
            })
            .ConfigureAwait(false);

            //var members = ((IEnumerable<dynamic>)adminGroup.Members).ToContentEnumerable();
            //var members = adminGroup.Members.ToContentEnumerable();
            var members = ContentExtensions.ToContentEnumerable(adminGroup.Members);

            foreach (dynamic member in members)
            {
                int newIndex = member.Index + 1;
                member.Index = newIndex;

                // use the client Content API, this was the purpose of the ToContentEnumerable extension method
                await member.SaveAsync().ConfigureAwait(false);

                // load it again from the server
                dynamic tempContent = await Content.LoadAsync(member.Id).ConfigureAwait(false);

                Assert.AreEqual(newIndex, (int)tempContent.Index);
            }
        }

        [TestMethod]
        public async Task LoadReferences_DynamicProperty_MultiRef_WithFilter()
        {
            // This request loads references and expands their reference field.
            var members = (await Content.LoadReferencesAsync(new ODataRequest
            {
                Path = Constants.Group.AdministratorsPath,
                PropertyName = "Members",
                Expand = new[] { "CreatedBy" },
                Select = new[] { "Id", "Name", "Path", "Index", "Type",
                    "CreatedBy/Id", "CreatedBy/Name", "CreatedBy/Path", "CreatedBy/Type", "CreatedBy/CreationDate", "CreatedBy/Index" },
                OrderBy = new[] { "Id" },
                Top = 1,
                Skip = 1
            })).ToArray();

            // there are actually 2 members but we skipped one in the filter above
            Assert.AreEqual(1, members.Length);

            dynamic content = members[0];
            string createdByName = content.CreatedBy.Name;

            Assert.AreEqual("Developers", content.Name);
            Assert.AreEqual("Group", (string)content.Type);
            Assert.IsTrue(content.Index > -1);
            Assert.AreEqual("Admin", createdByName);
        }
        [TestMethod]
        public async Task LoadReferences_DynamicProperty_SingleRef()
        {
            // Single will make sure that the array contains only 1 element
            var reference = (await Content.LoadReferencesAsync(new ODataRequest
            {
                Path = Constants.User.AdminPath,
                PropertyName = "CreatedBy",
            })).Single();

            Assert.AreEqual("Admin", reference.Name);
        }

        [TestMethod]
        public async Task Load_PropertyAccess_DateTime()
        {
            var content = await Content.LoadAsync(Constants.User.AdminId).ConfigureAwait(false);
            dynamic dContent = content;

            Assert.IsNotNull(content);

            var date1 = ((JValue)content["CreationDate"]).Value<DateTime>();
            var date2 = Convert.ToDateTime(content["CreationDate"]);
            DateTime date3 = dContent.CreationDate;

            var baseDate = new DateTime(2015, 1, 1);

            Assert.IsTrue(date1 > baseDate);
            Assert.IsTrue(date2 > baseDate);
            Assert.IsTrue(date3 > baseDate);
            Assert.IsTrue(date1 == date2);
            Assert.IsTrue(date2 == date3);
        }

        [TestMethod]
        public async Task Query_Simple()
        {
            var tasks = await Content.QueryAsync("+TypeIs:Folder",
                new[] { "Id", "Name", "Path", "DisplayName", "Description" },
                settings: new QuerySettings
                {
                    EnableAutofilters = FilterStatus.Disabled,
                    Top = 5
                })
                .ConfigureAwait(false);

            var count = tasks.Count();

            Assert.IsTrue(count > 0 && count <= 5);
        }

        [TestMethod]
        public async Task Query_Complex()
        {
            var tasks = await Content.QueryAsync("+TypeIs:(File Folder) +Name:(*e* *.js) +CreationDate:>'2000-01-01'",
                new[] { "Id", "Name", "Path", "DisplayName", "Description" },
                settings: new QuerySettings
                {
                    EnableAutofilters = FilterStatus.Disabled,
                    Top = 5
                })
                .ConfigureAwait(false);

            var count = tasks.Count();

            Assert.IsTrue(count > 0 && count <= 5);
        }

        [TestMethod]
        public async Task GetCurrentUser()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", CancellationToken.None).ConfigureAwait(false);

            var server = repository.Server;
            var originalToken = server.Authentication.AccessToken;

            try
            {
                // first check as Visitor
                server.Authentication.AccessToken = null;
                var visitor = await repository.GetCurrentUserAsync(_cancel).ConfigureAwait(false);

                Assert.AreEqual("Visitor", visitor.Name);
            }
            finally
            {
                server.Authentication.AccessToken = originalToken;
            }

            // we assume that the global test user is the Admin
            dynamic currentUser = await repository.GetCurrentUserAsync(_cancel).ConfigureAwait(false);
            Assert.AreEqual("Admin", currentUser.Name);

            // check if expansion works
            currentUser = await repository.GetCurrentUserAsync(new[] { "Id", "Name", "Path", "Type", "CreatedBy/Id", "CreatedBy/Name", "CreatedBy/Type" },
                new[] { "CreatedBy" }, _cancel).ConfigureAwait(false);

            Assert.AreEqual("Admin", (string)currentUser.CreatedBy.Name);
        }
    }
}
