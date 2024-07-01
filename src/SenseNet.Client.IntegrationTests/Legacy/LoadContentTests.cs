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
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            // load a non-existing content: this should not throw an exception
            var content = await repository.LoadContentAsync("/Root/ThisPathDoesNotExist", _cancel).ConfigureAwait(false);

            Assert.IsNull(content);
        }

        [TestMethod]
        public async Task LoadById()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var content = await repository.LoadContentAsync(Constants.User.AdminId, _cancel).ConfigureAwait(false);

            Assert.IsNotNull(content);
            Assert.AreEqual(Constants.User.AdminId, content.Id);
        }

        [TestMethod]
        public async Task LoadReferences_DynamicProperty()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
            
            var adminGroup = await repository.LoadContentAsync<Group>(new LoadContentRequest
            {
                Path = Constants.Group.AdministratorsPath,
                Expand = new[] { "Members" },
                Select = new[] { "Id", "Name", "Members/Id", "Members/Path", "Members/Type", "Members/CreationDate" },
            }, _cancel);

            Assert.IsNotNull(adminGroup);
            var members = adminGroup.Members;
            Assert.IsNotNull(members);

            var admin = members.FirstOrDefault();
            Assert.IsNotNull(admin);

            int id = admin.Id;
            var path = admin.Path;
            var cd = admin.CreationDate;

            Assert.AreEqual(Constants.User.AdminId, id);
            Assert.AreEqual(Constants.User.AdminPath, path);
        }

        [TestMethod]
        public async Task LoadReferences_DynamicProperty_Enumerable()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var adminGroup = await repository.LoadContentAsync<Group>(new LoadContentRequest
            {
                Path = Constants.Group.AdministratorsPath,
                Expand = new[] {"Members"},
                Select = new[]
                {
                    "Id", "Name", "Members/Id", "Members/Name", "Members/Path", "Members/Type", "Members/CreationDate",
                    "Members/Index"
                },
            }, _cancel);

            //var members = ((IEnumerable<dynamic>)adminGroup.Members).ToContentEnumerable();
            //var members = adminGroup.Members.ToContentEnumerable();
            var members = ContentExtensions.ToContentEnumerable(adminGroup.Members);

            foreach (var member in members)
            {
                int newIndex = (member.Index ?? 0) + 1;
                member.Index = newIndex;

                // use the client Content API, this was the purpose of the ToContentEnumerable extension method
                await member.SaveAsync().ConfigureAwait(false);

                // load it again from the server
                dynamic tempContent = await repository.LoadContentAsync(member.Id, _cancel).ConfigureAwait(false);

                Assert.AreEqual(newIndex, (int)tempContent.Index);
            }
        }

        [TestMethod]
        public async Task Load_PropertyAccess_DateTime()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var content = await repository.LoadContentAsync(Constants.User.AdminId, _cancel).ConfigureAwait(false);
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
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var tasks = await repository.QueryAsync(new QueryContentRequest
            {
                ContentQuery = "+TypeIs:Folder",
                Select = new[] { "Id", "Name", "Path", "DisplayName", "Description" },
                Top = 5,
                AutoFilters = FilterStatus.Disabled
            }, _cancel).ConfigureAwait(false);

            var count = tasks.Count();

            Assert.IsTrue(count > 0 && count <= 5);
        }

        [TestMethod]
        public async Task Query_Complex()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
            
            var tasks = await repository.QueryAsync(new QueryContentRequest
            {
                ContentQuery = "+TypeIs:(File Folder) +Name:(*e* *.js) +CreationDate:>'2000-01-01'",
                Select = new[] { "Id", "Name", "Path", "DisplayName", "Description" },
                Top = 5,
                AutoFilters = FilterStatus.Disabled
            }, _cancel).ConfigureAwait(false);

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
