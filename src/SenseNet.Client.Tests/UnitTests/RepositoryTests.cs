using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using SenseNet.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace SenseNet.Client.Tests.UnitTests
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

        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_IdVersion()
        {
            await ODataRequestForLoadContentTest(
                repository => new LoadContentRequest { ContentId = 42, Version = "V1.0.A" },
                "/OData.svc/content(42)?metadata=no&version=V1.0.A");
        }
        [TestMethod]
        public async Task Repository_LoadContent_ByOdataRequest_PathVersion()
        {
            await ODataRequestForLoadContentTest(
                repository => new LoadContentRequest { Path = "/Root/Content/MyFolder", Version = "V1.0.A" },
                "/OData.svc/Root/Content('MyFolder')?metadata=no&version=V1.0.A");
        }

        private async Task<(IRepository Repository, IRestCaller RestCaller)> GetDefaultRepositoryAndRestCallerMock()
        {
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Name"": ""Content"" }}"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            return (repository, restCaller);
        }
        private async Task ODataRequestForLoadContentTest(Func<IRepository, LoadContentRequest> getLoadContentRequest, string expectedUrl)
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Name"": ""Content"" }}"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var content = await repository.LoadContentAsync(getLoadContentRequest(repository), CancellationToken.None)
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
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{""d"": {""__count"": 2, ""results"": [
                    {""Id"": 3, ""Name"": ""IMS"", ""Type"": ""Domains""},
                    {""Id"": 1000, ""Name"": ""System"", ""Type"": ""SystemFolder""}]}}"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new LoadCollectionRequest { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

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
        [TestMethod]
        public async Task Repository_GetContentCount()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"42"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new LoadCollectionRequest { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var count = await repository.GetContentCountAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root/$count?metadata=no&$select=Id,Name,Type", requestedUri.PathAndQuery);

            Assert.AreEqual(42, count);
        }

        /* ====================================================================== QUERY CONTENT */

        [TestMethod]
        public async Task Repository_QueryForAdmin()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{""d"": {""__count"": 4, ""results"": [
      {""Name"": ""Admin""},
      {""Name"": ""PublicAdmin""},
      {""Name"": ""Somebody""},
      {""Name"": ""Visitor""}]}}"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new QueryContentRequest {ContentQuery = "TypeIs:User .SORT:Name", Select = new[] {"Name"}};
            var collection = await repository.QueryForAdminAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root?metadata=no&$select=Name&enableautofilters=false&enablelifespanfilter=false&query=TypeIs%3AUser%20.SORT%3AName", requestedUri.PathAndQuery);

            var actual = string.Join(", ", collection.Select(c => c.Name));
            Assert.AreEqual("Admin, PublicAdmin, Somebody, Visitor", actual);
        }
        [TestMethod]
        public async Task Repository_Query()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{""d"": {""__count"": 4, ""results"": [
      {""Name"": ""Admin""},
      {""Name"": ""PublicAdmin""},
      {""Name"": ""Somebody""},
      {""Name"": ""Visitor""}]}}"));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);

            // ACT
            var request = new QueryContentRequest { ContentQuery = "TypeIs:User .SORT:Name", Select = new[] { "Name" } };
            var collection = await repository.QueryAsync(request, CancellationToken.None);

            // ASSERT
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/Root?metadata=no&$select=Name&query=TypeIs%3AUser%20.SORT%3AName", requestedUri.PathAndQuery);

            var actual = string.Join(", ", collection.Select(c => c.Name));
            Assert.AreEqual("Admin, PublicAdmin, Somebody, Visitor", actual);
        }

        /* ====================================================================== CONTENT EXISTENCE */

        [TestMethod]
        public async Task Repository_IsContentExists_Yes()
        {
            // ALIGN
            var infrastructure = await GetDefaultRepositoryAndRestCallerMock();
            var restCaller = infrastructure.RestCaller;
            var repository = infrastructure.Repository;

            restCaller.GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{ ""d"": { ""Id"": 42 }}"));

            // ACT
            var isExist = await repository.IsContentExistsAsync("/Root/Content/MyContent", CancellationToken.None)
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

            restCaller.GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(string.Empty));

            // ACT
            var isExist = await repository.IsContentExistsAsync("/Root/Content/MyContent", CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT REQUEST
            var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First();
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
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(""));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new ODataRequest(repository.Server) { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            await repository.DeleteContentAsync("/Root/Content/MyContent", true, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().Single().GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[3] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[4] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":true,\"paths\":[\"/Root/Content/MyContent\"]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ByPath()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(""));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new ODataRequest(repository.Server) { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            await repository.DeleteContentAsync("/Root/Content/MyContent", false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().Single().GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[3] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[4] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[\"/Root/Content/MyContent\"]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ByPaths()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(""));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new ODataRequest(repository.Server) { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var paths = new[] { "/Root/F1", "/Root/F2", "/Root/F3" };
            await repository.DeleteContentAsync(paths, false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().Single().GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[3] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[4] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[\"/Root/F1\",\"/Root/F2\",\"/Root/F3\"]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ById()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(""));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new ODataRequest(repository.Server) { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            await repository.DeleteContentAsync(1234, false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().Single().GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[3] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[4] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[1234]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ByIds()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(""));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new ODataRequest(repository.Server) { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var ids = new[] { 1234, 1235, 1236, 1237 };
            await repository.DeleteContentAsync(ids, false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().Single().GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[3] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[4] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[1234,1235,1236,1237]}]", jsonBody);
        }
        [TestMethod]
        public async Task Repository_Delete_ByIdsOrPaths()
        {
            // ALIGN
            var restCaller = Substitute.For<IRestCaller>();
            restCaller
                .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(""));
            var repositories = GetRepositoryCollection(services =>
            {
                services.AddSingleton(restCaller);
            });
            var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
                .ConfigureAwait(false);
            var request = new ODataRequest(repository.Server) { Path = "/Root", Select = new[] { "Id", "Name", "Type" } };

            // ACT
            var idsOrPaths = new object[] { "/Root/F1", 1234, "/Root/F2", 1235, 1236 };
            await repository.DeleteContentAsync(idsOrPaths, false, CancellationToken.None);

            // ASSERT
            var arguments = restCaller.ReceivedCalls().Single().GetArguments();
            Assert.AreEqual(5, arguments.Length);
            var requestedUri = arguments[0] as Uri;
            Assert.IsNotNull(requestedUri);
            Assert.AreEqual("/OData.svc/('Root')/DeleteBatch?metadata=no", requestedUri.PathAndQuery);
            var method = arguments[3] as HttpMethod;
            Assert.IsNotNull(method);
            Assert.AreEqual(HttpMethod.Post, method);
            var jsonBody = arguments[4] as string;
            Assert.IsNotNull(jsonBody);
            Assert.AreEqual("models=[{\"permanent\":false,\"paths\":[\"/Root/F1\",1234,\"/Root/F2\",1235,1236]}]", jsonBody);
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
