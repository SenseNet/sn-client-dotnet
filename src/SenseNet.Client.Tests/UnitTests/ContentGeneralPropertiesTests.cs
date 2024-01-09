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

    [TestMethod]
    public async Task GeneralProps_T_Load_ApprovingModes_Null()
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
        Assert.IsNull(content.ApprovingMode);
        Assert.IsNull(content.InheritableApprovingMode);
    }
    [TestMethod]
    public async Task GeneralProps_T_Load_ApprovingModes_NotNull()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""ApprovingMode"": [ ""1"" ],
    ""InheritableApprovingMode"": [ ""2"" ],
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
        Assert.IsNotNull(content.ApprovingMode);
        Assert.AreEqual(ApprovingEnabled.No, content.ApprovingMode);
        Assert.IsNotNull(content.InheritableApprovingMode);
        Assert.AreEqual(ApprovingEnabled.Yes, content.InheritableApprovingMode);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_ApprovingModes_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""Content1"",
    ""ApprovingMode"": [ ""1"" ],
    ""InheritableApprovingMode"": [ ""2"" ],
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
        content.ApprovingMode = null;
        content.InheritableApprovingMode = null;
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
        Assert.AreEqual("Name, ApprovingMode, InheritableApprovingMode", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_ApprovingModes_NotNull()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""Content1"",
    ""ApprovingMode"": [ ""1"" ],
    ""InheritableApprovingMode"": [ ""0"" ],
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
        content.ApprovingMode = ApprovingEnabled.Inherited;
        content.InheritableApprovingMode = ApprovingEnabled.Yes;
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
        Assert.AreEqual("Name, ApprovingMode, InheritableApprovingMode", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
        Assert.AreEqual("[\"0\"]", RemoveWhitespaces(data.ApprovingMode.ToString()));
        Assert.AreEqual("[\"2\"]", RemoveWhitespaces(data.InheritableApprovingMode.ToString()));
    }

    [TestMethod]
    public async Task GeneralProps_T_Gender_NullToNull()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543}}", null, false, null);
    }
    [TestMethod]
    public async Task GeneralProps_T_Gender_NullToNotDefined()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543}}", Gender.NotDefined, true, "[\"...\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_Gender_NullToFemale()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543}}", Gender.Female, true, "[\"Female\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_Gender_EmptyToNull()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543,""Gender"": []}}", null, true, "");
    }
    [TestMethod]
    public async Task GeneralProps_T_Gender_EmptyToFemale()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543,""Gender"": []}}",
            Gender.Female, true, "[\"Female\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_Gender_NotDefinedToFemale()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543,""Gender"": [""...""]}}",
            Gender.Female, true, "[\"Female\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_Gender_FemaleToNull()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543,""Gender"": [""Female""]}}",
            null, true, "");
    }
    [TestMethod]
    public async Task GeneralProps_T_Gender_FemaleToNotDefined()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543,""Gender"": [""Female""]}}",
            Gender.NotDefined, true, "[\"...\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_Gender_FemaleToMale()
    {
        await GenderTest(@"{ ""d"": { ""Id"": 999543,""Gender"": [""Female""]}}",
            Gender.Male, true, "[\"Male\"]");
    }
    private async Task GenderTest(string loadString, Gender? editedValue, bool changed, string? expectedValueInSaveRequest)
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(loadString);

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            //services.RegisterGlobalContentType<TestContent_CustomProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var user = await repository.LoadContentAsync<User>(request, CancellationToken.None);

        // ACT
        user.Gender = editedValue;
        user.InheritableApprovingMode = ApprovingEnabled.Yes;
        await user.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        dynamic data = JsonHelper.Deserialize(json);
        if (!changed)
        {
            Assert.IsFalse(json.Contains("Gender"));
        }
        else
        {
            Assert.IsTrue(json.Contains("Gender"));
            Assert.AreEqual(expectedValueInSaveRequest, RemoveWhitespaces(data.Gender.ToString()));
        }
    }


    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_NullToNull()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543}}",
            null, false, null);
    }
    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_NullToNotDefined()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543}}",
            MaritalStatus.NotDefined, true, "[\"...\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_NullToSingle()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543}}",
            MaritalStatus.Single, true, "[\"Single\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_EmptyToNull()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543,""MaritalStatus"": []}}",
            null, true, "");
    }
    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_EmptyToSingle()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543,""MaritalStatus"": []}}",
            MaritalStatus.Single, true, "[\"Single\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_NotDefinedToSingle()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543,""MaritalStatus"": [""...""]}}",
            MaritalStatus.Single, true, "[\"Single\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_SingleToNull()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543,""MaritalStatus"": [""Single""]}}",
            null, true, "");
    }
    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_SingleToNotDefined()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543,""MaritalStatus"": [""Single""]}}",
            MaritalStatus.NotDefined, true, "[\"...\"]");
    }
    [TestMethod]
    public async Task GeneralProps_T_MaritalStatus_SingleToMarried()
    {
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543,""MaritalStatus"": [""Single""]}}",
            MaritalStatus.Married, true, "[\"Married\"]");
    }
    private async Task MaritalStatusTest(string loadString, MaritalStatus? editedValue, bool changed, string? expectedValueInSaveRequest)
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(loadString);

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            //services.RegisterGlobalContentType<TestContent_CustomProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var user = await repository.LoadContentAsync<User>(request, CancellationToken.None);

        // ACT
        user.MaritalStatus = editedValue;
        user.InheritableApprovingMode = ApprovingEnabled.Yes;
        await user.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        dynamic data = JsonHelper.Deserialize(json);
        if (!changed)
        {
            Assert.IsFalse(json.Contains("MaritalStatus"));
        }
        else
        {
            Assert.IsTrue(json.Contains("MaritalStatus"));
            Assert.AreEqual(expectedValueInSaveRequest, RemoveWhitespaces(data.MaritalStatus.ToString()));
        }
    }
}