using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using SenseNet.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SenseNet.Client;
using SenseNet.Testing;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class RepositoryTests
    {
        private const string ExampleUrl = "https://example.com";

        [TestMethod]
        public async Task Repository_Default()
        {
            // ALIGN
            var repositoryCollection = GetRepositoryCollection(services =>
            {
                services.ConfigureSenseNetRepository(opt => { opt.Url = ExampleUrl; });
            });

            // ACT
            var repository = await repositoryCollection.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual(ExampleUrl, repository.Server.Url);
        }
        [TestMethod]
        public async Task Repository_Named()
        {
            // ALIGN
            var repositoryCollection = GetRepositoryCollection(services =>
            {
                services.ConfigureSenseNetRepository("repo1", opt => { opt.Url = ExampleUrl; });
                services.ConfigureSenseNetRepository("repo2", opt => { opt.Url = "https://url2"; });
            });

            // ACT
            var repo = await repositoryCollection.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);
            var repo1 = await repositoryCollection.GetRepositoryAsync("repo1", CancellationToken.None).ConfigureAwait(false);
            var repo2 = await repositoryCollection.GetRepositoryAsync("repo2", CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            Assert.IsNull(repo.Server.Url);
            Assert.AreEqual(ExampleUrl, repo1.Server.Url);
            Assert.AreEqual("https://url2", repo2.Server.Url);
        }

        /* ====================================================================== CONTENT CREATION */

        [TestMethod]
        public async Task Repository_CreateContent()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync("local", CancellationToken.None);

            dynamic content = repository.CreateContent("/Root/Content/MyTasks", "Task", "Task1");

            Assert.AreEqual("/Root/Content/MyTasks", content.ParentPath);
            Assert.AreEqual("Task", content.__ContentType);
            Assert.AreEqual("Task1", content.Name);
        }
        [TestMethod]
        public async Task Repository_CreateContent_Error_MissingParentPath()
        {
            await TestParameterError<ArgumentNullException>(
                repository => repository.CreateContent(null, null, null),
                "Value cannot be null. (Parameter 'parentPath')").ConfigureAwait(false);
        }
        [TestMethod]
        public async Task Repository_CreateContent_Error_MissingContentType()
        {
            await TestParameterError<ArgumentNullException>(
                repository => repository.CreateContent("/Root/Content", null, null),
                "Value cannot be null. (Parameter 'contentTypeName')").ConfigureAwait(false);
        }
        [TestMethod]
        public async Task Repository_CreateContent_Error_MissingName()
        {
            await TestParameterError<ArgumentNullException>(
                repository => repository.CreateContent("/Root/Content", "Folder", null),
                "Value cannot be null. (Parameter 'name')").ConfigureAwait(false);
        }
        [TestMethod]
        public async Task Repository_CreateContent_Error_EmptyParentPath()
        {
            await TestParameterError<ArgumentException>(
                repository => repository.CreateContent("", null, null),
                "Value cannot be empty. (Parameter 'parentPath')").ConfigureAwait(false);
        }
        [TestMethod]
        public async Task Repository_CreateContent_Error_EmptyContentType()
        {
            await TestParameterError<ArgumentException>(
                repository => repository.CreateContent("/Root/Content", "", null),
                "Value cannot be empty. (Parameter 'contentTypeName')").ConfigureAwait(false);
        }
        [TestMethod]
        public async Task Repository_CreateContent_Error_EmptyName()
        {
            await TestParameterError<ArgumentException>(
                repository => repository.CreateContent("/Root/Content", "Folder", ""),
                "Value cannot be empty. (Parameter 'name')").ConfigureAwait(false);
        }


        [TestMethod]
        public async Task Repository_CreateContentByTemplate()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync("local", CancellationToken.None);

            dynamic content = repository.CreateContentByTemplate(
                "/Root/Content/MyTasks", "Task", "Task1", "Template1");

            Assert.AreEqual("/Root/Content/MyTasks", content.ParentPath);
            Assert.AreEqual("Task", content.__ContentType);
            Assert.AreEqual("Task1", content.Name);
            Assert.AreEqual("Template1", content.__ContentTemplate);
        }
        [TestMethod]
        public async Task Repository_CreateContentByTemplate_Error_MissingContentTemplate()
        {
            await TestParameterError<ArgumentNullException>(
                repository => repository.CreateContentByTemplate(
                    "/Root/Content/MyTasks", "Task", "Task1", null),
                "Value cannot be null. (Parameter 'contentTemplate')").ConfigureAwait(false);
        }
        [TestMethod]
        public async Task Repository_CreateContentByTemplate_Error_EmptyContentTemplate()
        {
            await TestParameterError<ArgumentException>(
                repository => repository.CreateContentByTemplate(
                    "/Root/Content/MyTasks", "Task", "Task1", ""),
                "Value cannot be empty. (Parameter 'contentTemplate')").ConfigureAwait(false);
        }

        public async Task TestParameterError<TException>(Action<IRepository> callback, string expectedMessage) where TException : Exception
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync("local", CancellationToken.None);

            try
            {
                callback(repository);
                Assert.Fail($"The expected {typeof(TException).Name} was not thrown.");
            }
            catch (TException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
            }
        }

        /* ====================================================================== LOAD CONTENT SHORTCUT */

        [TestMethod]
        public async Task Repository_LoadContent_ByPath()
        {
            // ALIGN
            var infrastructure = await GetDefaultRepositoryAndRestCallerMock();
            var restCaller = infrastructure.RestCaller;
            var repository = infrastructure.Repository;

            // ACT
            var content = await repository.LoadContentAsync("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root('Content')?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Content", content.Name);
        }
        [TestMethod]
        public async Task Repository_LoadContent_ById()
        {
            // ALIGN
            var infrastructure = await GetDefaultRepositoryAndRestCallerMock();
            var restCaller = infrastructure.RestCaller;
            var repository = infrastructure.Repository;

            // ACT
            var content = await repository.LoadContentAsync(42, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/content(42)?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Content", content.Name);
        }

        private async Task<(IRepository Repository, IRestCaller RestCaller)> GetDefaultRepositoryAndRestCallerMock()
        {
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Name"": ""Content"" }}"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton<IRestCaller>(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            return (repository, restCaller);
        }

        /* ====================================================================== LOAD CONTENT BY ODATA REQUEST */

        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Id()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) { ContentId = 42 },
                "/OData.svc/content(42)?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Path()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) { Path = "/Root/Content/MyFolder" },
                "/OData.svc/Root/Content('MyFolder')?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_CountOnly()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) { ContentId = 42, CountOnly = true },
                "/OData.svc/content(42)/$count?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_ActionName()
        {
            await ODataRequestForLoadContentTest(repository =>
                new ODataRequest(repository.Server) {ContentId = 42, ActionName = "Action1"},
                "/OData.svc/content(42)/Action1?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_PropertyName()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) { ContentId = 42, PropertyName = "Property1" },
                "/OData.svc/content(42)/Property1?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_MetadataFull()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) { ContentId = 42, Metadata = MetadataFormat.Full },
                "/OData.svc/content(42)");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_MetadataMinimal()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) { ContentId = 42, Metadata = MetadataFormat.Minimal },
                "/OData.svc/content(42)?metadata=minimal");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_MetadataNo()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) { ContentId = 42, Metadata = MetadataFormat.None },
                "/OData.svc/content(42)?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_TopSkip()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) { ContentId = 42, Top = 10, Skip = 11 },
                "/OData.svc/content(42)?metadata=no&$top=10&$skip=11");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_ExpandSelect()
        {
            await ODataRequestForLoadContentTest(repository =>
                new ODataRequest(repository.Server)
                {
                    ContentId = 42,
                    Expand = new[] {"Manager", "CreatedBy/Manager"},
                    Select = new[] {"Id", "Name", "Manager/Name", "CreatedBy/Manager/Name"}
                },
                "/OData.svc/content(42)?metadata=no&$expand=Manager,CreatedBy/Manager&$select=Id,Name,Manager/Name,CreatedBy/Manager/Name");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Filter()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, ChildrenFilter = "isof('Folder')"},
                "/OData.svc/content(42)?metadata=no&$filter=isof('Folder')");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_OrderBy()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, OrderBy = new []{"Name", "Index desc"}},
                "/OData.svc/content(42)?metadata=no&$orderby=Name,Index%20desc");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_InlineCountDefault()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, InlineCount = InlineCountOptions.Default},
                "/OData.svc/content(42)?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_InlineCountNone()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, InlineCount = InlineCountOptions.None},
                "/OData.svc/content(42)?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_InlineCountAllPages()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, InlineCount = InlineCountOptions.AllPages},
                "/OData.svc/content(42)?metadata=no&$inlinecount=allpages");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_AutoFiltersDefault()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, AutoFilters = FilterStatus.Default},
                "/OData.svc/content(42)?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_AutoFiltersEnabled()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, AutoFilters = FilterStatus.Enabled},
                "/OData.svc/content(42)?metadata=no&enableautofilters=true");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_AutoFiltersDisabled()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, AutoFilters = FilterStatus.Disabled},
                "/OData.svc/content(42)?metadata=no&enableautofilters=false");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_LifespanFilterDefault()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, LifespanFilter = FilterStatus.Default},
                "/OData.svc/content(42)?metadata=no");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_LifespanFilterEnabled()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, LifespanFilter = FilterStatus.Enabled},
                "/OData.svc/content(42)?metadata=no&enablelifespanfilter=true");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_LifespanFilterDisabled()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, LifespanFilter = FilterStatus.Disabled},
                "/OData.svc/content(42)?metadata=no&enablelifespanfilter=false");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Version()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, Version = "V1.0.A"},
                "/OData.svc/content(42)?metadata=no&version=V1.0.A");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Scenario()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, Scenario = "scenario1"},
                "/OData.svc/content(42)?metadata=no&scenario=scenario1");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_ContentQuery()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, ContentQuery = "Index:>100 .SORT:Name"},
                "/OData.svc/content(42)?metadata=no&query=Index%3A%3E100%20.SORT%3AName");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Permissions()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, Permissions = new[] {"Save", "Custom01"}},
                "/OData.svc/content(42)?metadata=no&permissions=Save,Custom01");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_User()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server) {ContentId = 42, User = "user1"},
                "/OData.svc/content(42)?metadata=no&user=user1");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Parameters()
        {
            await ODataRequestForLoadContentTest(repository =>
                    new ODataRequest(repository.Server)
                    {
                        ContentId = 42,
                        Parameters = { {"param1", "value1"}, { "param2", "value2" } }
                    },
                "/OData.svc/content(42)?metadata=no&param1=value1&param2=value2");
        }

        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Error_MissingIdOrPath()
        {
            try
            {
                await ODataRequestForLoadContentTest(repository =>
                        new ODataRequest(repository.Server),
                    "_________");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual("Invalid request properties: either content id or path must be provided.", e.Message);
            }
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_Error_ActionAndProperty()
        {
            try
            {
                await ODataRequestForLoadContentTest(repository =>
                        new ODataRequest(repository.Server)
                        {
                            ContentId = 42,
                            ActionName = "action1",
                            PropertyName = "property1"
                        },
                    "_________");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual("Invalid request properties: both action name and property name are provided.", e.Message);
            }
        }

        private async Task ODataRequestForLoadContentTest(Func<IRepository, ODataRequest> getOdataRequest, string expectedUrl)
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Name"": ""Content"" }}"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton<IRestCaller>(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            getOdataRequest(repository);
            var content = await repository.LoadContentAsync(getOdataRequest(repository), CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual(expectedUrl, requestedUri.PathAndQuery);
        }

        /* ====================================================================== LOAD COLLECTION */

        [TestMethod]
        public async Task Repository_LoadCollection()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>())
                .Returns(Task.FromResult(@"{""d"": {""__count"": 2, ""results"": [
                    {""Id"": 3, ""Name"": ""IMS"", ""Type"": ""Domains""},
                    {""Id"": 1000, ""Name"": ""System"", ""Type"": ""SystemFolder""}]}}"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton<IRestCaller>(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new ODataRequest(repository.Server) {Path = "/Root", Select = new[] {"Id", "Name", "Type"}};

            // ACT
            var collection = await repository.LoadCollectionAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root?metadata=no&$select=Id,Name,Type", requestedUri.PathAndQuery);

            var contents = collection.ToArray();
            Assert.AreEqual(2, contents.Length);

            var content = contents[0];
            Assert.AreEqual(3, content.Id);
            Assert.AreEqual("IMS", content.Name);
            Assert.AreEqual("Domains", content["Type"].ToString());
            content = contents[1];
            Assert.AreEqual(1000, content.Id);
            Assert.AreEqual("System", content.Name);
            Assert.AreEqual("SystemFolder", content["Type"].ToString());
        }

        /* ====================================================================== CONTENT EXISTENCE */

        [TestMethod]
        public async Task Repository_IsContentExists_Yes()
        {
            // ALIGN
            var infrastructure = await GetDefaultRepositoryAndRestCallerMock();
            var restCaller = infrastructure.RestCaller;
            var repository = infrastructure.Repository;

            restCaller.GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Id"": 42 }}"));

            // ACT
            var isExist = await repository.IsContentExistAsync("/Root/Content/MyContent", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT REQUEST
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/Content('MyContent')?metadata=no&$select=Id", requestedUri.PathAndQuery);
            // ASSERT RESPONSE
            Assert.IsTrue(isExist);
        }
        [TestMethod]
        public async Task Repository_IsContentExists_No()
        {
            // ALIGN
            var infrastructure = await GetDefaultRepositoryAndRestCallerMock();
            var restCaller = infrastructure.RestCaller;
            var repository = infrastructure.Repository;

            restCaller.GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>())
                .Returns(Task.FromResult(string.Empty));

            // ACT
            var isExist = await repository.IsContentExistAsync("/Root/Content/MyContent", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT REQUEST
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/Content('MyContent')?metadata=no&$select=Id", requestedUri.PathAndQuery);
            // ASSERT RESPONSE
            Assert.IsFalse(isExist);
        }

        /* ====================================================================== TOOLS */

        private static IRepositoryCollection GetRepositoryCollection(Action<IServiceCollection> addServices = null)
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<RepositoryTests>()
                .Build();

            services
                .AddSingleton<IConfiguration>(config)
                .AddSenseNetClient()
                //.AddSingleton<ITokenProvider, TestTokenProvider>()
                //.AddSingleton<ITokenStore, TokenStore>()
                .ConfigureSenseNetRepository("local", repositoryOptions =>
                {
                    // set test url and authentication in user secret
                    config.GetSection("sensenet:repository").Bind(repositoryOptions);
                });

            addServices?.Invoke(services);

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IRepositoryCollection>();
        }
    }
}
