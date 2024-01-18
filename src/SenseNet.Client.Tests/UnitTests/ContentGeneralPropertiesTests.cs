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
        Assert.AreEqual("Name, PreviewEnabled", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
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
        Assert.AreEqual("Name, PreviewEnabled", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
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
        Assert.AreEqual("Name, Priority", keys);
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
        Assert.AreEqual("Name, Priority", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
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
        Assert.AreEqual("Name, Priority", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
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
    ""Status"": [ ""Pending"" ]
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
        Assert.AreEqual("Name, Status", keys);
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
    ""Status"": [ ""1"" ]
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
        Assert.AreEqual("Name, Status", keys);
        Assert.AreEqual("Content1", data.Name.ToString());
        Assert.AreEqual("[\"Deferred\"]", RemoveWhitespaces(data.Status.ToString()));
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
        Assert.AreEqual("Name, MemoType", keys);
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
        Assert.AreEqual("Name, MemoType", keys);
        var value = dict["MemoType"];
        Assert.IsNotNull(value);
        var valueAsJArray = value as JArray;
        Assert.IsNotNull(valueAsJArray);
        Assert.AreEqual("iso", valueAsJArray.FirstOrDefault());
    }
}