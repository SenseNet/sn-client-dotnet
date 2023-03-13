﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class ContentSavingTests
{
    public class TestContent_A : Content
    {
        public TestContent_A(IRestCaller restCaller, ILogger<TestContent_A> logger) : base(restCaller, logger) { }
        public int Index { get; set; }
    }

    [TestMethod]
    public async Task Content_T_SaveFirst_Dynamic_AddProperty()
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .PostContentAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        dynamic content = repository.CreateContent("/Root/Content", "Folder", "MyContent-1");
        content.Index = 9999;
        await content.SaveAsync().ConfigureAwait(false);

        // ASSERT
        var arguments = restCaller.ReceivedCalls().Single().GetArguments();
        Assert.AreEqual("/Root/Content", arguments[0]); // parentPath
        dynamic data = arguments[1]!;
        var fields = (IDictionary<string, object?>)data;

        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("__ContentType, Existing, Index, Name", names);
        Assert.IsNotNull(data);
        Assert.AreEqual("Folder", data.__ContentType);
        Assert.AreEqual("MyContent-1", data.Name);
        Assert.AreEqual(false, data.Existing);
        Assert.AreEqual(9999, data.Index);
    }
    [TestMethod]
    public async Task Content_T_SaveFirst_Custom_SetProperty()
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .PostContentAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.RegisterGlobalContentType<TestContent_A>();
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var content = repository.CreateContent<TestContent_A>("/Root/Content", null, "MyContent-1");
        content.Index = 9998;
        await content.SaveAsync().ConfigureAwait(false);

        // ASSERT
        var arguments = restCaller.ReceivedCalls().Single().GetArguments();
        Assert.AreEqual("/Root/Content", arguments[0]); // parentPath
        dynamic data = arguments[1]!;
        var fields = (IDictionary<string, object?>) data;

        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("__ContentType, Existing, Index, Name", names);
        Assert.IsNotNull(data);
        Assert.AreEqual("TestContent_A", data.__ContentType);
        Assert.AreEqual("MyContent-1", data.Name);
        Assert.AreEqual(false, data.Existing);
        Assert.AreEqual(9998, (int)data.Index);
    }
    [TestMethod]
    public async Task Content_T_SaveFirst_Custom_AddAndSetProperty()
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .PostContentAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.RegisterGlobalContentType<TestContent_A>();
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        dynamic content = repository.CreateContent<TestContent_A>("/Root/Content", null, "MyContent-1");
        content.Index = 9997;
        content.Index2 = 9996;
        await content.SaveAsync().ConfigureAwait(false);

        // ASSERT
        var arguments = restCaller.ReceivedCalls().Single().GetArguments();
        Assert.AreEqual("/Root/Content", arguments[0]); // parentPath
        dynamic data = arguments[1]!;
        var fields = (IDictionary<string, object?>)data;

        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("__ContentType, Existing, Index, Index2, Name", names);
        Assert.IsNotNull(data);
        Assert.AreEqual("TestContent_A", data.__ContentType);
        Assert.AreEqual("MyContent-1", data.Name);
        Assert.AreEqual(false, data.Existing);
        Assert.AreEqual(9997, (int)data.Index);
        Assert.AreEqual(9996, (int)data.Index2);
    }

    [TestMethod]
    public async Task Content_T_BaseType_Update_1()
    {
        var fields = await UpdateBaseTypeTest(content =>
        {
            content.Index = 42;
        });

        // ASSERT-2
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    [TestMethod]
    public async Task Content_T_BaseType_Update_2()
    {
        var fields = await UpdateBaseTypeTest(content =>
        {
            content.Index = 42;
        });

        // ASSERT-2
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    [TestMethod]
    public async Task Content_T_BaseType_Update_3()
    {
        var fields = await UpdateBaseTypeTest(content =>
        {
            Assert.AreEqual("99", content.Index.ToString()); // Force read property
            content["Index2"] = 43;
        });

        // ASSERT-2
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index2, Name", names);
        Assert.AreEqual(43, fields["Index2"]);
    }
    [TestMethod]
    public async Task Content_T_BaseType_Update_4()
    {
        var fields = await UpdateBaseTypeTest(content =>
        {
            content.Index = 42;
            content["Index2"] = 43;
        });

        // ASSERT-2
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Index2, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    private async Task<IDictionary<string, object>> UpdateBaseTypeTest(Action<dynamic> setProperties)
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""MyContent"",
    ""Path"": ""/Root/MyContent"",
    ""Type"": ""Folder"",
    ""Index"": 99
  }
}"));

        restCaller
            .PatchContentAsync(Arg.Any<int>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT-1: Load
        var request = new LoadContentRequest { ContentId = 999543, Select = new[] { "Id", "Name", "Path", "Type", "Index" } };
        dynamic content = await repository.LoadContentAsync(request, CancellationToken.None);

        // ASSERT-1
        var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/content(999543)?metadata=no&$select=Id,Name,Path,Type,Index", requestedUri.PathAndQuery);

        Assert.IsNotNull(content);
        Assert.AreEqual("Folder", ((JValue)content.Type).Value<string>());
        Assert.AreEqual("MyContent", ((JValue)content.Name).Value<string>());
        Assert.AreEqual(99, ((JValue)content.Index).Value<int>());

        // ACT-2: Update a property and save
        setProperties(content);
        await content.SaveAsync();

        // ASSERT-2
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(2, calls.Length);
        Assert.AreEqual("PatchContentAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        Assert.AreEqual(999543, arguments[0]);
        dynamic data = arguments[1]!;
        return data;
    }

    [TestMethod]
    public async Task Content_T_StrongType_UpdateAsDictionary()
    {
        var fields = await UpdateStrongTypeTest<Content>(content =>
        {
            content["Index"] = 42;
        });

        // ASSERT-2
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    [TestMethod]
    public async Task Content_T_StrongType_UpdateAsWellKnownType()
    {
        var fields = await UpdateStrongTypeTest<TestContent_A>(content =>
        {
            content.Index = 42;
        });

        // ASSERT-2
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    //UNDONE: Red test: expectation: strong property is not saved if not changed
    //[TestMethod]
    //public async Task Content_T_StrongType_UpdateUnknownProperty()
    //{
    //    var fields = await UpdateStrongTypeTest<TestContent_A>(content =>
    //    {
    //        content["Index2"] = 43;
    //    });

    //    // ASSERT (Strong property is not saved if not changed)
    //    var names = string.Join(", ", fields.Keys.OrderBy(x => x));
    //    Assert.AreEqual("Index2, Name", names);
    //    Assert.AreEqual(43, fields["Index2"]);
    //}
    [TestMethod]
    public async Task Content_T_StrongType_UpdateMixed()
    {
        var fields = await UpdateStrongTypeTest<TestContent_A>(content =>
        {
            content.Index = 42;
            content["Index2"] = 43;
        });

        // ASSERT
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Index2, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    private async Task<IDictionary<string, object>> UpdateStrongTypeTest<T>(Action<T> setProperties) where T : Content
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""MyContent"",
    ""Path"": ""/Root/MyContent"",
    ""Type"": ""Folder"",
    ""Index"": 99
  }
}"));

        restCaller
            .PatchContentAsync(Arg.Any<int>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.RegisterGlobalContentType<T>();
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT-1: Load
        var request = new LoadContentRequest { ContentId = 999543, Select = new[] { "Id", "Name", "Path", "Type", "Index" } };
        dynamic content = await repository.LoadContentAsync<T>(request, CancellationToken.None);

        // ASSERT-1
        var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/content(999543)?metadata=no&$select=Id,Name,Path,Type,Index", requestedUri.PathAndQuery);

        Assert.IsNotNull(content);
        Assert.AreEqual("Folder", ((JValue)content.Type).Value<string>());
        Assert.AreEqual("MyContent", ((JValue)content.Name).Value<string>());
        Assert.AreEqual(99, ((JValue)content.Index).Value<int>());

        // ACT-2: Update a property and save
        setProperties(content);
        await content.SaveAsync();

        // ASSERT-2
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(2, calls.Length);
        Assert.AreEqual("PatchContentAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        Assert.AreEqual(999543, arguments[0]);
        dynamic data = arguments[1]!;
        return data;
    }

    /* ====================================================================== TOOLS */

    private static IRepositoryCollection GetRepositoryCollection(Action<IServiceCollection>? addServices = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        var config = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json", optional: true)
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