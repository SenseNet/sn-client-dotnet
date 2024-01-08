using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class ContentGeneralPropertiesTests : TestBase
{
    private CancellationToken _cancel = new CancellationTokenSource().Token;

    [TestMethod]
    public async Task GeneralProps_T_Load_VersioningModes_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            //services.RegisterGlobalContentType<TestContent_CustomProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Content>(request, CancellationToken.None);

        // ASSERT
        Assert.IsNull(content.VersioningMode);
        Assert.IsNull(content.InheritableVersioningMode);
    }
    [TestMethod]
    public async Task GeneralProps_T_Load_VersioningModes_NotNull()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""VersioningMode"": [ ""1"" ],
    ""InheritableVersioningMode"": [ ""2"" ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            //services.RegisterGlobalContentType<TestContent_CustomProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Content>(request, CancellationToken.None);

        // ASSERT
        Assert.IsNotNull(content.VersioningMode);
        Assert.AreEqual(VersioningMode.None, content.VersioningMode);
        Assert.IsNotNull(content.InheritableVersioningMode);
        Assert.AreEqual(VersioningMode.MajorOnly, content.InheritableVersioningMode);
    }

    [TestMethod]
    public async Task GeneralProps_T_Save_VersioningModes_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""Content1"",
    ""VersioningMode"": [ ""1"" ],
    ""InheritableVersioningMode"": [ ""2"" ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            //services.RegisterGlobalContentType<TestContent_CustomProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Content>(request, CancellationToken.None);

        // ACT
        content.VersioningMode = null;
        content.InheritableVersioningMode = null;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        dynamic data = JsonHelper.Deserialize(json);
        var dict = data.ToObject<Dictionary<string, object>>();
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("Name, VersioningMode, InheritableVersioningMode", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_VersioningModes_NotNull()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""Content1"",
    ""VersioningMode"": [ ""1"" ],
    ""InheritableVersioningMode"": [ ""2"" ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            //services.RegisterGlobalContentType<TestContent_CustomProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Content>(request, CancellationToken.None);

        // ACT
        content.VersioningMode = VersioningMode.None;
        content.InheritableVersioningMode = VersioningMode.MajorAndMinor;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        dynamic data = JsonHelper.Deserialize(json);
        var dict = data.ToObject<Dictionary<string, object>>();
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("Name, VersioningMode, InheritableVersioningMode", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
        Assert.AreEqual("[\"1\"]", RemoveWhitespaces(data.VersioningMode.ToString()));
        Assert.AreEqual("[\"3\"]", RemoveWhitespaces(data.InheritableVersioningMode.ToString()));
    }
}