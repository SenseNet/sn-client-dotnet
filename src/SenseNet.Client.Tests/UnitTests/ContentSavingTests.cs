using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using SenseNet.Extensions.DependencyInjection;
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local

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
    [TestMethod]
    public async Task Content_T_StronglyTyped_UpdateUnknownProperty()
    {
        var fields = await UpdateStronglyTypedTest<TestContent_A>(content =>
        {
            content["Index2"] = 43;
        });

        // ASSERT (Strong property is not saved if not changed)
        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Index2, Name", names);
        Assert.AreEqual(43, fields["Index2"]);
    }
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
        [JsonProperty(PropertyName = "property1")]
        public string Property1 { get; set; }
        [JsonProperty(PropertyName = "property2")]
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
    [TestMethod]
    public async Task Content_T_StronglyTyped_UpdateCustomProperties_OnlyChanged()
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
        content["Index2"] = 99999;
        await content.SaveAsync();

        // ASSERT-2
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(2, calls.Length);
        Assert.AreEqual("PatchContentAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        Assert.AreEqual(999543, arguments[0]);
        var data = (IDictionary<string, object>)arguments[1]!;
        var names = string.Join(", ", data.Keys.OrderBy(x => x));
        Assert.AreEqual("Index2, Name", names);
    }

    private class ReferredContent : Content
    {
        public ReferredContent(IRestCaller restCaller, ILogger<ReferredContent> logger) : base(restCaller, logger) { }
    }
    private class TestContent_References : Content
    {
        public TestContent_References(IRestCaller restCaller, ILogger<TestContent_References> logger) : base(restCaller, logger) { }

        public Content Reference_Content { get; set; }
        public Content[] References_ContentArray { get; set; }
        public IEnumerable<Content> References_ContentEnumerable { get; set; }
        public List<Content> References_ContentList { get; set; }
        public ReferredContent Reference_WellKnown { get; set; }
        public ReferredContent[] References_WellKnownArray { get; set; }
        public IEnumerable<ReferredContent> References_WellKnownEnumerable { get; set; }
        public List<ReferredContent> References_WellKnownList { get; set; }
    }
    [TestMethod]
    public async Task Content_T_StronglyTyped_References_SaveFirst()
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .PostContentAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.RegisterGlobalContentType<ReferredContent>();
            services.RegisterGlobalContentType<TestContent_References>();
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        var contents = new Content[7];
        for (int i = 0; i < contents.Length; i++)
        {
            contents[i] = new Content(null, null)
            {
                Id = i % 2 == 0 ? 100001 + i : 0,
                Name = $"Content-{i + 1}",
                Path = $"/Root/Refs/Content-{i + 1}",
                ParentId = 100000,
                ParentPath = "/Root/Refs",
                Repository = repository,
                Server = repository.Server
            };
        }
        var referredContents = new ReferredContent[7];
        for (int i = 0; i < contents.Length; i++)
        {
            referredContents[i] = new ReferredContent(null, null)
            {
                Id = i % 2 == 1 ? 200001 + i : 0,
                Name = $"ReferredContent-{i + 1}",
                Path = $"/Root/Refs/ReferredContent-{i + 1}",
                ParentId = 100000,
                ParentPath = "/Root/Refs",
                Repository = repository,
                Server = repository.Server
            };
        }

        // ACT
        var content = repository.CreateContent<TestContent_References>("/Root/Content", null, "MyContent-1");
        content.Reference_Content = contents[0];
        content.References_ContentArray = new[] { contents[1], contents[2] };
        content.References_ContentEnumerable = new[] { contents[3], contents[4] };
        content.References_ContentList = new List<Content> { contents[5], contents[6] };
        content.Reference_WellKnown = referredContents[0];
        content.References_WellKnownArray = new[] { referredContents[1], referredContents[2] };
        content.References_WellKnownEnumerable = new[] { referredContents[3], referredContents[4] };
        content.References_WellKnownList = new List<ReferredContent> { referredContents[5], referredContents[6] };
        await content.SaveAsync().ConfigureAwait(false);

        // ASSERT
        var arguments = restCaller.ReceivedCalls().Single().GetArguments();
        Assert.AreEqual("/Root/Content", arguments[0]); // parentPath
        dynamic data = arguments[1]!;
        var fields = (IDictionary<string, object?>)data;

        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("__ContentType, Existing, Name, Reference_Content, Reference_WellKnown," +
                        " References_ContentArray, References_ContentEnumerable, References_ContentList," +
                        " References_WellKnownArray, References_WellKnownEnumerable, References_WellKnownList", names);
        Assert.IsNotNull(data);
        Assert.AreEqual("{\"Name\":\"MyContent-1\",\"__ContentType\":\"TestContent_References\",\"Existing\":false," +
                        "\"Reference_Content\":[100001]," +
                        "\"References_ContentArray\":[\"/Root/Refs/Content-2\",100003]," +
                        "\"References_ContentEnumerable\":[\"/Root/Refs/Content-4\",100005]," +
                        "\"References_ContentList\":[\"/Root/Refs/Content-6\",100007]," +
                        "\"Reference_WellKnown\":[\"/Root/Refs/ReferredContent-1\"]," +
                        "\"References_WellKnownArray\":[200002,\"/Root/Refs/ReferredContent-3\"]," +
                        "\"References_WellKnownEnumerable\":[200004,\"/Root/Refs/ReferredContent-5\"]," +
                        "\"References_WellKnownList\":[200006,\"/Root/Refs/ReferredContent-7\"]}", JsonHelper.Serialize(data));
    }
    [TestMethod]
    public async Task Content_T_StronglyTyped_References_Update()
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{
  ""d"": {
    ""Id"": 899612,
    ""Reference_Content"": [{ ""Id"": 100001 }],
    ""References_ContentArray"": [{ ""Id"": 100002 },{ ""Id"": 100003 }],
    ""References_ContentEnumerable"": [{ ""Id"": 100004 },{ ""Id"": 100005 }],
    ""References_ContentList"": [{ ""Id"": 100006 },{ ""Id"": 100007 }],
    ""Reference_WellKnown"": [{ ""Id"": 200001 }],
    ""References_WellKnownArray"": [{ ""Id"": 200002 },{ ""Id"": 200003 }],
    ""References_WellKnownEnumerable"": [{ ""Id"": 200004 },{ ""Id"": 200005 }],
    ""References_WellKnownList"": [{ ""Id"": 200006 },{ ""Id"": 200007 }],
  }
}
"));
        restCaller
            .PatchContentAsync(Arg.Any<int>(), Arg.Any<object>(), Arg.Any<ServerContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new Content(null, null));

        var repositories = GetRepositoryCollection(services =>
        {
            services.RegisterGlobalContentType<ReferredContent>();
            services.RegisterGlobalContentType<TestContent_References>();
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        var contents = new Content[7];
        for (int i = 0; i < contents.Length; i++)
        {
            contents[i] = new Content(null, null)
            {
                Id = i % 2 == 0 ? 300001 + i : 0,
                Name = $"Content-{i + 1}",
                Path = $"/Root/Refs2/Content-{i + 1}",
                ParentId = 100000,
                ParentPath = "/Root/Refs2",
                Repository = repository,
                Server = repository.Server
            };
        }
        var referredContents = new ReferredContent[7];
        for (int i = 0; i < contents.Length; i++)
        {
            referredContents[i] = new ReferredContent(null, null)
            {
                Id = i % 2 == 1 ? 400001 + i : 0,
                Name = $"ReferredContent-{i + 1}",
                Path = $"/Root/Refs2/ReferredContent-{i + 1}",
                ParentId = 100000,
                ParentPath = "/Root/Refs2",
                Repository = repository,
                Server = repository.Server
            };
        }
        var request = new LoadContentRequest { ContentId = 999543, Select = new[] { "Id", "Name", "Path", "Type", "Index" } };
        dynamic content = await repository.LoadContentAsync<TestContent_References>(request, CancellationToken.None);

        // ACT
        content.Reference_Content = contents[0];
        content.References_ContentEnumerable = new[] { contents[3], contents[4] };
        content.References_WellKnownArray = new[] { referredContents[1], referredContents[2] };
        content.References_WellKnownList = new List<ReferredContent> { referredContents[5], referredContents[6] };
        await content.SaveAsync().ConfigureAwait(false);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(2, calls.Length);
        Assert.AreEqual("PatchContentAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        Assert.AreEqual(899612, arguments[0]);
        //Assert.AreEqual("/Root/Content", arguments[0]); // parentPath
        dynamic data = arguments[1]!;
        var fields = (IDictionary<string, object?>)data;

        var names = string.Join(", ", fields.Keys.OrderBy(x => x));
        Assert.AreEqual("Name, Reference_Content, References_ContentEnumerable, References_WellKnownArray, References_WellKnownList", names);
        Assert.IsNotNull(data);
        Assert.AreEqual("{\"Name\":null," +
                        "\"Reference_Content\":[300001]," +
                        "\"References_ContentEnumerable\":[\"/Root/Refs2/Content-4\",300005]," +
                        "\"References_WellKnownArray\":[400002,\"/Root/Refs2/ReferredContent-3\"]," +
                        "\"References_WellKnownList\":[400006,\"/Root/Refs2/ReferredContent-7\"]}", JsonHelper.Serialize(data));
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