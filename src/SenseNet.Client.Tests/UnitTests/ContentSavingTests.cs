using Microsoft.Extensions.Configuration;
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
    public async Task Content_T_StronglyTyped_UpdateAsDictionary()
    {
        var fields = await UpdateStronglyTypedTest<Content>(content =>
        {
            content["Index"] = 42;
        });

        // ASSERT-2
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    [TestMethod]
    public async Task Content_T_StronglyTyped_UpdateAsWellKnownType()
    {
        var fields = await UpdateStronglyTypedTest<TestContent_A>(content =>
        {
            content.Index = 42;
        });

        // ASSERT-2
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    //UNDONE: Inactivated red test: expectation: strong property is not saved if not changed
    //[TestMethod]
    //public async Task Content_T_StronglyTyped_UpdateUnknownProperty()
    //{
    //    var fields = await UpdateStronglyTypedTest<TestContent_A>(content =>
    //    {
    //        content["Index2"] = 43;
    //    });

    //    // ASSERT (Strong property is not saved if not changed)
    //    var names = string.Join(", ", fields.Keys.OrderBy(x => x));
    //    Assert.AreEqual("Index2, Name", names);
    //    Assert.AreEqual(43, fields["Index2"]);
    //}
    [TestMethod]
    public async Task Content_T_StronglyTyped_UpdateMixed()
    {
        var fields = await UpdateStronglyTypedTest<TestContent_A>(content =>
        {
            content.Index = 42;
            content["Index2"] = 43;
        });

        // ASSERT
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index, Index2, Name", names);
        Assert.AreEqual(42, fields["Index"]);
    }
    private async Task<IDictionary<string, object>> UpdateStronglyTypedTest<T>(Action<T> setProperties) where T : Content
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

    /* =================================================================== CUSTOM PROPERTIES */

    #region Nested classes: CustomType1, TestContent_CustomProperties
    private class CustomType1
    {
        public string Property1 { get; set; }
        public int Property2 { get; set; }
    }
    private class TestContent_CustomProperties : Content
    {
        public TestContent_CustomProperties(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public CustomType1 Field_CustomType1 { get; set; }
        public bool Field_StringToBool { get; set; }
        public Dictionary<string, int> Field_StringToDictionary { get; set; }

        protected override bool TryConvertToProperty(string propertyName, JToken jsonValue, out object propertyValue)
        {
            if (jsonValue == null)
            {
                propertyValue = null;
                return false;
            }
            if (propertyName == nameof(Field_StringToBool))
            {
                var stringValue = jsonValue.Value<string>();
                propertyValue = !string.IsNullOrEmpty(stringValue) && "0" != stringValue;
                return true;
            }
            if (propertyName == nameof(Field_StringToDictionary))
            {
                var stringValue = jsonValue.Value<string>();
                if (stringValue != null)
                {
                    propertyValue = new Dictionary<string, int>(stringValue.Split(',').Select(x =>
                    {
                        var split = x.Split(':');
                        var name = split[0].Trim();
                        var value = int.Parse(split[1]);
                        return new KeyValuePair<string, int>(name, value);
                    }));
                    return true;
                }
            }
            return base.TryConvertToProperty(propertyName, jsonValue, out propertyValue);
        }

        protected override bool TryConvertFromProperty(string propertyName, out object convertedValue)
        {
            if (propertyName == nameof(Field_StringToBool))
            {
                convertedValue = Field_StringToBool ? 1 : 0;
                return true;
            }
            if (propertyName == nameof(Field_StringToDictionary))
            {
                convertedValue = string.Join(",", Field_StringToDictionary
                    // Ordering is needed for tests
                    .OrderBy(x => x.Key)
                    .Select(x => $"{x.Key}:{x.Value}"));
                return true;
            }
            return base.TryConvertFromProperty(propertyName, out convertedValue);
        }
    }
    #endregion
    [TestMethod]
    public async Task Content_T_StronglyTyped_UpdateCustomProperties()
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{
  ""d"": {
    ""Id"": 999543,
    ""Field_CustomType1"": {
      ""property1"": ""value1"",
      ""property2"": 42,
    },
    ""Field_StringToBool"": ""0"",
    ""Field_StringToDictionary"": ""Name1:111,Name2:222,Name3:333""
  }
}"));

        restCaller
            .PatchContentAsync(Arg.Any<int>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.RegisterGlobalContentType<TestContent_CustomProperties>();
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT-1 Load
        var request = new LoadContentRequest { Path = "/Root/Content" };
        var content = await repository.LoadContentAsync<TestContent_CustomProperties>(request, CancellationToken.None);

        // ASSERT-1 Property original values
        // Field_CustomType1 is converted automatically
        Assert.IsNotNull(content.Field_CustomType1);
        Assert.AreEqual("value1", content.Field_CustomType1.Property1);
        Assert.AreEqual(42, content.Field_CustomType1.Property2);
        Assert.IsNotNull(content.Field_StringToDictionary);
        // Field_StringToBool is converted by the overridden ConvertToProperty method.
        Assert.AreEqual(false, content.Field_StringToBool);
        // Field_StringToDictionary is converted by the overridden ConvertToProperty method.
        Assert.AreEqual(3, content.Field_StringToDictionary.Count);
        Assert.AreEqual(111, content.Field_StringToDictionary["Name1"]);
        Assert.AreEqual(222, content.Field_StringToDictionary["Name2"]);
        Assert.AreEqual(333, content.Field_StringToDictionary["Name3"]);

        // ACT-2 Modify properties and save
        content.Field_CustomType1.Property1 = "updated";
        content.Field_CustomType1.Property2 = 442;
        content.Field_StringToBool = true;
        content.Field_StringToDictionary["Name1"] = 11111;
        content.Field_StringToDictionary.Remove("Name2");
        content.Field_StringToDictionary["Name4"] = 444;
        await content.SaveAsync();

        // ASSERT-2
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(2, calls.Length);
        Assert.AreEqual("PatchContentAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        Assert.AreEqual(999543, arguments[0]);
        dynamic data = arguments[1]!;
        Assert.AreEqual("updated", data.Field_CustomType1.Property1);
        Assert.AreEqual(442, data.Field_CustomType1.Property2);
        Assert.AreEqual(1, data.Field_StringToBool);
        Assert.AreEqual(typeof(string), data.Field_StringToDictionary.GetType());
        Assert.AreEqual("Name1:11111,Name3:333,Name4:444", data.Field_StringToDictionary);
    }
    //UNDONE: Inactivated red test: expectation: strong property is not saved if not changed
//    [TestMethod]
//    public async Task Content_T_StronglyTyped_UpdateCustomProperties_OnlyChanged()
//    {
//        var restCaller = Substitute.For<IRestCaller>();
//        restCaller
//            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
//            .Returns(Task.FromResult(@"{
//  ""d"": {
//    ""Id"": 999543,
//    ""Field_CustomType1"": {
//      ""property1"": ""value1"",
//      ""property2"": 42,
//    },
//    ""Field_StringToBool"": ""0"",
//    ""Field_StringToDictionary"": ""Name1:111,Name2:222,Name3:333""
//  }
//}"));

//        restCaller
//            .PatchContentAsync(Arg.Any<int>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
//                Arg.Any<CancellationToken>())
//            .Returns(new Content(null, null));

//        var repositories = GetRepositoryCollection(services =>
//        {
//            services.RegisterGlobalContentType<TestContent_CustomProperties>();
//            services.AddSingleton(restCaller);
//        });
//        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
//            .ConfigureAwait(false);

//        // ACT-1 Load
//        var request = new LoadContentRequest { Path = "/Root/Content" };
//        var content = await repository.LoadContentAsync<TestContent_CustomProperties>(request, CancellationToken.None);

//        // ASSERT-1 Property original values
//        // Field_CustomType1 is converted automatically
//        Assert.IsNotNull(content.Field_CustomType1);
//        Assert.AreEqual("value1", content.Field_CustomType1.Property1);
//        Assert.AreEqual(42, content.Field_CustomType1.Property2);
//        Assert.IsNotNull(content.Field_StringToDictionary);
//        // Field_StringToBool is converted by the overridden ConvertToProperty method.
//        Assert.AreEqual(false, content.Field_StringToBool);
//        // Field_StringToDictionary is converted by the overridden ConvertToProperty method.
//        Assert.AreEqual(3, content.Field_StringToDictionary.Count);
//        Assert.AreEqual(111, content.Field_StringToDictionary["Name1"]);
//        Assert.AreEqual(222, content.Field_StringToDictionary["Name2"]);
//        Assert.AreEqual(333, content.Field_StringToDictionary["Name3"]);

//        // ACT-2 Modify properties and save
//        content["Index2"] = 99999;
//        await content.SaveAsync();

//        // ASSERT-2
//        var calls = restCaller.ReceivedCalls().ToArray();
//        Assert.IsNotNull(calls);
//        Assert.AreEqual(2, calls.Length);
//        Assert.AreEqual("PatchContentAsync", calls[1].GetMethodInfo().Name);
//        var arguments = calls[1].GetArguments();
//        Assert.AreEqual(999543, arguments[0]);
//        var data = (IDictionary<string, object>) arguments[1]!;
//        var names = string.Join(", ", data.Keys.OrderBy(x => x));
//        Assert.AreEqual("Index2, Name", names);
//    }

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