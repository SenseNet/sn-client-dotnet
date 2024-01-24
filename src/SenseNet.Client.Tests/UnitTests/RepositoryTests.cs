using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute;
using SenseNet.Client;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Accessors;

namespace DifferentNamespace
{
    public class MyContent : Content
    {
        public MyContent(IRestCaller restCaller, ILogger<MyContent> logger) : base(restCaller, logger) { }
    }
}

namespace SenseNet.Client.Tests.UnitTests
{
    [TestClass]
    public class RepositoryTests : TestBase
    {
        #region Test classes
        public class File : Content
        {
            public File(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
        }
        #endregion

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
        public async Task Repository_Default_WithToken()
        {
            // ALIGN
            var repositoryCollection = GetRepositoryCollection(services =>
            {
                services.ConfigureSenseNetRepository(opt => { opt.Url = ExampleUrl; });
            });

            // ACT
            var repository1 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs(), CancellationToken.None).ConfigureAwait(false);
            var repository2 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs
                {
                    AccessToken = "abc"
                }, CancellationToken.None).ConfigureAwait(false);

            // get the same repository instance again
            var repository3 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs
                {
                    AccessToken = "abc"
                }, CancellationToken.None).ConfigureAwait(false);

            // get a new repository instance with a different token
            var repository4 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs
                {
                    AccessToken = "def"
                }, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual(ExampleUrl, repository1.Server.Url);
            Assert.AreEqual(ExampleUrl, repository2.Server.Url);
            Assert.AreEqual(null, repository1.Server.Authentication.AccessToken);
            Assert.AreEqual("abc", repository2.Server.Authentication.AccessToken);
            Assert.AreEqual("def", repository4.Server.Authentication.AccessToken);

            // the same repository instance should be returned for the same token
            Assert.AreSame(repository2, repository3);
            Assert.AreNotSame(repository3, repository4);
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

        [TestMethod]
        public async Task Repository_Named_WithToken()
        {
            // ALIGN
            var repositoryCollection = GetRepositoryCollection(services =>
            {
                services.ConfigureSenseNetRepository("repo1", opt => { opt.Url = ExampleUrl; });
                services.ConfigureSenseNetRepository("repo2", opt => { opt.Url = "https://url2"; });
            });

            // ACT
            var repository1 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs { Name = "repo1" }, CancellationToken.None)
                .ConfigureAwait(false);
            var repository2 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs { Name = "repo1", AccessToken = "abc" }, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual(ExampleUrl, repository1.Server.Url);
            Assert.AreEqual(ExampleUrl, repository2.Server.Url);
            Assert.AreEqual(null, repository1.Server.Authentication.AccessToken);
            Assert.AreEqual("abc", repository2.Server.Authentication.AccessToken);
            Assert.AreNotSame(repository1, repository2);
            
            // ACT
            var repository3 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs
                    {
                        Name = "repo1",
                        AccessToken = "abc"
                    },
                    CancellationToken.None).ConfigureAwait(false);

            // same repository, different token: different instances
            var repository4 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs
                {
                    Name = "repo1",
                    AccessToken = "def"
                }, CancellationToken.None).ConfigureAwait(false);

            // same repository, same token: same instance
            var repository5 = await repositoryCollection
                .GetRepositoryAsync(new RepositoryArgs
                {
                    Name = "repo1",
                    AccessToken = "def"
                }, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("abc", repository3.Server.Authentication.AccessToken);
            Assert.AreEqual("def", repository4.Server.Authentication.AccessToken);
            Assert.AreNotSame(repository3, repository4);
            Assert.AreSame(repository4, repository5);
        }

        /* ====================================================================== CONTENT CREATION */

        [TestMethod]
        public async Task Repository_CreateExistingContent_ById()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);

            // ACT
            var content = repository.CreateExistingContent(42);

            // ASSERT
            Assert.AreEqual(null, content.Path);
            Assert.AreEqual(42, content.Id);
            // technical structure
            Assert.AreSame(repository, content.Repository);
            Assert.AreSame(repository.Server, content.Server);
            Assert.IsNull(content.ParentPath);
            Assert.IsNull(content.Name);
            Assert.AreEqual(true, ((dynamic)content).Existing);
        }
        [TestMethod]
        public async Task Repository_CreateExistingContent_ByPath()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);

            // ACT
            var content = repository.CreateExistingContent("/Root/MyContent");

            // ASSERT
            Assert.AreEqual("/Root/MyContent", content.Path);
            Assert.AreEqual(0, content.Id);
            // technical structure
            Assert.AreSame(repository, content.Repository);
            Assert.AreSame(repository.Server, content.Server);
            Assert.IsNull(content.ParentPath);
            Assert.IsNull(content.Name);
            Assert.AreEqual(true, ((dynamic)content).Existing);
        }
        [TestMethod]
        public async Task Repository_CreateExistingContent_T_ById()
        {
            var repository = await GetRepositoryCollection(
                    services => services.RegisterGlobalContentType<MyContent>())
                .GetRepositoryAsync(FakeServer, CancellationToken.None);

            // ACT
            var content = repository.CreateExistingContent<MyContent>(42);

            // ASSERT
            Assert.AreEqual(null, content.Path);
            Assert.AreEqual(42, content.Id);
            // technical structure
            Assert.IsInstanceOfType(content, typeof(MyContent));
            Assert.AreSame(repository, content.Repository);
            Assert.AreSame(repository.Server, content.Server);
            Assert.IsNull(content.ParentPath);
            Assert.IsNull(content.Name);
            Assert.AreEqual(true, ((dynamic)content).Existing);
        }
        [TestMethod]
        public async Task Repository_CreateExistingContent_T_ByPath()
        {
            var repository = await GetRepositoryCollection(
                    services => services.RegisterGlobalContentType<MyContent>())
                .GetRepositoryAsync(FakeServer, CancellationToken.None);

            // ACT
            var content = repository.CreateExistingContent<MyContent>("/Root/MyContent");

            // ASSERT
            Assert.AreEqual("/Root/MyContent", content.Path);
            Assert.AreEqual(0, content.Id);
            // technical structure
            Assert.IsInstanceOfType(content, typeof(MyContent));
            Assert.AreSame(repository, content.Repository);
            Assert.AreSame(repository.Server, content.Server);
            Assert.IsNull(content.ParentPath);
            Assert.IsNull(content.Name);
            Assert.AreEqual(true, ((dynamic)content).Existing);
        }


        [TestMethod]
        public async Task Repository_CreateContent()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);

            dynamic content = repository.CreateContent("/Root/Content/MyTasks", "Task", "Task1");

            Assert.AreEqual("/Root/Content/MyTasks", content.ParentPath);
            Assert.AreEqual("Task", content.__ContentType);
            Assert.AreEqual("Task1", content.Name);
        }
        [TestMethod]
        public async Task Repository_CreateContent_MissingName()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);

            dynamic content = repository.CreateContent("/Root/Content/MyTasks", "Task", null);

            Assert.AreEqual("/Root/Content/MyTasks", content.ParentPath);
            Assert.AreEqual("Task", content.__ContentType);
            Assert.IsNull(content.Name);
        }
        [TestMethod]
        public async Task Repository_CreateContent_EmptyName()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);

            dynamic content = repository.CreateContent("/Root/Content/MyTasks", "Task", "");

            Assert.AreEqual("/Root/Content/MyTasks", content.ParentPath);
            Assert.AreEqual("Task", content.__ContentType);
            Assert.IsNull(content.Name);
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
        public async Task Repository_CreateContentByTemplate()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);

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

        /* ====================================================================== LOAD CONTENT */

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
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
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
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/content(42)?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Content", content.Name);
        }

        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_IdVersion()
        {
            await ODataRequestForLoadContentTest(
                _ => new LoadContentRequest { ContentId = 42, Version = "V1.0.A" },
                "/OData.svc/content(42)?metadata=no&version=V1.0.A");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_PathVersion()
        {
            await ODataRequestForLoadContentTest(
                _ => new LoadContentRequest { Path = "/Root/Content/MyFolder", Version = "V1.0.A" },
                "/OData.svc/Root/Content('MyFolder')?metadata=no&version=V1.0.A");
        }

        private async Task<(IRepository Repository, IRestCaller RestCaller)> GetDefaultRepositoryAndRestCallerMock()
        {
            var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            return (repository, restCaller);
        }
        private async Task ODataRequestForLoadContentTest(Func<IRepository, LoadContentRequest> getLoadContentRequest, string expectedUrl)
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var _ = await repository.LoadContentAsync(getLoadContentRequest(repository), CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual(expectedUrl, requestedUri.PathAndQuery);
        }

        /* ====================================================================== LOAD COLLECTION */

        [TestMethod]
        public async Task Repository_LoadCollection()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 2, ""results"": [
                    {""Id"": 3, ""Name"": ""IMS"", ""Type"": ""Domains""},
                    {""Id"": 1000, ""Name"": ""System"", ""Type"": ""SystemFolder""}]}}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            var request = new LoadCollectionRequest { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var collection = await repository.LoadCollectionAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
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
        [TestMethod]
        public async Task Repository_LoadCollection_WithQuery()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 2, ""results"": [
                    {""Id"": 3, ""Name"": ""IMS"", ""Type"": ""Domains""},
                    {""Id"": 1000, ""Name"": ""System"", ""Type"": ""SystemFolder""}]}}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", ContentQuery = "TypeIs:File", Select = new[] { "Id", "Name", "Type" } };
            var collection = await repository.LoadCollectionAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            // decoded query expectation: query=+InFolder:'/Root/MyContent' +(TypeIs:File)
            Assert.AreEqual("/OData.svc/Root/MyContent?metadata=no&$select=Id,Name,Type&" +
                            "query=%2BInFolder%3A%27%2FRoot%2FMyContent%27%20%2B%28TypeIs%3AFile%29",
                requestedUri.PathAndQuery);
        }
        [TestMethod]
        public async Task Repository_LoadCollection_TotalCount()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 7, ""results"": [
                    {""Id"": 3, ""Name"": ""IMS"", ""Type"": ""Domains""},
                    {""Id"": 1000, ""Name"": ""System"", ""Type"": ""SystemFolder""}]}}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            var request = new LoadCollectionRequest { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var collection = await repository.LoadCollectionAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root?metadata=no&$select=Id,Name,Type", requestedUri.PathAndQuery);

            Assert.AreEqual(2, collection.Count);
            Assert.AreEqual(7, collection.TotalCount);

            var contents = collection.ToArray();
            Assert.AreEqual(collection.Count, contents.Length);
        }

        [TestMethod]
        public async Task Repository_GetContentCount()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"42");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            var request = new LoadCollectionRequest { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var count = await repository.GetContentCountAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/$count?metadata=no&$select=Id,Name,Type", requestedUri.PathAndQuery);

            Assert.AreEqual(42, count);
        }

        [TestMethod]
        public async Task Repository_InFolderRestriction_1()
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);
            var repositoryAcc = new ObjectAccessor(repository);

            void Test(string query, string path, string expected)
            {
                var result = (string) repositoryAcc.Invoke("AddInFolderRestriction",
                    new[] {typeof(string), typeof(string)},
                    new[] {query, path});
                Assert.AreEqual(expected, result);
            }

            Test("Type:Folder", "/Root", "+InFolder:'/Root' +(Type:Folder)");
            Test("Type:Folder .SORT:Name", "/Root", "+InFolder:'/Root' +(Type:Folder ) .SORT:Name");
            Test(".SORT:Name Type:Folder", "/Root", "+InFolder:'/Root' +( Type:Folder) .SORT:Name");

            Test(".TOP:10 +Type:Folder +Name:a*", "/Root",
                "+InFolder:'/Root' +( +Type:Folder +Name:a*) .TOP:10");

            Test(".TOP:10 +Type:Folder .SKIP:5 +Name:a* .SORT:Index", "/Root",
                "+InFolder:'/Root' +( +Type:Folder  +Name:a* ) .TOP:10  .SKIP:5  .SORT:Index");
        }

        /* ====================================================================== QUERY CONTENT */

        [TestMethod]
        public async Task Repository_QueryForAdmin()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 4, ""results"": [
      {""Name"": ""Admin""},
      {""Name"": ""PublicAdmin""},
      {""Name"": ""Somebody""},
      {""Name"": ""Visitor""}]}}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new QueryContentRequest {ContentQuery = "TypeIs:User .SORT:Name", Select = new[] {"Name"}};
            var collection = await repository.QueryForAdminAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root?metadata=no&$select=Name&enableautofilters=false&enablelifespanfilter=false&query=TypeIs%3AUser%20.SORT%3AName", requestedUri.PathAndQuery);

            var actual = string.Join(", ", collection.Select(c => c.Name));
            Assert.AreEqual("Admin, PublicAdmin, Somebody, Visitor", actual);
        }
        [TestMethod]
        public async Task Repository_Query()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 4, ""results"": [
      {""Name"": ""Admin""},
      {""Name"": ""PublicAdmin""},
      {""Name"": ""Somebody""},
      {""Name"": ""Visitor""}]}}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new QueryContentRequest { ContentQuery = "TypeIs:User .SORT:Name", Select = new[] { "Name" } };
            var collection = await repository.QueryAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root?metadata=no&$select=Name&query=TypeIs%3AUser%20.SORT%3AName", requestedUri.PathAndQuery);

            var actual = string.Join(", ", collection.Select(c => c.Name));
            Assert.AreEqual("Admin, PublicAdmin, Somebody, Visitor", actual);
        }

        [TestMethod]
        public async Task Repository_QueryCountForAdmin()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"42");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new QueryContentRequest { ContentQuery = "TypeIs:User .SORT:Name", Select = new[] { "Name" } };
            var count = await repository.QueryCountForAdminAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/$count?metadata=no&$select=Name&enableautofilters=false&enablelifespanfilter=false&query=TypeIs%3AUser%20.SORT%3AName", requestedUri.PathAndQuery);

            Assert.AreEqual(42, count);
        }
        [TestMethod]
        public async Task Repository_QueryCount()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"42");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new QueryContentRequest { ContentQuery = "TypeIs:User .SORT:Name", Select = new[] { "Name" } };
            var count = await repository.QueryCountAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/$count?metadata=no&$select=Name&query=TypeIs%3AUser%20.SORT%3AName", requestedUri.PathAndQuery);

            Assert.AreEqual(42, count);
        }

        /* ====================================================================== CONTENT EXISTENCE */

        [TestMethod]
        public async Task Repository_IsContentExists_Yes()
        {
            // ALIGN
            var infrastructure = await GetDefaultRepositoryAndRestCallerMock();
            var restCaller = infrastructure.RestCaller;
            var repository = infrastructure.Repository;

            restCaller
//.GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Id"": 42 }}"));

            // ACT
            var isExist = await repository.IsContentExistsAsync("/Root/Content/MyContent", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT REQUEST
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
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

            restCaller
//.GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(string.Empty));

            // ACT
            var isExist = await repository.IsContentExistsAsync("/Root/Content/MyContent", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT REQUEST
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/Content('MyContent')?metadata=no&$select=Id", requestedUri.PathAndQuery);
            // ASSERT RESPONSE
            Assert.IsFalse(isExist);
        }

        /* ====================================================================== DELETE CONTENT */

        [TestMethod]
        public async Task Repository_Delete_Permanent_ByPath()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor("");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            await repository.DeleteContentAsync("/Root/Content/MyContent", true, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[1] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[2] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":true,\"paths\":[\"/Root/Content/MyContent\"]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ByPath()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor("");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            await repository.DeleteContentAsync("/Root/Content/MyContent", false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[1] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[2] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[\"/Root/Content/MyContent\"]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ByPaths()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor("");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var paths = new[] { "/Root/F1", "/Root/F2", "/Root/F3" };
            await repository.DeleteContentAsync(paths, false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[1] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[2] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[\"/Root/F1\",\"/Root/F2\",\"/Root/F3\"]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ById()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor("");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            await repository.DeleteContentAsync(1234, false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[1] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[2] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[1234]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ByIds()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor("");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var ids = new[] { 1234, 1235, 1236, 1237 };
            await repository.DeleteContentAsync(ids, false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[1] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[2] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[1234,1235,1236,1237]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ByIdsOrPaths()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor("");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var idsOrPaths = new object[] { "/Root/F1", 1234, "/Root/F2", 1235, 1236 };
            await repository.DeleteContentAsync(idsOrPaths, false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[1] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[2] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[\"/Root/F1\",1234,\"/Root/F2\",1235,1236]}]", jsonBody);
        }

        [TestMethod]
        public async Task Repository_Delete_Error_MissingIdsOrPaths()
        {
            await TestParameterError<ArgumentNullException>(
                callback: async repository => await repository.DeleteContentAsync(
                    (object[])null, false, CancellationToken.None).ConfigureAwait(false),
                expectedMessage: "Value cannot be null. (Parameter 'idsOrPaths')").ConfigureAwait(false);
        }

        /* =================================================================== CONTENT TYPE REGISTRATION */

        public class MyContent : Content
        {
            public string HelloMessage => $"Hello {this.Name}!";
            public MyContent(IRestCaller restCaller, ILogger<MyContent> logger) : base(restCaller, logger) { }
        }
        public class MyContent2 : Content { public MyContent2(IRestCaller restCaller, ILogger<MyContent> logger) : base(restCaller, logger) { } }
        public class MyContent3 : Content { public MyContent3(IRestCaller restCaller, ILogger<MyContent> logger) : base(restCaller, logger) { } }
        public class MyContent4 : Content { public MyContent4(IRestCaller restCaller, ILogger<MyContent> logger) : base(restCaller, logger) { } }

        [TestMethod]
        public async Task Repository_T_RegisterGlobalContentType_TypeParam()
        {
            // ACTION
            var repositories = GetRepositoryCollection(services =>
            {
                services.RegisterGlobalContentType<MyContent>();
            });

            // ASSERT
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            repository.GlobalContentTypes.ContentTypes.TryGetValue("MyContent", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_T_RegisterGlobalContentType_TypeParamAndDifferentName()
        {
            // ACTION
            var repositories = GetRepositoryCollection(services =>
            {
                services.RegisterGlobalContentType<MyContent>("MyType");
            });

            // ASSERT
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            repository.GlobalContentTypes.ContentTypes.TryGetValue("MyType", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_T_RegisterGlobalContentType_Type()
        {
            // ACTION
            var repositories = GetRepositoryCollection(services =>
            {
                services.RegisterGlobalContentType(typeof(MyContent));
            });

            // ASSERT
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            repository.GlobalContentTypes.ContentTypes.TryGetValue("MyContent", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }
        [TestMethod]
        public async Task Repository_T_RegisterGlobalContentType_TypeAndDifferentName()
        {
            // ACTION
            var repositories = GetRepositoryCollection(services =>
            {
                services.RegisterGlobalContentType(typeof(MyContent), "MyType");
            });

            // ASSERT
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            repository.GlobalContentTypes.ContentTypes.TryGetValue("MyType", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(MyContent), contentType);
        }

        [TestMethod]
        public async Task Repository_T_RegisterGlobalContentType_BuiltIn_User()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"
{ 
    ""d"": { 
        ""Id"": 1,
        ""Path"": ""/Root/IMS/BuiltIn/Admin"",
        ""Name"": ""Admin"", 
        ""Type"": ""User"",
        ""LoginName"": ""admin"",
        ""Email"": ""admin@example.com"",
        ""BirthDate"": ""2000-02-03T00:00:00Z"",
        ""Avatar"": {
            ""Url"": ""/Root/Content/images/avatar.png""
        },
        ""ImageRef"": {
	        ""Id"": 123,
            ""Path"": ""/Root/Content/images/avatar.png"",
	        ""Type"": ""Image"",
	        ""Name"": ""avatar.png"",
	        ""DateTaken"": ""2010-10-01T00:00:00Z"",
	        ""Width"": 456,
	        ""Height"": 789
        }
    }
}");

            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });

            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var user = await repository.LoadContentAsync<User>("/testcontentpath", CancellationToken.None);

            // ASSERT
            Assert.IsNotNull(user);
            Assert.AreEqual(typeof(User), user.GetType());
            Assert.AreEqual("Admin", user.Name);
            Assert.AreEqual("admin", user.LoginName);
            Assert.AreEqual("admin@example.com", user.Email);
            Assert.AreEqual(new DateTime(2000, 02, 03, 0, 0, 0, DateTimeKind.Utc), user.BirthDate);

            Assert.AreEqual(typeof(Image), user.ImageRef.GetType());
            Assert.AreEqual(123, user.ImageRef.Id);
            Assert.AreEqual(456, user.ImageRef.Width);
            Assert.AreEqual(789, user.ImageRef.Height);
            Assert.AreEqual("/Root/Content/images/avatar.png", user.Avatar.Url);
        }

        [TestMethod]
        public async Task Repository_T_RegisterContentTypes()
        {
            // ALIGN
            var repositories = GetRepositoryCollection(services =>
            {
                services.ConfigureSenseNetRepository(
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes
                            .Add<MyContent>()
                            .Add<MyContent2>("MyContent_2")
                            .Add(typeof(MyContent3))
                            .Add(typeof(MyContent4), "MyContent_4")
                            ;
                    });
            });

            // ACTION
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            var repositoryAcc = new ObjectAccessor(repository);
            var services = (IServiceProvider)repositoryAcc.GetField("_services");
            Assert.AreEqual(ExampleUrl, repository.Server.Url);
            //Assert.AreEqual(3, repository.GlobalContentTypes.ContentTypes.Count);
            var contentTypeRegistrations = repository.Server.RegisteredContentTypes.ContentTypes
                .OrderBy(x => x.Value.Name)
                .ToArray();

            Assert.AreEqual(4, contentTypeRegistrations.Length);

            Assert.IsNotNull(services.GetService<MyContent>());
            Assert.AreEqual("MyContent", contentTypeRegistrations[0].Key);
            Assert.AreEqual(typeof(MyContent), contentTypeRegistrations[0].Value);

            Assert.IsNotNull(services.GetService<MyContent2>());
            Assert.AreEqual("MyContent_2", contentTypeRegistrations[1].Key);
            Assert.AreEqual(typeof(MyContent2), contentTypeRegistrations[1].Value);

            Assert.IsNotNull(services.GetService<MyContent3>());
            Assert.AreEqual("MyContent3", contentTypeRegistrations[2].Key);
            Assert.AreEqual(typeof(MyContent3), contentTypeRegistrations[2].Value);

            Assert.IsNotNull(services.GetService<MyContent4>());
            Assert.AreEqual("MyContent_4", contentTypeRegistrations[3].Key);
            Assert.AreEqual(typeof(MyContent4), contentTypeRegistrations[3].Value);
        }
        [TestMethod]
        public async Task Repository_T_RegisterContentTypes_DifferentTypeSameName()
        {
            // ALIGN
            var repo1Name = "Repo1";
            var repo2Name = "Repo2";
            var exampleUrl2 = "https://example2.com";
            var repositories = GetRepositoryCollection(services =>
            {
                services.ConfigureSenseNetRepository(repo1Name,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<MyContent>();
                    });
                services.ConfigureSenseNetRepository(repo2Name,
                    configure: options => { options.Url = exampleUrl2; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<DifferentNamespace.MyContent>();
                    });
            });

            // ACT
            var repository1 = await repositories.GetRepositoryAsync(repo1Name, CancellationToken.None).ConfigureAwait(false);
            var repository2 = await repositories.GetRepositoryAsync(repo2Name, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            // check repo1
            Assert.AreEqual(ExampleUrl, repository1.Server.Url);
            //Assert.AreEqual(3, repository1.GlobalContentTypes.ContentTypes.Count);
            var contentTypeRegistrations1 = repository1.Server.RegisteredContentTypes.ContentTypes
                .OrderBy(x => x.Value.Name)
                .ToArray();
            Assert.AreEqual(1, contentTypeRegistrations1.Length);
            Assert.AreEqual("MyContent", contentTypeRegistrations1[0].Key);
            Assert.AreEqual(typeof(MyContent), contentTypeRegistrations1[0].Value);

            // check repo2
            Assert.AreEqual(exampleUrl2, repository2.Server.Url);
            //Assert.AreEqual(3, repository2.GlobalContentTypes.ContentTypes.Count);
            var contentTypeRegistrations2 = repository2.Server.RegisteredContentTypes.ContentTypes
                .OrderBy(x => x.Value.Name)
                .ToArray();
            Assert.AreEqual(1, contentTypeRegistrations2.Length);
            Assert.AreEqual("MyContent", contentTypeRegistrations2[0].Key);
            Assert.AreEqual(typeof(DifferentNamespace.MyContent), contentTypeRegistrations2[0].Value);

            // check services
            var repositoryAcc = new ObjectAccessor(repository1);
            var services = (IServiceProvider)repositoryAcc.GetField("_services");
            Assert.IsNotNull(services.GetService<MyContent>());
            Assert.IsNotNull(services.GetService<DifferentNamespace.MyContent>());
        }

        [TestMethod]
        public async Task Repository_T_RegisterGlobalContentType_OverrideExisting()
        {
            // ACTION
            var repositories = GetRepositoryCollection(services =>
            {
                services.RegisterGlobalContentType(typeof(DownloadTests.File));

                // override the registration above
                services.RegisterGlobalContentType(typeof(File));
            });

            // ASSERT
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            repository.GlobalContentTypes.ContentTypes.TryGetValue("File", out var contentType);
            Assert.IsNotNull(contentType);
            Assert.AreEqual(typeof(File), contentType);
        }

        /* =================================================================== CREATE REGISTERED CONTENT */

        [TestMethod]
        public async Task Repository_T_CreateContent_Global()
        {
            var repositories = GetRepositoryCollection(services =>
            {
                services.RegisterGlobalContentType<MyContent>();
            });
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = repository.CreateContent<MyContent>("/Root/Content", null, "MyContent-1");

            // ASSERT
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
        }
        [TestMethod]
        public async Task Repository_T_CreateContent_ByName_Global()
        {
            var repositories = GetRepositoryCollection(services =>
            {
                services.RegisterGlobalContentType<MyContent>();
            });
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = repository.CreateContent("/Root/Content", "MyContent", null);

            // ASSERT
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
        }
        [TestMethod]
        public async Task Repository_T_CreateContent()
        {
            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                //services.RegisterGlobalContentType<MyContent>();
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes => { contentTypes.Add<MyContent>(); });
            });
            var repository = await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = repository.CreateContent<MyContent>("/Root/Content", null, "MyContent-1");

            // ASSERT
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent));
        }
        [TestMethod]
        public async Task Repository_T_CreateContent_ByName()
        {
            var repoName = "MyRepo";
            var contentTypeName = "MyContent_2";
            var repositories = GetRepositoryCollection(services =>
            {
                //services.RegisterGlobalContentType<MyContent>();
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes => { contentTypes.Add<MyContent2>(contentTypeName); });
            });
            var repository = await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = repository.CreateContent("/Root/Content", contentTypeName, null);

            // ASSERT
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent2));
        }
        [TestMethod]
        public async Task Repository_T_CreateContent_DifferentTypeSameName()
        {
            // ALIGN
            var repo1Name = "Repo1";
            var repo2Name = "Repo2";
            var exampleUrl2 = "https://example2.com";
            var repositories = GetRepositoryCollection(services =>
            {
                services.ConfigureSenseNetRepository(repo1Name,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<MyContent>();
                    });
                services.ConfigureSenseNetRepository(repo2Name,
                    configure: options => { options.Url = exampleUrl2; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<DifferentNamespace.MyContent>();
                    });
            });
            var repository1 = await repositories.GetRepositoryAsync(repo1Name, CancellationToken.None).ConfigureAwait(false);
            var repository2 = await repositories.GetRepositoryAsync(repo2Name, CancellationToken.None).ConfigureAwait(false);

            // ACT
            var content1 = repository1.CreateContent("/Root/Content", "MyContent", null);
            var content2 = repository2.CreateContent("/Root/Content", "MyContent", null);

            // ASSERT
            Assert.IsNotNull(content1);
            Assert.IsInstanceOfType(content1, typeof(MyContent));
            Assert.IsNotNull(content2);
            Assert.IsInstanceOfType(content2, typeof(DifferentNamespace.MyContent));
        }
        [TestMethod]
        public async Task Repository_T_CreateContent_Unknown_ByType()
        {
            var repositories = GetRepositoryCollection();
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);
            Exception? exception = null;

            // ACTION
            try
            {
                var _ = repository.CreateContent<MyContent>("/Root/Content", null, "MyContent-1");
                // ASSERT
                Assert.Fail($"The expected {nameof(ApplicationException)} was not thrown.");
            }
            catch (ApplicationException e)
            {
                exception = e;
            }
            Assert.IsTrue(exception.Message.Contains(nameof(MyContent)));
            Assert.IsNotNull(exception.InnerException, "The exception.InnerException is null");
        }
        [TestMethod]
        public async Task Repository_T_CreateContent_Unknown_ByName()
        {
            var repositories = GetRepositoryCollection();
            var repository = await repositories.GetRepositoryAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = repository.CreateContent("/Root/Content", nameof(MyContent), null);

            // ASSERT
            Assert.AreEqual(typeof(Content), content.GetType());
        }


        [TestMethod]
        public async Task Repository_T_CreateContentByTemplate()
        {
            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                //services.RegisterGlobalContentType<MyContent>();
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes => { contentTypes.Add<MyContent>(); });
            });
            var repository = await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            dynamic content = repository.CreateContentByTemplate<MyContent>(
                "/Root/Content", null, "Content1", "Template1");

            Assert.AreEqual("/Root/Content", content.ParentPath);
            Assert.AreEqual("MyContent", content.__ContentType);
            Assert.AreEqual("Content1", content.Name);
            Assert.AreEqual("Template1", content.__ContentTemplate);
        }
        [TestMethod]
        public async Task Repository_T_CreateContentByTemplate_Error_MissingContentTemplate()
        {
            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                //services.RegisterGlobalContentType<MyContent>();
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes => { contentTypes.Add<MyContent>(); });
            });
            var repository = await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            try
            {
                repository.CreateContentByTemplate<MyContent>(
                    "/Root/Content", null, "Content1", null);
                Assert.Fail($"The expected {nameof(ArgumentNullException)} was not thrown.");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null. (Parameter 'contentTemplate')", e.Message);
            }
        }
        [TestMethod]
        public async Task Repository_T_CreateContentByTemplate_Error_EmptyContentTemplate()
        {
            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                //services.RegisterGlobalContentType<MyContent>();
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes => { contentTypes.Add<MyContent>(); });
            });
            var repository = await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            try
            {
                repository.CreateContentByTemplate<MyContent>(
                    "/Root/Content", null, "Content1", "");
                Assert.Fail($"The expected {nameof(ArgumentException)} was not thrown.");
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("Value cannot be empty. (Parameter 'contentTemplate')", e.Message);
            }
        }

        [TestMethod]
        public async Task Repository_T_CreateContent_Error_MissingParentPath()
        {
            await TestParameterError<ArgumentNullException>(
                repository => repository.CreateContent<MyContent>(null, null, null),
                "Value cannot be null. (Parameter 'parentPath')").ConfigureAwait(false);
        }
        [TestMethod]
        public async Task Repository_T_CreateContent_Error_EmptyParentPath()
        {
            await TestParameterError<ArgumentException>(
                repository => repository.CreateContent<MyContent>("", null, null),
                "Value cannot be empty. (Parameter 'parentPath')").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Repository_T_GetContentTypeNameByType_Template()
        {
            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                //services.RegisterGlobalContentType<MyContent>();
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<MyContent>();
                        contentTypes.Add<MyContent2>("MyContent_2");
                        contentTypes.Add<MyContent3>();
                    });
            });
            var repository = (Repository) await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var contentTypeNames = new[]
            {
                repository.GetContentTypeNameByType<MyContent>(),
                repository.GetContentTypeNameByType<MyContent2>(),
                repository.GetContentTypeNameByType<MyContent3>(),
            };

            // ASSERT
            var allNames = string.Join(", ", contentTypeNames);
            Assert.AreEqual("MyContent, MyContent_2, MyContent3", allNames);
        }
        [TestMethod]
        public async Task Repository_T_GetContentTypeNameByType_Type()
        {
            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                //services.RegisterGlobalContentType<MyContent>();
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<MyContent>();
                        contentTypes.Add<MyContent2>("MyContent_2");
                        contentTypes.Add<MyContent3>();
                    });
            });
            var repository = (Repository) await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var contentTypeNames = new[]
            {
                repository.GetContentTypeNameByType(null),
                repository.GetContentTypeNameByType(typeof(MyContent)),
                repository.GetContentTypeNameByType(typeof(MyContent2)),
                repository.GetContentTypeNameByType(typeof(MyContent3)),
            };

            // ASSERT
            var allNames = string.Join(", ", contentTypeNames);
            Assert.AreEqual(", MyContent, MyContent_2, MyContent3", allNames);
        }
        [TestMethod]
        public async Task Repository_T_GetContentTypeNameByType_Error_NotSpecifiedName()
        {
            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                //services.RegisterGlobalContentType<MyContent>();
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<MyContent2>();
                        contentTypes.Add<MyContent2>("MyContent_2");
                        contentTypes.Add<MyContent2>("MyContent_Two");
                    });
            });
            var repository = (Repository) await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            try
            {
                var _ = repository.GetContentTypeNameByType(typeof(MyContent2));
                // ASSERT
                Assert.Fail($"The expected {nameof(InvalidOperationException)} was not thrown.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Cannot resolve the content type name for the type MyContent2 " +
                                "because two or more names are registered: " +
                                "MyContent2, MyContent_2, MyContent_Two.", ex.Message);
            }
        }

        [TestMethod]
        public async Task Repository_T_CreateContentAndSave_Global()
        {
            var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""MyContent"",
    ""Path"": ""/Root/MyContent"",
    ""Type"": ""Folder"",
    ""Index"": 99
  }
}");

            var repositories = GetRepositoryCollection(services =>
            {
                services.RegisterGlobalContentType<MyContent>();
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var content = repository.CreateContent<MyContent> ("/Root/Content", null, "MyContent-1");
            await content.SaveAsync().ConfigureAwait(false);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.IsTrue(arguments[0].ToString().Contains("/Root('Content')")); // parentPath
            Assert.AreEqual(HttpMethod.Post, (HttpMethod)arguments[1]);
            var json = (string)arguments[2]!;
            json = json.Substring("models=[".Length).TrimEnd(']');
            dynamic data = JsonHelper.Deserialize(json);
            Assert.IsNotNull(data);
            Assert.AreEqual("MyContent-1", data.Name.ToString());
            Assert.AreEqual("MyContent", data.__ContentType.ToString());

        }

        [TestMethod]
        public async Task Repository_T_CreateContentAndSave_LocalAndDifferentName()
        {
            var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""MyContent"",
    ""Path"": ""/Root/MyContent"",
    ""Type"": ""Folder"",
    ""Index"": 99
  }
}");

            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<MyContent2>("MyContent_Two");
                    });
            });
            var repository = await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var content = repository.CreateContent<MyContent2>("/Root/Content", null, "MyContent-1");
            await content.SaveAsync().ConfigureAwait(false);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.IsTrue(arguments[0].ToString().Contains("/Root('Content')")); // parentPath
            Assert.AreEqual(HttpMethod.Post, (HttpMethod)arguments[1]);
            var json = (string)arguments[2]!;
            json = json.Substring("models=[".Length).TrimEnd(']');
            dynamic data = JsonHelper.Deserialize(json);
            Assert.IsNotNull(data);
            Assert.AreEqual("MyContent-1", data.Name.ToString());
            Assert.AreEqual("MyContent_Two", data.__ContentType.ToString());
        }
        [TestMethod]
        public async Task Repository_T_CreateContentAndSave_SpecifiedDifferentName()
        {
            var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""MyContent"",
    ""Path"": ""/Root/MyContent"",
    ""Type"": ""Folder"",
    ""Index"": 99
  }
}");

            var repoName = "MyRepo";
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.ConfigureSenseNetRepository(repoName,
                    configure: options => { options.Url = ExampleUrl; },
                    registerContentTypes: contentTypes =>
                    {
                        contentTypes.Add<MyContent2>();
                        contentTypes.Add<MyContent2>("MyContent_2");
                        contentTypes.Add<MyContent2>("MyContent_Two");
                    });
            });
            var repository = await repositories.GetRepositoryAsync(repoName, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var content = repository.CreateContent<MyContent2>("/Root/Content", "MyContent_Two", "MyContent-1");
            await content.SaveAsync().ConfigureAwait(false);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().ToArray()[1].GetArguments();
            Assert.IsTrue(arguments[0].ToString().Contains("/Root('Content')")); // parentPath
            Assert.AreEqual(HttpMethod.Post, (HttpMethod)arguments[1]);
            var json = (string)arguments[2]!;
            json = json.Substring("models=[".Length).TrimEnd(']');
            dynamic data = JsonHelper.Deserialize(json);
            Assert.IsNotNull(data);
            Assert.AreEqual("MyContent-1", data.Name.ToString());
            Assert.AreEqual("MyContent_Two", data.__ContentType.ToString());
        }

        /* =================================================================== CONTENT REGISTRATION AND CREATION EXAMPLES */

        [TestMethod]
        public async Task Repository_T_CreateContent_EXAMPLE_GlobalContentTypes()
        {
            var cancel = new CancellationTokenSource().Token;
            var services = new ServiceCollection()
                .AddLogging()
                .AddSenseNetClient()
                .RegisterGlobalContentType<MyContent>()
                .RegisterGlobalContentType<MyContent2>()
                .ConfigureSenseNetRepository(repositoryOptions => { })
                .BuildServiceProvider();
            var repository = await services.GetRequiredService<IRepositoryCollection>().GetRepositoryAsync(cancel);

            // ACTION
            var content = repository.CreateContent<MyContent2>("/Root/Content", null, "MyContent-1");

            // ASSERT
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent2));
        }
        [TestMethod]
        public async Task Repository_T_CreateContent_EXAMPLE_LocalContentTypes()
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
            var services = new ServiceCollection()
                .AddLogging()
                .AddSenseNetClient()
                .ConfigureSenseNetRepository(repositoryOptions => { },
                    registeredContentTypes =>
                    {
                        registeredContentTypes.Add<MyContent>();
                        registeredContentTypes.Add<MyContent2>();
                    })
                .BuildServiceProvider();

            // ACT
            var repository = await services.GetRequiredService<IRepositoryCollection>().GetRepositoryAsync(cancel);

            // ACTION
            var content = repository.CreateContent<MyContent2>("/Root/Content", null, "MyContent-1");

            // ASSERT
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent2));
        }
        [TestMethod]
        public async Task Repository_T_CreateContent_EXAMPLE_ReRegisterLocally()
        {
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
            var services = new ServiceCollection()
                .AddLogging()
                .AddSenseNetClient()
                .RegisterGlobalContentType<MyContent>()
                .RegisterGlobalContentType<MyContent2>()
                .ConfigureSenseNetRepository(repositoryOptions => { },
                    registeredContentTypes =>
                    {
                        registeredContentTypes.Add<MyContent3>("MyContent2");
                    })
                .BuildServiceProvider();

            // ACT
            var repository = await services.GetRequiredService<IRepositoryCollection>().GetRepositoryAsync(cancel);

            // ACTION
            var content = repository.CreateContent("/Root/Content", "MyContent2", "MyContent-1");

            // ASSERT
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType(content, typeof(MyContent3));
        }

        /* =================================================================== LOAD CONTENT */

        [TestMethod]
        public async Task Repository_LoadContent_Request_ByPath()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var content = await repository.LoadContentAsync("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root('Content')?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Content", content.Name);
        }

        [TestMethod]
        public async Task Repository_LoadContent_GeneralType()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(content);
            Assert.AreEqual("Content", content.Name);
        }
        [TestMethod]
        public async Task Repository_LoadContent_KnownCustomTypeById()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.AddTransient<MyContent, MyContent>();
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync<MyContent>(42, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("Hello Content!", content.HelloMessage);
        }
        [TestMethod]
        public async Task Repository_LoadContent_KnownCustomType_ByPath()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.AddTransient<MyContent, MyContent>();
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync<MyContent>("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("Hello Content!", content.HelloMessage);
        }
        [TestMethod]
        public async Task Repository_LoadContent_KnownCustomTypeAsGeneral()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Type"": ""MyContent"", ""Name"": ""Content"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);

                services.RegisterGlobalContentType<MyContent>();
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACTION
            var content = await repository.LoadContentAsync("/Root/Content", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.IsNotNull(content);
            Assert.AreEqual("Content", content.Name);
            Assert.AreEqual(typeof(MyContent), content.GetType());
        }

        /* =================================================================== LOAD COLLECTION */

        [TestMethod]
        public async Task Repository_T_LoadCollection()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 4, ""results"": [
                    {""Id"": 10001, ""Name"": ""Content1"", ""Type"": ""MyContent""},
                    {""Id"": 10002, ""Name"": ""Content2"", ""Type"": ""MyContent2""},
                    {""Id"": 10003, ""Name"": ""Content3"", ""Type"": ""MyContent3""},
                    {""Id"": 10004, ""Name"": ""Content4"", ""Type"": ""MyContent4""},
                    ]}}");

            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.RegisterGlobalContentType<MyContent>();
                services.RegisterGlobalContentType<MyContent2>();
                services.RegisterGlobalContentType<MyContent3>();
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            var request = new LoadCollectionRequest { Path = "/Root/Somewhere", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var collection = await repository.LoadCollectionAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/Somewhere?metadata=no&$select=Id,Name,Type", requestedUri.PathAndQuery);

            var contents = collection.ToArray();
            Assert.AreEqual(4, contents.Length);
            var typeNames = string.Join(", ", contents.Select(x => x.GetType().Name));
            Assert.AreEqual("MyContent, MyContent2, MyContent3, Content", typeNames);
        }
        [TestMethod]
        public async Task Repository_T_LoadCollection_TotalCount()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 11, ""results"": [
                    {""Id"": 10001, ""Name"": ""Content1"", ""Type"": ""MyContent""},
                    {""Id"": 10002, ""Name"": ""Content2"", ""Type"": ""MyContent2""},
                    {""Id"": 10003, ""Name"": ""Content3"", ""Type"": ""MyContent3""},
                    {""Id"": 10004, ""Name"": ""Content4"", ""Type"": ""MyContent4""},
                    ]}}");

            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.RegisterGlobalContentType<MyContent>();
                services.RegisterGlobalContentType<MyContent2>();
                services.RegisterGlobalContentType<MyContent3>();
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            var request = new LoadCollectionRequest { Path = "/Root/Somewhere", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var collection = await repository.LoadCollectionAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/Somewhere?metadata=no&$select=Id,Name,Type", requestedUri.PathAndQuery);

            Assert.AreEqual(4, collection.Count);
            Assert.AreEqual(11, collection.TotalCount);

            var contents = collection.ToArray();
            Assert.AreEqual(collection.Count, contents.Length);
        }

        public class Item1 : Content { public Item1(IRestCaller rc, ILogger<MyContent> l) : base(rc, l) { } }
        public class Item2 : Item1 { public Item2(IRestCaller rc, ILogger<MyContent> l) : base(rc, l) { } }
        public class Item3 : Item2 { public Item3(IRestCaller rc, ILogger<MyContent> l) : base(rc, l) { } }
        public class Item4 : Item3 { public Item4(IRestCaller rc, ILogger<MyContent> l) : base(rc, l) { } }
        [TestMethod] public Task Repository_T_LoadCollection_T_Content() => LoadCollectionTest<Content>(false);
        [TestMethod] public Task Repository_T_LoadCollection_T_Item1() => LoadCollectionTest<Item1>(false);
        [TestMethod] public Task Repository_T_LoadCollection_T_Item2() => LoadCollectionTest<Item2>(true);
        [TestMethod] public Task Repository_T_LoadCollection_T_Item3() => LoadCollectionTest<Item3>(true);
        private async Task LoadCollectionTest<T>(bool isExceptionExpected) where T : Content
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 4, ""results"": [
                    {""Id"": 10001, ""Name"": ""Content1"", ""Type"": ""Item1""},
                    {""Id"": 10002, ""Name"": ""Content2"", ""Type"": ""Item2""},
                    {""Id"": 10003, ""Name"": ""Content3"", ""Type"": ""Item3""},
                    {""Id"": 10004, ""Name"": ""Content4"", ""Type"": ""Item4""},
                    ]}}");

            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.RegisterGlobalContentType<Item1>();
                services.RegisterGlobalContentType<Item2>();
                services.RegisterGlobalContentType<Item3>();
                services.RegisterGlobalContentType<Item4>();
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            var request = new LoadCollectionRequest { Path = "/Root/Somewhere", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            T[] contents;
            try
            {
                var collection = await repository.LoadCollectionAsync<T>(request, CancellationToken.None);
                contents = collection.ToArray();
                if (isExceptionExpected)
                    Assert.Fail("The expected InvalidCastException was not thrown.");
            }
            catch (InvalidCastException e)
            {
                // Unable to cast object of type 'Item1' to type '{T}'.
                Assert.AreEqual($"Unable to cast object of type 'Item1' to type '{typeof(T).Name}'.", e.Message);
                return;
            }

            // ASSERT
            Assert.AreEqual(4, contents.Length);
            var typeNames = string.Join(", ", contents.Select(x => x.GetType().Name));
            Assert.AreEqual("Item1, Item2, Item3, Item4", typeNames);
        }

        /* =================================================================== QUERY */

        [TestMethod] public Task Repository_T_Query_T_Content() => QueryTest<Content>(false);
        [TestMethod] public Task Repository_T_Query_T_Item1() => QueryTest<Item1>(false);
        [TestMethod] public Task Repository_T_Query_T_Item2() => QueryTest<Item2>(true);
        [TestMethod] public Task Repository_T_Query_T_Item3() => QueryTest<Item3>(true);
        private async Task QueryTest<T>(bool isExceptionExpected) where T : Content
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 4, ""results"": [
                    {""Id"": 10001, ""Name"": ""Content1"", ""Type"": ""Item1""},
                    {""Id"": 10002, ""Name"": ""Content2"", ""Type"": ""Item2""},
                    {""Id"": 10003, ""Name"": ""Content3"", ""Type"": ""Item3""},
                    {""Id"": 10004, ""Name"": ""Content4"", ""Type"": ""Item4""},
                    ]}}");

            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.RegisterGlobalContentType<Item1>();
                services.RegisterGlobalContentType<Item2>();
                services.RegisterGlobalContentType<Item3>();
                services.RegisterGlobalContentType<Item4>();
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            var request = new QueryContentRequest { /* Irrelevant because of mocking */ };

            // ACT
            T[] contents;
            try
            {
                var collection = await repository.QueryAsync<T>(request, CancellationToken.None);
                contents = collection.ToArray();
                if (isExceptionExpected)
                    Assert.Fail("The expected InvalidCastException was not thrown.");
            }
            catch (InvalidCastException e)
            {
                // Unable to cast object of type 'Item1' to type '{T}'.
                Assert.AreEqual($"Unable to cast object of type 'Item1' to type '{typeof(T).Name}'.", e.Message);
                return;
            }

            // ASSERT
            Assert.AreEqual(4, contents.Length);
            var typeNames = string.Join(", ", contents.Select(x => x.GetType().Name));
            Assert.AreEqual("Item1, Item2, Item3, Item4", typeNames);
        }


        [TestMethod] public Task Repository_T_QueryForAdmin_T_Content() => QueryForAdminTest<Content>(false);
        [TestMethod] public Task Repository_T_QueryForAdmin_T_Item1() => QueryForAdminTest<Item1>(false);
        [TestMethod] public Task Repository_T_QueryForAdmin_T_Item2() => QueryForAdminTest<Item2>(true);
        [TestMethod] public Task Repository_T_QueryForAdmin_T_Item3() => QueryForAdminTest<Item3>(true);
        private async Task QueryForAdminTest<T>(bool isExceptionExpected) where T : Content
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""__count"": 4, ""results"": [
                    {""Id"": 10001, ""Name"": ""Content1"", ""Type"": ""Item1""},
                    {""Id"": 10002, ""Name"": ""Content2"", ""Type"": ""Item2""},
                    {""Id"": 10003, ""Name"": ""Content3"", ""Type"": ""Item3""},
                    {""Id"": 10004, ""Name"": ""Content4"", ""Type"": ""Item4""},
                    ]}}");

            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
                services.RegisterGlobalContentType<Item1>();
                services.RegisterGlobalContentType<Item2>();
                services.RegisterGlobalContentType<Item3>();
                services.RegisterGlobalContentType<Item4>();
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);
            var request = new QueryContentRequest { /* Irrelevant because of mocking */ };

            // ACT
            T[] contents;
            try
            {
                var collection = await repository.QueryForAdminAsync<T>(request, CancellationToken.None);
                contents = collection.ToArray();
                if (isExceptionExpected)
                    Assert.Fail("The expected InvalidCastException was not thrown.");
            }
            catch (InvalidCastException e)
            {
                // Unable to cast object of type 'Item1' to type '{T}'.
                Assert.AreEqual($"Unable to cast object of type 'Item1' to type '{typeof(T).Name}'.", e.Message);
                return;
            }

            // ASSERT
            Assert.AreEqual(4, contents.Length);
            var typeNames = string.Join(", ", contents.Select(x => x.GetType().Name));
            Assert.AreEqual("Item1, Item2, Item3, Item4", typeNames);
        }

        /* ============================================================================ AUTHENTICATION */

        [TestMethod]
        public async Task Repository_Auth_GetCurrentUser_ValidUser_ValidToken()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": { 
""Name"": ""Admin"", ""Id"": 1, ""Type"": ""User"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // this is a test token containing the admin id (1) as a SUB
            repository.Server.Authentication.AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibm" +
                                                           "FtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.ZU43TYZENiuL" +
                                                           "dKJPpd-hnkFhRkpLPurixsKr-8m-kBc";

            // ACT
            var content = await repository.GetCurrentUserAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/content(1)?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Admin", content.Name);
        }
        [TestMethod]
        public async Task Repository_Auth_GetCurrentUser_ValidUser_ValidToken_WithParameters()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": { 
""Name"": ""Admin"", ""Id"": 1, ""Type"": ""User"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // this is a test token containing the admin id (1) as a SUB
            repository.Server.Authentication.AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibm" +
                                                           "FtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.ZU43TYZENiuL" +
                                                           "dKJPpd-hnkFhRkpLPurixsKr-8m-kBc";

            // ACT
            // define select and expand parameters
            var content = await repository.GetCurrentUserAsync(
                new []{"Id", "Name", "Type", "Manager/Name"}, 
                new []{"Manager"},
                CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/content(1)?metadata=no&$expand=Manager&$select=Id,Name,Type,Manager/Name",
                requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Admin", content.Name);
        }
        [TestMethod]
        public async Task Repository_Auth_GetCurrentUser_ValidUser_ExpiredToken()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();

            // first call: expired token, inaccessible user id
            restCaller
                .GetResponseStringAsync(Arg.Is<Uri>(uri => uri.PathAndQuery.Contains("/OData.svc/content(123456)")),
                    Arg.Any<HttpMethod>(), Arg.Any<string>(), 
                    Arg.Any<Dictionary<string, IEnumerable<string>>>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(string.Empty));

            // second call: GetCurrentUser action, returns the Visitor user
            restCaller.GetResponseStringAsync(Arg.Is<Uri>(uri => uri.PathAndQuery.Contains("/OData.svc/('Root')/GetCurrentUser")),
                    Arg.Any<HttpMethod>(), Arg.Any<string>(),
                    Arg.Any<Dictionary<string, IEnumerable<string>>>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{""d"": { ""Name"": ""Visitor"", ""Id"": 6, ""Type"": ""User"" }}"));
            
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // this is a test token containing 123456 (INACCESSIBLE user) as a SUB
            repository.Server.Authentication.AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMj" +
                                                           "M0NTYiLCJuYW1lIjoiSm9obiBEb2UiLCJpYXQiOjE1MTYyMzkwM" +
                                                           "jJ9.MkiS50WhvOFwrwxQzd5Kp3VzkQUZhvex3kQv-CLeS3M";

            // ACT
            var content = await repository.GetCurrentUserAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri1 = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.AreEqual("/OData.svc/content(123456)?metadata=no", requestedUri1.PathAndQuery);

            var requestedUri2 = (Uri)restCaller.ReceivedCalls().ToArray()[2].GetArguments().First()!;
            Assert.AreEqual("/OData.svc/('Root')/GetCurrentUser?metadata=no", requestedUri2.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Visitor", content.Name);
        }
        [TestMethod]
        public async Task Repository_Auth_GetCurrentUser_ValidUser_UnknownToken()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""Name"": ""Admin"", ""Id"": 1, ""Type"": ""User"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // edge case: this is a not parseable token that is still accepted by the server
            repository.Server.Authentication.AccessToken = "not parseable token";

            // ACT
            var content = await repository.GetCurrentUserAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/GetCurrentUser?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Admin", content.Name);
        }
        [TestMethod]
        public async Task Repository_Auth_GetCurrentUser_ValidUser_ApiKey()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{""d"": {""Name"": ""Admin"", ""Id"": 1, ""Type"": ""User"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // we provide an api key instead of an access token
            repository.Server.Authentication.ApiKey = "valid api key";

            // ACT
            var content = await repository.GetCurrentUserAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/GetCurrentUser?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Admin", content.Name);
        }
        [TestMethod]
        public async Task Repository_Auth_GetCurrentUser_Visitor_NoToken()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{ ""d"": {
""Name"": ""Visitor"", ""Id"": 6, ""Type"": ""User"" }}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // no token
            repository.Server.Authentication.AccessToken = null;

            // ACT
            var content = await repository.GetCurrentUserAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/IMS/BuiltIn/Portal('Visitor')?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(content);
            Assert.AreEqual("Visitor", content.Name);
        }

        /* ====================================================================== CUSTOM REQUESTS */

        private class CustomNestedObject
        {
            public string Property11 { get; set; }
            public int Property12 { get; set; }
        }
        private class CustomObject
        {
            public string Property1 { get; set; }
            public CustomNestedObject Property2 { get; set; }
            public int[] Property3 { get; set; }
        }

        [TestMethod]
        public async Task Repository_CustomRequest_Json()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{
""Property1"": ""Value1"",
""Property2"": {
    ""Property11"": ""Value11"",
    ""Property12"": 122,
  },
""Property3"": [1, 42, 999],
}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new ODataRequest {ContentId = 42, ActionName = "Whatever" /* just for mock request */ };
            var jsonResult = await repository.GetResponseJsonAsync(request, HttpMethod.Get, default);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/content(42)/Whatever?metadata=no", requestedUri.PathAndQuery);

            var jObject = jsonResult as JObject;
            Assert.IsNotNull(jObject);
            var customObject = jObject.ToObject<CustomObject>();
            Assert.AreEqual("Value1", customObject.Property1);
            Assert.AreEqual("Value11", customObject.Property2.Property11);
            Assert.AreEqual(122, customObject.Property2.Property12);
            Assert.AreEqual("1,42,999", string.Join(",", customObject.Property3.Select(x=>x.ToString())));
        }
        [TestMethod]
        public async Task Repository_CustomRequest_CustomObject()
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(@"{
""Property1"": ""Value1"",
""Property2"": {
    ""Property11"": ""Value11"",
    ""Property12"": 122,
  },
""Property3"": [1, 42, 999],
}");
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new ODataRequest { ContentId = 42, ActionName = "Whatever" /* just for mock request */ };
            var customObject = await repository.GetResponseAsync<CustomObject>(request, HttpMethod.Get, default);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/content(42)/Whatever?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(customObject);
            Assert.AreEqual("Value1", customObject.Property1);
            Assert.AreEqual("Value11", customObject.Property2.Property11);
            Assert.AreEqual(122, customObject.Property2.Property12);
            Assert.AreEqual("1,42,999", string.Join(",", customObject.Property3.Select(x => x.ToString())));
        }
        [TestMethod]
        public async Task Repository_CustomRequest_IntegralTypeResponse()
        {
            Assert.AreEqual(145, await IntegralTypeResponseTest<int>("145"));
            Assert.AreEqual(145L, await IntegralTypeResponseTest<long>("145"));
            Assert.AreEqual(145.789d, await IntegralTypeResponseTest<double>("145.789"));
            Assert.AreEqual(145.789d.ToString(CultureInfo.CurrentCulture),
                await IntegralTypeResponseTest<string>("145.789"));
        }

        private async Task<T> IntegralTypeResponseTest<T>(string odataResponse)
        {
            // ALIGN
            var restCaller = CreateRestCallerFor(odataResponse);
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new ODataRequest { ContentId = 42, ActionName = "Whatever" /* just for mock request */ };
            var customObject = await repository.GetResponseAsync<T>(request, HttpMethod.Get, default);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/content(42)/Whatever?metadata=no", requestedUri.PathAndQuery);

            Assert.IsNotNull(customObject);
            return customObject;
        }

        /* ====================================================================== TOOLS */

        public async Task TestParameterError<TException>(Action<IRepository> callback, string expectedMessage) where TException : Exception
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);

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
        public async Task TestParameterError<TException>(Func<IRepository, Task> callback, string expectedMessage) where TException : Exception
        {
            var repository = await GetRepositoryCollection().GetRepositoryAsync(FakeServer, CancellationToken.None);

            try
            {
                await callback(repository);
                Assert.Fail($"The expected {typeof(TException).Name} was not thrown.");
            }
            catch (TException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
            }
        }
    }
}
