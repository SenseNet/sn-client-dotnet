using AngleSharp.Io;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class InvokeOperationTests : TestBase
{
    private CancellationToken _cancel = new CancellationTokenSource().Token;

    [TestMethod]
    public async Task InvokeOperation_T_Function_Int()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"42");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 2, OperationName = "FakeOperation" };
        var response = await repository.InvokeFunctionAsync<int>(request, _cancel);

        // ASSERT
        Assert.AreEqual(typeof(int), response.GetType());
        Assert.AreEqual(42, response);
    }
    [TestMethod]
    public async Task InvokeOperation_T_Action_Int()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"42");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 2, OperationName = "FakeOperation" };
        var response = await repository.InvokeActionAsync<int>(request, _cancel);

        // ASSERT
        Assert.AreEqual(typeof(int), response.GetType());
        Assert.AreEqual(42, response);
    }

    private class CustomResponse { public string Name { get; set; } public int Value { get; set; } }
    [TestMethod]
    public async Task InvokeOperation_T_Function_CustomResponse()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"[
  {""Name"": ""Name1"", ""Value"": 42},
  {""Name"": ""Name2"", ""Value"": 142},
]");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 2, OperationName = "FakeOperation" };
        var response = await repository.InvokeFunctionAsync<CustomResponse[]>(request, _cancel);

        // ASSERT
        Assert.IsNotNull(response);
        Assert.AreEqual(typeof(CustomResponse[]), response.GetType());
        Assert.AreEqual("Name1", response[0].Name);
        Assert.AreEqual(42, response[0].Value);
        Assert.AreEqual("Name2", response[1].Name);
        Assert.AreEqual(142, response[1].Value);
    }
    [TestMethod]
    public async Task InvokeOperation_T_Action_CustomResponse()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"[
  {""Name"": ""Name1"", ""Value"": 42},
  {""Name"": ""Name2"", ""Value"": 142},
]");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 2, OperationName = "FakeOperation" };
        var response = await repository.InvokeActionAsync<CustomResponse[]>(request, _cancel);

        // ASSERT
        Assert.IsNotNull(response);
        Assert.AreEqual(typeof(CustomResponse[]), response.GetType());
        Assert.AreEqual("Name1", response[0].Name);
        Assert.AreEqual(42, response[0].Value);
        Assert.AreEqual("Name2", response[1].Name);
        Assert.AreEqual(142, response[1].Value);
    }
    [TestMethod]
    public async Task InvokeOperation_T_Function_Content_Error()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"{
  ""d"": {
    ""Id"": 999543,
    ""VersioningMode"": [ ""1"" ],
    ""InheritableVersioningMode"": [ ""2"" ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        Exception exception = null;
        var request = new OperationRequest { ContentId = 2, OperationName = "FakeOperation" };
        try
        {
            var response = await repository.InvokeFunctionAsync<Content>(request, _cancel);
            Assert.Fail("Expected ApplicationException was not thrown.");
        }
        catch (Exception e)
        {
            exception = e;
        }

        // ASSERT
        Assert.AreEqual(typeof(ApplicationException), exception.GetType());
        Assert.AreEqual("The ProcessOperationResponse cannot be called with type parameter SenseNet.Client.Content. " +
                        "If the type parameter is Content or any inherited type, call the InvokeContentFunctionAsync<T> " +
                        "or InvokeContentCollectionFunctionAsync<T> method.", exception.Message);
    }
    [TestMethod]
    public async Task InvokeOperation_T_Action_Content_Error()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"{
  ""d"": {
    ""Id"": 999543,
    ""VersioningMode"": [ ""1"" ],
    ""InheritableVersioningMode"": [ ""2"" ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        Exception exception = null;
        var request = new OperationRequest { ContentId = 2, OperationName = "FakeOperation" };
        try
        {
            var response = await repository.InvokeActionAsync<Content>(request, _cancel);
            Assert.Fail("Expected ApplicationException was not thrown.");
        }
        catch (Exception e)
        {
            exception = e;
        }

        // ASSERT
        Assert.AreEqual(typeof(ApplicationException), exception.GetType());
        Assert.AreEqual("The ProcessOperationResponse cannot be called with type parameter SenseNet.Client.Content. " +
                        "If the type parameter is Content or any inherited type, call the InvokeContentActionAsync<T> " +
                        "or InvokeContentCollectionActionAsync<T> method.", exception.Message);
    }

    [TestMethod]
    public async Task InvokeOperation_T_ContentFunction_User()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"{
  ""d"": {
    ""Name"": ""Admin"",
    ""Path"": ""/Root/IMS/BuiltIn/Portal/Admin"",
    ""Id"": 1,
    ""Type"": ""User""
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<User>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 2, OperationName = "GetCurrentUser" };
        var user = await repository.InvokeContentFunctionAsync<User>(request, _cancel);

        // ASSERT
        Assert.IsNotNull(user);
        Assert.AreEqual(typeof(User), user.GetType());
        Assert.AreEqual(1, user.Id);
        Assert.AreEqual("/Root/IMS/BuiltIn/Portal/Admin", user.Path);
        Assert.AreEqual("Admin", user.Name);
    }
    [TestMethod]
    public async Task InvokeOperation_T_ContentAction_User()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"{
  ""d"": {
    ""Name"": ""Admin"",
    ""Path"": ""/Root/IMS/BuiltIn/Portal/Admin"",
    ""Id"": 1,
    ""Type"": ""User""
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<User>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 2, OperationName = "GetCurrentUser" };
        var user = await repository.InvokeContentActionAsync<User>(request, _cancel);

        // ASSERT
        Assert.IsNotNull(user);
        Assert.AreEqual(typeof(User), user.GetType());
        Assert.AreEqual(1, user.Id);
        Assert.AreEqual("/Root/IMS/BuiltIn/Portal/Admin", user.Path);
        Assert.AreEqual("Admin", user.Name);
    }
    [TestMethod]
    public async Task InvokeOperation_T_ContentFunction_ContentButUser()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"{
  ""d"": {
    ""Name"": ""Admin"",
    ""Path"": ""/Root/IMS/BuiltIn/Portal/Admin"",
    ""Id"": 1,
    ""Type"": ""User""
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<User>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 2, OperationName = "GetCurrentUser" };
        var user = await repository.InvokeContentFunctionAsync<Content>(request, _cancel);

        // ASSERT
        Assert.IsNotNull(user);
        Assert.AreEqual(typeof(User), user.GetType());
        Assert.AreEqual(1, user.Id);
        Assert.AreEqual("/Root/IMS/BuiltIn/Portal/Admin", user.Path);
        Assert.AreEqual("Admin", user.Name);
    }
    [TestMethod]
    public async Task InvokeOperation_T_ContentAction_ContentButUser()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"{
  ""d"": {
    ""Name"": ""Admin"",
    ""Path"": ""/Root/IMS/BuiltIn/Portal/Admin"",
    ""Id"": 1,
    ""Type"": ""User""
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<User>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 2, OperationName = "GetCurrentUser" };
        var user = await repository.InvokeContentActionAsync<Content>(request, _cancel);

        // ASSERT
        Assert.IsNotNull(user);
        Assert.AreEqual(typeof(User), user.GetType());
        Assert.AreEqual(1, user.Id);
        Assert.AreEqual("/Root/IMS/BuiltIn/Portal/Admin", user.Path);
        Assert.AreEqual("Admin", user.Name);
    }


    private class OrgUnit : Content { public OrgUnit(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { } }
    private class Domain : Content { public Domain(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { } }
    private class Domains : Content { public Domains(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { } }
    private class PortalRoot : Content { public PortalRoot(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { } }
    [TestMethod]
    public async Task InvokeOperation_T_ContentCollectionFunction_Content()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"{
  ""d"": {
    ""__count"": 4,
    ""results"": [
      { ""Name"": ""Portal"",  ""Path"": ""/Root/IMS/BuiltIn/Portal"", ""Id"": 5, ""Type"": ""OrgUnit"" },
      { ""Name"": ""BuiltIn"", ""Path"": ""/Root/IMS/BuiltIn"",        ""Id"": 4, ""Type"": ""Domain"" },
      { ""Name"": ""IMS"",     ""Path"": ""/Root/IMS"",                ""Id"": 3, ""Type"": ""Domains"" },
      { ""Name"": ""Root"",    ""Path"": ""/Root"",                    ""Id"": 2, ""Type"": ""PortalRoot"" }
    ]
  }
}
");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<User>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest {ContentId = 1, OperationName = "Ancestors"};
        var contents = await repository.InvokeContentCollectionFunctionAsync<Content>(request, _cancel);

        // ASSERT
        Assert.IsNotNull(contents);
        var names = string.Join("|", contents.Select(x => $"{x.Name}:{x["Type"]}"));
        Assert.AreEqual("Portal:OrgUnit|BuiltIn:Domain|IMS:Domains|Root:PortalRoot", names);
    }
    [TestMethod]
    public async Task InvokeOperation_T_ContentCollectionAction_Content()
    {
        // ALIGN
        var restCaller = CreateRestCallerForProcessWebRequestResponse(@"{
  ""d"": {
    ""__count"": 4,
    ""results"": [
      { ""Name"": ""Portal"",  ""Path"": ""/Root/IMS/BuiltIn/Portal"", ""Id"": 5, ""Type"": ""OrgUnit"" },
      { ""Name"": ""BuiltIn"", ""Path"": ""/Root/IMS/BuiltIn"",        ""Id"": 4, ""Type"": ""Domain"" },
      { ""Name"": ""IMS"",     ""Path"": ""/Root/IMS"",                ""Id"": 3, ""Type"": ""Domains"" },
      { ""Name"": ""Root"",    ""Path"": ""/Root"",                    ""Id"": 2, ""Type"": ""PortalRoot"" }
    ]
  }
}
");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<User>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
            .ConfigureAwait(false);

        // ACT
        var request = new OperationRequest { ContentId = 1, OperationName = "Ancestors" };
        var contents = await repository.InvokeContentCollectionActionAsync<Content>(request, _cancel);

        // ASSERT
        Assert.IsNotNull(contents);
        var names = string.Join("|", contents.Select(x => $"{x.Name}:{x["Type"]}"));
        Assert.AreEqual("Portal:OrgUnit|BuiltIn:Domain|IMS:Domains|Root:PortalRoot", names);
    }

}