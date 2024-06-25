using Microsoft.Extensions.Logging;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.IntegrationTests;

[TestClass]
public class QueryTests : IntegrationTestBase
{
    public class Identity : Content { public Identity(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { } }
    public class TestUser : Identity { public TestUser(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { } }
    public class TestGroup : Identity { public TestGroup(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { } }

    [TestMethod]
    public async Task IT_Content_Query_T_AllUsers()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection(services =>
            {
                services
                    //.RegisterGlobalContentType<TestUser>("Identity")
                    .RegisterGlobalContentType<TestUser>("User")
                    .RegisterGlobalContentType<TestGroup>("Group");
            })
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        var request = new QueryContentRequest
        {
            ContentQuery = "TypeIs:User",
            Select = new[] { "Id", "Path", "Name", "Type" },
            OrderBy = new[] { "Name" }
        };
        var contents = await repository.QueryAsync<TestUser>(request, cancel).ConfigureAwait(false);

        // ASSERT
        var names = contents.Select(x => x.Name).ToArray();
        Assert.IsTrue(names.Contains("Admin"));
        Assert.IsTrue(names.Contains("Visitor"));
        var types = contents.Select(x => x.GetType().Name).Distinct().ToArray();
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("TestUser", types[0]);
    }
    [TestMethod]
    public async Task IT_Content_Query_T_AllGroups()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection(services =>
            {
                services
                    //.RegisterGlobalContentType<TestUser>("Identity")
                    .RegisterGlobalContentType<TestUser>("User")
                    .RegisterGlobalContentType<TestGroup>("Group");
            })
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        var request = new QueryContentRequest
        {
            ContentQuery = "TypeIs:Group",
            Select = new[] { "Id", "Path", "Name", "Type" },
            OrderBy = new[] { "Name" }
        };
        var contents = await repository.QueryAsync<TestGroup>(request, cancel).ConfigureAwait(false);

        // ASSERT
        var names = contents.Select(x => x.Name).ToArray();
        Assert.IsTrue(names.Contains("Administrators"));
        Assert.IsTrue(names.Contains("Everyone"));
        var types = contents.Select(x => x.GetType().Name).Distinct().ToArray();
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("TestGroup", types[0]);
    }
    [TestMethod]
    public async Task IT_Content_Query_T_AllGroupsAndUsers()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection(services =>
            {
                services
                    //.RegisterGlobalContentType<TestUser>("Identity")
                    .RegisterGlobalContentType<TestUser>("User")
                    .RegisterGlobalContentType<TestGroup>("Group");
            })
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        var request = new QueryContentRequest
        {
            ContentQuery = "+TypeIs:(Group User)",
            Select = new[] { "Id", "Path", "Name", "Type" },
            OrderBy = new[] { "Name" }
        };
        // The result can contain items that are User or Group so need to give the common ancestor as the type parameter.
        var contents = await repository.QueryAsync<Identity>(request, cancel).ConfigureAwait(false);

        // ASSERT
        var names = contents.Select(x => x.Name).ToArray();
        Assert.IsTrue(names.Contains("Admin"));
        Assert.IsTrue(names.Contains("Visitor"));
        Assert.IsTrue(names.Contains("Administrators"));
        Assert.IsTrue(names.Contains("Everyone"));
        var types = contents.Select(x => x.GetType().Name).Distinct().OrderBy(x => x).ToArray();
        Assert.AreEqual(2, types.Length);
        Assert.AreEqual("TestGroup", types[0]);
        Assert.AreEqual("TestUser", types[1]);
    }

    [TestMethod]
    public async Task IT_Linq_Content_Where()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection(services =>
            {
                services
                    //.RegisterGlobalContentType<TestUser>("Identity")
                    .RegisterGlobalContentType<TestUser>("User")
                    .RegisterGlobalContentType<TestGroup>("Group");
            })
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        var contents = repository.Content
            .Where(u => u.InTree("/Root/IMS") && u.Name.StartsWith("Adm"))
            .OrderBy(c => c.Path)
            .Select(u => Content.Create<Content>(u.Name, u.Type, u.Path))
            .ToArray();

        // ASSERT
        Assert.IsTrue(contents.Length > 2);
        Assert.IsTrue(contents.Any(c => c.Name == "Admin" && c is TestUser));
        Assert.IsTrue(contents.Any(c => c.Name == "Administrators" && c is TestGroup));

        // Check that the result is in the correct order.
        var paths = contents.Select(c => c.Path).ToArray();
        var orderedPaths = contents.Select(c => c.Path).OrderBy(s => s).ToArray();
        Assert.AreEqual(string.Join(", ", orderedPaths), string.Join(", ", paths));
    }
}