using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Linq;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class LinqTests : TestBase
{
    private async Task LinqTest(Action<IRepository> callback)
    {
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""Content1"",
    ""Type"": ""Content"",
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            //services.RegisterGlobalContentType<TestContent_CustomProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);


    }

    [TestMethod]
    public async Task Linq_Bool()
    {
        await LinqTest(repository =>
        {
            var q = GetQueryString(repository.Content.Where(c => c.Id > 42 && c.IsFolder == true));
            //var q = GetQueryString(repository.Content.Where(c => (bool)c["IsFolder"] == true));
            Assert.AreEqual("IsFolder:yes", q);

            q = GetQueryString(repository.Content.Where(c => c.IsFolder == false));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(repository.Content.Where(c => c.IsFolder != true));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(repository.Content.Where(c => true == c.IsFolder));
            Assert.AreEqual("IsFolder:yes", q);

            q = GetQueryString(repository.Content.Where(c => (bool)c["Hidden"]));
            Assert.AreEqual("Hidden:yes", q);

            q = GetQueryString(repository.Content.OfType<Workspace>().Where(c => c.IsWallContainer == true));
            Assert.AreEqual("+TypeIs:workspace +IsWallContainer:yes", q);
        });
    }

    private string GetQueryString<T>(IQueryable<T> queryable)
    {
        var cs = queryable.Provider as ContentSet<T>;
        return cs?.GetCompiledQuery().ToString() ?? string.Empty;
    }

}