using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
        Assert.AreEqual("VersioningMode, InheritableVersioningMode", keys);
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
        content.VersioningMode = VersioningMode.None; // not changed
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
        Assert.AreEqual("InheritableVersioningMode", keys);
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
        Assert.AreEqual("ApprovingMode, InheritableApprovingMode", keys);
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
        Assert.AreEqual("ApprovingMode, InheritableApprovingMode", keys);
        Assert.AreEqual("[\"0\"]", RemoveWhitespaces(data.ApprovingMode.ToString()));
        Assert.AreEqual("[\"2\"]", RemoveWhitespaces(data.InheritableApprovingMode.ToString()));
    }

    /* ====================================================================== FOLDER */

    [TestMethod]
    public async Task GeneralProps_T_Load_PreviewEnabled_Null()
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
            services.RegisterGlobalContentType<Folder>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Folder>(request, CancellationToken.None);

        // ASSERT
        Assert.IsNull(content.PreviewEnabled);
    }
    [TestMethod]
    public async Task GeneralProps_T_Load_PreviewEnabled_NotNull()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""PreviewEnabled"": [ ""2"" ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<Folder>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Folder>(request, CancellationToken.None);

        // ASSERT
        Assert.IsNotNull(content.PreviewEnabled);
        Assert.AreEqual(PreviewEnabled.Yes, content.PreviewEnabled);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_PreviewEnabled_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""Content1"",
    ""PreviewEnabled"": [ ""2"" ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<Folder>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Folder>(request, CancellationToken.None);

        // ACT
        content.PreviewEnabled = null;
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
        Assert.AreEqual("PreviewEnabled", keys);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_PreviewEnabled_NotNull()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""Content1"",
    ""PreviewEnabled"": [ ""0"" ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<Folder>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Folder>(request, CancellationToken.None);

        // ACT
        content.PreviewEnabled = PreviewEnabled.Yes;
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
        Assert.AreEqual("PreviewEnabled", keys);
        Assert.AreEqual("[\"2\"]", RemoveWhitespaces(data.PreviewEnabled.ToString()));
    }

    /* ====================================================================== USER */

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
        // Setting null on an enum means  "not changed" if that"s value was an empty array.
        await GenderTest(@"{ ""d"": { ""Id"": 999543,""Gender"": []}}",
            null, false, "");
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
        // Setting null on an enum means  "not changed" if that"s value was an empty array.
        await MaritalStatusTest(@"{ ""d"": { ""Id"": 999543,""MaritalStatus"": []}}",
            null, false, "");
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

    /* ====================================================================== TASK */

    [TestMethod]
    public async Task GeneralProps_T_Save_TaskPriority_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""Task"",
    ""Name"": ""Content1"",
    ""Priority"": [ ""1"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<SnTask>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<SnTask>(request, CancellationToken.None);
        Assert.AreEqual(TaskPriority.Urgent, content.Priority);

        // ACT
        content.Priority = null;
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
        Assert.AreEqual("Priority", keys);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_TaskPriority_NotNull()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""Task"",
    ""Name"": ""Content1"",
    ""Priority"": [ ""2"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<SnTask>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<SnTask>(request, CancellationToken.None);
        Assert.AreEqual(TaskPriority.Normal, content.Priority);

        // ACT
        content.Priority = TaskPriority.NotUrgent;
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
        Assert.AreEqual("Priority", keys);
        Assert.AreEqual("[\"3\"]", RemoveWhitespaces(data.Priority.ToString()));
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_TaskPriority_EmptyToUrgent()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""Task"",
    ""Name"": ""Content1"",
    ""Priority"": []
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<SnTask>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<SnTask>(request, CancellationToken.None);
        Assert.AreEqual(null, content.Priority);

        // ACT
        content.Priority = TaskPriority.Urgent;
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
        Assert.AreEqual("Priority", keys);
        Assert.AreEqual("[\"1\"]", RemoveWhitespaces(data.Priority.ToString()));
    }

    [TestMethod]
    public async Task GeneralProps_T_Save_TaskStatus_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""Task"",
    ""Name"": ""Content1"",
    ""Status"": [ ""pending"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<SnTask>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<SnTask>(request, CancellationToken.None);
        Assert.AreEqual(TaskState.Pending, content.Status);

        // ACT
        content.Status = null;
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
        var dict = (Dictionary<string, object>)data.ToObject<Dictionary<string, object>>();
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("Status", keys);
        Assert.AreEqual(null, dict["Status"]);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_TaskStatus_NotNull()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""Task"",
    ""Name"": ""Content1"",
    ""Status"": [ ""active"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<SnTask>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<SnTask>(request, CancellationToken.None);
        Assert.AreEqual(TaskState.Active, content.Status);

        // ACT
        content.Status = TaskState.Deferred;
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
        Assert.AreEqual("Status", keys);
        Assert.AreEqual("[\"deferred\"]", RemoveWhitespaces(data.Status.ToString()));
    }

    /* ====================================================================== MEMO */

    [TestMethod]
    public async Task GeneralProps_T_Save_MemoType_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""Memo"",
    ""Name"": ""Content1"",
    ""MemoType"": [ ""iaudit"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<Memo>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Memo>(request, CancellationToken.None);
        Assert.AreEqual(MemoType.InternalAudit, content.MemoType);

        // ACT
        content.MemoType = null;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("MemoType", keys);
        Assert.AreEqual(null, dict["MemoType"]);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_MemoType_InternalAuditToIso()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""Memo"",
    ""Name"": ""Content1"",
    ""MemoType"": [ ""iaudit"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<Memo>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<Memo>(request, CancellationToken.None);
        Assert.AreEqual(MemoType.InternalAudit, content.MemoType);

        // ACT
        content.MemoType = MemoType.Iso;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("MemoType", keys);
        var value = dict["MemoType"];
        Assert.IsNotNull(value);
        var valueAsJArray = value as JArray;
        Assert.IsNotNull(valueAsJArray);
        Assert.AreEqual("iso", valueAsJArray.FirstOrDefault());
    }

    /* ====================================================================== WebHookSubscription */

    [TestMethod]
    public async Task GeneralProps_T_Save_WebHookSubscription_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""WebHookSubscription"",
    ""Name"": ""Content1"",
    ""WebHookHttpMethod"": [ ""PATCH"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<WebHookSubscription>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<WebHookSubscription>(request, CancellationToken.None);
        Assert.AreEqual(WebHookHttpMethod.Patch, content.WebHookHttpMethod);

        // ACT
        content.WebHookHttpMethod = null;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("WebHookHttpMethod", keys);
        Assert.AreEqual(null, dict["WebHookHttpMethod"]);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_WebHookHttpMethod_PatchToDelete()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""WebHookSubscription"",
    ""Name"": ""Content1"",
    ""WebHookHttpMethod"": [ ""PATCH"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<WebHookSubscription>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<WebHookSubscription>(request, CancellationToken.None);
        Assert.AreEqual(WebHookHttpMethod.Patch, content.WebHookHttpMethod);

        // ACT
        content.WebHookHttpMethod = WebHookHttpMethod.Delete;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("WebHookHttpMethod", keys);
        var value = dict["WebHookHttpMethod"];
        Assert.IsNotNull(value);
        var valueAsJArray = value as JArray;
        Assert.IsNotNull(valueAsJArray);
        Assert.AreEqual("DELETE", valueAsJArray.FirstOrDefault());
    }

    /* ====================================================================== CalendarEvent */

    [TestMethod]
    public async Task GeneralProps_T_Save_NotificationMode_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""CalendarEvent"",
    ""Name"": ""Content1"",
    ""NotificationMode"": [ ""E-mail digest"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<CalendarEvent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<CalendarEvent>(request, CancellationToken.None);
        Assert.AreEqual(EventNotificationMode.EmailDigest, content.NotificationMode);

        // ACT
        content.NotificationMode = null;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("NotificationMode", keys);
        Assert.AreEqual(null, dict["NotificationMode"]);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_NotificationMode_EmailToNone()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""CalendarEvent"",
    ""Name"": ""Content1"",
    ""NotificationMode"": [ ""E-mail"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<CalendarEvent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<CalendarEvent>(request, CancellationToken.None);
        Assert.AreEqual(EventNotificationMode.Email, content.NotificationMode);

        // ACT
        content.NotificationMode = EventNotificationMode.None;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("NotificationMode", keys);
        var value = dict["NotificationMode"];
        Assert.IsNotNull(value);
        var valueAsJArray = value as JArray;
        Assert.IsNotNull(valueAsJArray);
        Assert.AreEqual("None", valueAsJArray.FirstOrDefault());
    }

    [TestMethod]
    public async Task GeneralProps_T_Save_EventType_Null()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""CalendarEvent"",
    ""Name"": ""Content1"",
    ""EventType"": [ ""Meeting"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<CalendarEvent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<CalendarEvent>(request, CancellationToken.None);
        Assert.AreEqual(EventType.Meeting, content.EventType);

        // ACT
        content.EventType = null;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("EventType", keys);
        Assert.AreEqual(null, dict["EventType"]);
    }
    [TestMethod]
    public async Task GeneralProps_T_Save_EventType_DeadlineAndMeetingToMeetingAndDemo()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""CalendarEvent"",
    ""Name"": ""Content1"",
    ""EventType"": [ ""Deadline"", ""Meeting"" ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<CalendarEvent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<CalendarEvent>(request, CancellationToken.None);
        Assert.AreEqual(EventType.Deadline | EventType.Meeting, content.EventType);

        // ACT
        content.EventType = EventType.Meeting | EventType.Demo;
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);
        Assert.AreEqual("EventType", keys);
        Assert.IsNotNull(dict["EventType"], "EventType is null.");
        Assert.AreEqual(typeof(JArray), dict["EventType"].GetType());
        var values = (JArray)dict["EventType"];
        Assert.AreEqual("Meeting", values[0].ToString());
        Assert.AreEqual("Demo", values[1].ToString());
    }

    /* ====================================================================== Reference handling */

    [TestMethod]
    public async Task GeneralProps_T_Load_Reference_NullDeferredExpanded()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 9125,
    ""Type"": ""CalendarEvent"",
    ""Name"": ""Event-1"",
    ""Path"": ""/Root/Content/Events/Event-1"",
    ""CreatedBy"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root/Content/Events('Event-1')/CreatedBy""
      }
    },
    ""Owner"": {
      ""Id"": 1,
      ""Type"": ""User"",
      ""Name"": ""Admin""
    },
    ""Workspace"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root/Content/Events('Event-1')/Workspace""
      }
    }
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<CalendarEvent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content" };

        // ACT-1
        var content = await repository.LoadContentAsync<CalendarEvent>(request, CancellationToken.None);

        // ASSERT-1
        Assert.IsNull(content.CreatedBy);
        Assert.IsNull(content.ModifiedBy);
        Assert.IsNull(content.Workspace);
        Assert.IsNotNull(content.Owner);
    }

    /* ====================================================================== Single property tests */

    [TestMethod]
    public async Task GeneralProps_T_SingleProperty_Load_()
    {
        await PropertyAfterLoadTest<Folder>("", content => { Assert.IsNull(content.Workspace); });
        await PropertyAfterLoadTest<Folder>("", content => { Assert.IsNull(content.Index); });

        await PropertyAfterLoadTest<Folder>(
            propertyJson: @"""CreatedBy"": {""__deferred"": {""uri"": ""/odata.svc/Root/Content/Events('Event-1')/CreatedBy""}}",
            assertAfterLoad: content => { Assert.IsNull(content.CreatedBy); });

        await PropertyAfterLoadTest<Folder>(
            propertyJson: @"""Owner"": {""Type"": ""User"",""Name"": ""Admin""}",
            assertAfterLoad: content =>
            {
                Assert.IsNotNull(content.Owner);
                Assert.IsNotNull("User", content.Owner.Type);
                Assert.IsNotNull("Admin", content.Owner.Name);
            });
    }

    private async Task PropertyAfterLoadTest<TContent>(string propertyJson, Action<TContent> assertAfterLoad) where TContent : Content
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 9125,
    ""Type"": """ + typeof(TContent).Name + @""",
    ""Name"": ""Content-1"",
    ""Path"": ""/Root/Content/Content-1"",
    " + propertyJson + @"
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TContent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content/Content-1" };

        // ACT-1
        var content = await repository.LoadContentAsync<TContent>(request, CancellationToken.None);

        // ASSERT-1
        assertAfterLoad(content);
    }


    [TestMethod]
    public async Task GeneralProps_T_SingleProperty_Save_()
    {
        await PropertyAfterPatchTest<Folder>(
            propertyLoadJson: "", 
            setProperty: content => { content.Index = 42; content.DisplayName = "Content 1"; },
            assertSaveRequest: (savedPropertyNames, properties) =>
            {
                Assert.AreEqual("DisplayName, Index", savedPropertyNames);
                Assert.AreEqual("42", properties["Index"].ToString());
                Assert.AreEqual("Content 1", properties["DisplayName"].ToString());
            });
    }
    private async Task PropertyAfterPatchTest<TContent>(string propertyLoadJson,
        Action<TContent> setProperty,
        Action<string, Dictionary<string, object>> assertSaveRequest) where TContent : Content
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 9125,
    ""Type"": """ + typeof(TContent).Name + @""",
    ""Name"": ""Content-1"",
    ""Path"": ""/Root/Content/Content-1"",
    " + propertyLoadJson + @"
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TContent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);
        var request = new LoadContentRequest { Path = "/Root/Content/Content-1" };

        var content = await repository.LoadContentAsync<TContent>(request, CancellationToken.None);

        setProperty(content);

        // ACT
        await content.SaveAsync(_cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(3, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[2].GetMethodInfo().Name);
        var arguments = calls[2].GetArguments();
        var json = (string)arguments[2]!;
        json = json.Substring("models=[".Length).TrimEnd(']');
        var dict = JsonHelper.Deserialize<Dictionary<string, object>>(json);
        var keys = string.Join(", ", dict.Keys);

        assertSaveRequest(keys, dict);
    }

}