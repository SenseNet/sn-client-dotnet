using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Extensions.DependencyInjection;
using static SenseNet.Client.Tests.UnitTests.RepositoryTests;
using Newtonsoft.Json.Linq;
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class ContentLoadingTests : TestBase
{
    /* =================================================================== GENERAL PROPERTIES */

    private class TestGenericContent : Content
    {
        public TestGenericContent(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        // --- Implemented in base class
        //public int Id { get; set; }
        //public int ParentId { get; set; }
        //public string Name { get; set; }
        //public string Path { get; set; }
        // --- Irrelevant readonly properties
        //public string TypeIs { get; set; }
        //public string InTree { get; set; }
        //public string InFolder { get; set; }
        // ---
        public int OwnerId { get; set; }
        public Content Owner { get; set; }
        public int VersionId { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
        public int CreatedById { get; set; }
        public int ModifiedById { get; set; }
        public string Version { get; set; }
        public int Depth { get; set; }
        public bool IsSystemContent { get; set; }
        public bool IsFolder { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool? Hidden { get; set; }
        public int Index { get; set; }
        public bool? EnableLifespan { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTill { get; set; }
        public Content[] AllowedChildTypes { get; set; }
        public Content[] EffectiveAllowedChildTypes { get; set; }
        //public string VersioningMode { get; set; }
        //public string InheritableVersioningMode { get; set; }
        public Content CreatedBy { get; set; }
        public Content VersionCreatedBy { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? VersionCreationDate { get; set; }
        public Content ModifiedBy { get; set; }
        public Content VersionModifiedBy { get; set; }
        public DateTime? ModificationDate { get; set; }
        public DateTime? VersionModificationDate { get; set; }
        public string ApprovingMode { get; set; }
        public string InheritableApprovingMode { get; set; }
        public bool? Locked { get; set; }
        public Content CheckedOutTo { get; set; }
        public bool? TrashDisabled { get; set; }
        public string SavingState { get; set; }
        public string ExtensionData { get; set; }
        public Content BrowseApplication { get; set; }
        public bool? Approvable { get; set; }
        public bool? IsTaggable { get; set; }
        public string Tags { get; set; }
        public bool? IsRateable { get; set; }
        public string RateStr { get; set; }
        public string RateAvg { get; set; }
        public int RateCount { get; set; }
        public string Rate { get; set; }
        public bool? Publishable { get; set; }
        public Content[] Versions { get; set; }
        public string CheckInComments { get; set; }
        public string RejectReason { get; set; }
        public Content Workspace { get; set; }
        public string BrowseUrl { get; set; }
        public string Sharing { get; set; }
        public string SharedWith { get; set; }
        public string SharedBy { get; set; }
        public string SharingMode { get; set; }
        public string SharingLevel { get; set; }
    }
    private class TestFolder : TestGenericContent
    {
        public TestFolder(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
        
        public string PreviewEnabled { get; set; }
    }
    private class TestWorkspace : TestFolder
    {
        public TestWorkspace(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
        
        public Content Manager { get; set; }
        public DateTime? Deadline { get; set; }
        public bool? IsActive { get; set; }
        public Content WorkspaceSkin { get; set; }
        public bool? IsCritical { get; set; }
        public bool? IsWallContainer { get; set; }
        public bool? IsFollowed { get; set; }
    }
    [TestMethod]
    public async Task LoadContent_T_Properties_General_ByRealRequest()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Name"": ""Content"",
    ""DisplayName"": ""Content"",
    ""Description"": null,
    ""Manager"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/Manager""
      }
    },
    ""Deadline"": ""0001-01-01T00:00:00"",
    ""IsActive"": true,
    ""WorkspaceSkin"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/WorkspaceSkin""
      }
    },
    ""IsCritical"": false,
    ""IsWallContainer"": false,
    ""IsFollowed"": false,
    ""AllowedChildTypes"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/AllowedChildTypes""
      }
    },
    ""InheritableVersioningMode"": [
      ""0""
    ],
    ""InheritableApprovingMode"": [
      ""0""
    ],
    ""Path"": ""/Root/Content"",
    ""Version"": ""V1.0.A"",
    ""TrashDisabled"": false,
    ""PreviewEnabled"": [
      ""0""
    ],
    ""Id"": 1368,
    ""ParentId"": 2,
    ""OwnerId"": 1,
    ""Owner"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/Owner""
      }
    },
    ""VersionId"": 381,
    ""Type"": ""Workspace"",
    ""TypeIs"": null,
    ""Icon"": ""Workspace"",
    ""CreatedById"": 1,
    ""ModifiedById"": 1,
    ""Depth"": 1,
    ""InTree"": null,
    ""InFolder"": null,
    ""IsSystemContent"": false,
    ""IsFolder"": true,
    ""Hidden"": false,
    ""Index"": 0,
    ""EnableLifespan"": false,
    ""ValidFrom"": ""2022-12-15T01:54:20Z"",
    ""ValidTill"": ""2022-12-15T01:54:20Z"",
    ""EffectiveAllowedChildTypes"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/EffectiveAllowedChildTypes""
      }
    },
    ""VersioningMode"": [
      ""0""
    ],
    ""CreatedBy"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/CreatedBy""
      }
    },
    ""VersionCreatedBy"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/VersionCreatedBy""
      }
    },
    ""CreationDate"": ""2022-12-15T00:54:20.64Z"",
    ""VersionCreationDate"": ""2023-03-08T13:58:24.2076366Z"",
    ""ModifiedBy"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/ModifiedBy""
      }
    },
    ""VersionModifiedBy"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/VersionModifiedBy""
      }
    },
    ""ModificationDate"": ""2023-03-08T13:58:24.2076017Z"",
    ""VersionModificationDate"": ""2023-03-08T13:58:24.2076369Z"",
    ""ApprovingMode"": [
      ""0""
    ],
    ""Locked"": false,
    ""CheckedOutTo"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/CheckedOutTo""
      }
    },
    ""SavingState"": [
      ""0""
    ],
    ""ExtensionData"": null,
    ""BrowseApplication"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/BrowseApplication""
      }
    },
    ""Approvable"": false,
    ""IsTaggable"": false,
    ""Tags"": null,
    ""IsRateable"": null,
    ""RateStr"": null,
    ""RateAvg"": null,
    ""RateCount"": null,
    ""Rate"": null,
    ""Publishable"": false,
    ""Versions"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/Versions""
      }
    },
    ""CheckInComments"": null,
    ""RejectReason"": null,
    ""Workspace"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/Workspace""
      }
    },
    ""BrowseUrl"": """",
    ""Sharing"": """",
    ""SharedWith"": """",
    ""SharedBy"": """",
    ""SharingMode"": """",
    ""SharingLevel"": """",
    ""Actions"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/Actions""
      }
    },
    ""IsFile"": false,
    ""Children"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/Children""
      }
    }
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestGenericContent>();
            services.RegisterGlobalContentType<TestFolder>();
            services.RegisterGlobalContentType<TestWorkspace>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
        };
        var content = await repository.LoadContentAsync<TestWorkspace>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no", requestedUri.PathAndQuery);

        Assert.AreEqual(1368, content.Id);
        Assert.AreEqual(2, content.ParentId);
        Assert.AreEqual(1, content.OwnerId);
        Assert.IsNull(content.Owner);
        Assert.AreEqual(381, content.VersionId);
        Assert.AreEqual("Workspace", content.Type); // NodeType
        //Assert.AreEqual("____", content.TypeIs); // NodeType
        Assert.AreEqual("Workspace", content.Icon);  // ShortText
        Assert.AreEqual("Content", content.Name);  // ShortText
        Assert.AreEqual(1, content.CreatedById); // Integer
        Assert.AreEqual(1, content.ModifiedById); // Integer
        Assert.AreEqual("V1.0.A", content.Version); // Version
        Assert.AreEqual("/Root/Content", content.Path); // ShortText
        Assert.AreEqual(1, content.Depth); // Integer
        //Assert.AreEqual("____", content.InTree); // ShortText
        //Assert.AreEqual("____", content.InFolder); // ShortText
        Assert.AreEqual(false, content.IsSystemContent); // Boolean
        Assert.AreEqual(true, content.IsFolder); // Boolean
        Assert.AreEqual("Content", content.DisplayName); // ShortText
        Assert.AreEqual(null, content.Description); // RichText
        Assert.AreEqual(false, content.Hidden); // Boolean
        Assert.AreEqual(0, content.Index); // Integer
        Assert.AreEqual(false, content.EnableLifespan); // Boolean
        Assert.AreEqual(DateTime.Parse("2022-12-15T01:54:20Z").ToUniversalTime(), content.ValidFrom); // DateTime
        Assert.AreEqual(DateTime.Parse("2022-12-15T01:54:20Z").ToUniversalTime(), content.ValidTill); // DateTime
        Assert.AreEqual(null, content.AllowedChildTypes); // AllowedChildTypes
        Assert.AreEqual(null, content.EffectiveAllowedChildTypes); // AllowedChildTypes
        Assert.AreEqual(VersioningMode.Inherited, content.VersioningMode); // VersioningMode
        Assert.AreEqual(VersioningMode.Inherited, content.InheritableVersioningMode); // InheritableVersioningMode
        Assert.AreEqual(null, content.CreatedBy); // Reference
        Assert.AreEqual(null, content.VersionCreatedBy); // Reference
        Assert.AreEqual(DateTime.Parse("2022-12-15T00:54:20.64Z").ToUniversalTime(), content.CreationDate); // DateTime
        Assert.AreEqual(DateTime.Parse("2023-03-08T13:58:24.2076366Z").ToUniversalTime(), content.VersionCreationDate); // DateTime
        Assert.AreEqual(null, content.ModifiedBy); // Reference
        Assert.AreEqual(null, content.VersionModifiedBy); // Reference
        Assert.AreEqual(DateTime.Parse("2023-03-08T13:58:24.2076017Z").ToUniversalTime(), content.ModificationDate); // DateTime
        Assert.AreEqual(DateTime.Parse("2023-03-08T13:58:24.2076369Z").ToUniversalTime(), content.VersionModificationDate); // DateTime
        Assert.AreEqual("0", content.ApprovingMode); // ApprovingMode
        Assert.AreEqual("0", content.InheritableApprovingMode); // InheritableApprovingMode
        Assert.AreEqual(false, content.Locked); // Boolean
        Assert.AreEqual(null, content.CheckedOutTo); // Reference
        Assert.AreEqual(false, content.TrashDisabled); // Boolean
        Assert.AreEqual("0", content.SavingState); // Choice
        Assert.AreEqual(null, content.ExtensionData); // LongText
        Assert.AreEqual(null, content.BrowseApplication); // Reference
        Assert.AreEqual(false, content.Approvable); // Boolean
        Assert.AreEqual(false, content.IsTaggable); // Boolean
        Assert.AreEqual(null, content.Tags); // LongText
        Assert.AreEqual(null, content.IsRateable); // Boolean
        Assert.AreEqual(null, content.RateStr); // ShortText
        Assert.AreEqual(null, content.RateAvg); // Number
        Assert.AreEqual(0, content.RateCount); // Integer
        Assert.AreEqual(null, content.Rate); // Rating
        Assert.AreEqual(false, content.Publishable); // Boolean
        Assert.AreEqual(null, content.Versions); // Reference
        Assert.AreEqual(null, content.CheckInComments); // LongText
        Assert.AreEqual(null, content.RejectReason); // LongText
        Assert.AreEqual(null, content.Workspace); // Reference
        Assert.AreEqual("", content.BrowseUrl); // ShortText
        Assert.AreEqual("", content.Sharing); // Sharing
        Assert.AreEqual("", content.SharedWith); // Sharing
        Assert.AreEqual("", content.SharedBy); // Sharing
        Assert.AreEqual("", content.SharingMode); // Sharing
        Assert.AreEqual("", content.SharingLevel); // Sharing

        Assert.AreEqual("0", content.PreviewEnabled); // Choice

        Assert.AreEqual(null, content.Manager); // Reference
        Assert.AreEqual(DateTime.Parse("0001-01-01T00:00:00").ToUniversalTime(), content.Deadline); // DateTime
        Assert.AreEqual(true, content.IsActive); // Boolean
        Assert.AreEqual(null, content.WorkspaceSkin); // Reference
        Assert.AreEqual(false, content.IsCritical); // Boolean
        Assert.AreEqual(false, content.IsWallContainer); // Boolean
        Assert.AreEqual(false, content.IsFollowed); // Boolean
    }
    [TestMethod]
    public async Task LoadContent_T_Properties_General_ByRealRequest_Projected()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 1368,
    ""Name"": ""Content"",
    ""Type"": ""Workspace""
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestGenericContent>();
            services.RegisterGlobalContentType<TestFolder>();
            services.RegisterGlobalContentType<TestWorkspace>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content", Select = new[] { "Id", "Name", "Type" }
        };
        var content = await repository.LoadContentAsync<TestWorkspace>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$select=Id,Name,Type", requestedUri.PathAndQuery);

        Assert.AreEqual(1368, content.Id);
        Assert.AreEqual(0, content.ParentId);
        Assert.AreEqual(0, content.OwnerId);
        Assert.IsNull(content.Owner);
        Assert.AreEqual(0, content.VersionId);
        Assert.AreEqual("Workspace", content.Type); // NodeType
        //Assert.AreEqual("____", content.TypeIs); // NodeType
        Assert.AreEqual(null, content.Icon);  // ShortText
        Assert.AreEqual("Content", content.Name);  // ShortText
        Assert.AreEqual(0, content.CreatedById); // Integer
        Assert.AreEqual(0, content.ModifiedById); // Integer
        Assert.AreEqual(null, content.Version); // Version
        Assert.AreEqual(null, content.Path); // ShortText
        Assert.AreEqual(0, content.Depth); // Integer
        //Assert.AreEqual("____", content.InTree); // ShortText
        //Assert.AreEqual("____", content.InFolder); // ShortText
        Assert.AreEqual(false, content.IsSystemContent); // Boolean
        Assert.AreEqual(false, content.IsFolder); // Boolean
        Assert.AreEqual(null, content.DisplayName); // ShortText
        Assert.AreEqual(null, content.Description); // RichText
        Assert.AreEqual(null, content.Hidden); // Boolean
        Assert.AreEqual(0, content.Index); // Integer
        Assert.AreEqual(null, content.EnableLifespan); // Boolean
        Assert.AreEqual(DateTime.Parse("0001-01-01T00:00:00Z").ToUniversalTime(), content.ValidFrom); // DateTime
        Assert.AreEqual(null, content.ValidTill); // DateTime
        Assert.AreEqual(null, content.AllowedChildTypes); // AllowedChildTypes
        Assert.AreEqual(null, content.EffectiveAllowedChildTypes); // AllowedChildTypes
        Assert.AreEqual(null, content.VersioningMode); // VersioningMode
        Assert.AreEqual(null, content.InheritableVersioningMode); // InheritableVersioningMode
        Assert.AreEqual(null, content.CreatedBy); // Reference
        Assert.AreEqual(null, content.VersionCreatedBy); // Reference
        Assert.AreEqual(null, content.CreationDate); // DateTime
        Assert.AreEqual(null, content.VersionCreationDate); // DateTime
        Assert.AreEqual(null, content.ModifiedBy); // Reference
        Assert.AreEqual(null, content.VersionModifiedBy); // Reference
        Assert.AreEqual(null, content.ModificationDate); // DateTime
        Assert.AreEqual(null, content.VersionModificationDate); // DateTime
        Assert.AreEqual(null, content.ApprovingMode); // ApprovingMode
        Assert.AreEqual(null, content.InheritableApprovingMode); // InheritableApprovingMode
        Assert.AreEqual(null, content.Locked); // Boolean
        Assert.AreEqual(null, content.CheckedOutTo); // Reference
        Assert.AreEqual(null, content.TrashDisabled); // Boolean
        Assert.AreEqual(null, content.SavingState); // Choice
        Assert.AreEqual(null, content.ExtensionData); // LongText
        Assert.AreEqual(null, content.BrowseApplication); // Reference
        Assert.AreEqual(null, content.Approvable); // Boolean
        Assert.AreEqual(null, content.IsTaggable); // Boolean
        Assert.AreEqual(null, content.Tags); // LongText
        Assert.AreEqual(null, content.IsRateable); // Boolean
        Assert.AreEqual(null, content.RateStr); // ShortText
        Assert.AreEqual(null, content.RateAvg); // Number
        Assert.AreEqual(0, content.RateCount); // Integer
        Assert.AreEqual(null, content.Rate); // Rating
        Assert.AreEqual(null, content.Publishable); // Boolean
        Assert.AreEqual(null, content.Versions); // Reference
        Assert.AreEqual(null, content.CheckInComments); // LongText
        Assert.AreEqual(null, content.RejectReason); // LongText
        Assert.AreEqual(null, content.Workspace); // Reference
        Assert.AreEqual(null, content.BrowseUrl); // ShortText
        Assert.AreEqual(null, content.Sharing); // Sharing
        Assert.AreEqual(null, content.SharedWith); // Sharing
        Assert.AreEqual(null, content.SharedBy); // Sharing
        Assert.AreEqual(null, content.SharingMode); // Sharing
        Assert.AreEqual(null, content.SharingLevel); // Sharing

        Assert.AreEqual(null, content.PreviewEnabled); // Choice

        Assert.AreEqual(null, content.Manager); // Reference
        Assert.AreEqual(null, content.Deadline); // DateTime
        Assert.AreEqual(null, content.IsActive); // Boolean
        Assert.AreEqual(null, content.WorkspaceSkin); // Reference
        Assert.AreEqual(null, content.IsCritical); // Boolean
        Assert.AreEqual(null, content.IsWallContainer); // Boolean
        Assert.AreEqual(null, content.IsFollowed); // Boolean
    }

    private class TestContent_RelevantProperties : Content
    {
        public TestContent_RelevantProperties(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public static string Static { get; set; }
        public string ReadOnly { get; }
        internal string Internal { get; set; }
        public string PublicInstanceReadWrite { get; set; }
    }
    [TestMethod]
    public async Task LoadContent_T_OnlyPublicInstanceReadWritePropertiesAreBoundButNoErrors()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 9998987,
    ""Name"": ""Content-1"",
    ""Static"": ""Value1"",
    ""ReadOnly"": ""Value2"",
    ""Internal"": ""Value3"",
    ""PublicInstanceReadWrite"": ""Value4"",
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_RelevantProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest {ContentId = 9998987};
        var content = await repository.LoadContentAsync<TestContent_RelevantProperties>(request, CancellationToken.None);

        // ASSERT
        Assert.IsNull(TestContent_RelevantProperties.Static);
        Assert.IsNull(content.Internal);
        Assert.IsNull(content.ReadOnly);
        Assert.AreEqual("Value4", content.PublicInstanceReadWrite);
    }

    private class TestContent_MultiChoice_StringToString : Content
    {
        public TestContent_MultiChoice_StringToString(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public string[] MultiChoice_Null { get; set; }
        public string[] MultiChoice_Empty { get; set; }
        public string[] MultiChoice_1 { get; set; }
        public string[] MultiChoice_2 { get; set; }
    }
    [TestMethod]
    public async Task LoadContent_T_Properties_MultiChoice_StringToString()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Name"": ""Content"",
    ""MultiChoice_Null"": null,
    ""MultiChoice_Empty"": [],
    ""MultiChoice_1"": [
      ""0""
    ],
    ""MultiChoice_2"": [
      ""2"",
      ""42"",
    ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_MultiChoice_StringToString>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
        };
        var content = await repository.LoadContentAsync<TestContent_MultiChoice_StringToString>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no", requestedUri.PathAndQuery);

        Assert.AreEqual(null, content.MultiChoice_Null);
        Assert.IsNotNull(content.MultiChoice_Empty);
        Assert.AreEqual(0, content.MultiChoice_Empty.Length);
        Assert.IsNotNull(content.MultiChoice_1);
        Assert.AreEqual(1, content.MultiChoice_1.Length);
        Assert.AreEqual("0", content.MultiChoice_1[0]);
        Assert.IsNotNull(content.MultiChoice_2);
        Assert.AreEqual(2, content.MultiChoice_2.Length);
        Assert.AreEqual("2", content.MultiChoice_2[0]);
        Assert.AreEqual("42", content.MultiChoice_2[1]);
    }

    private class TestContent_MultiChoice_StringToInt : Content
    {
        public TestContent_MultiChoice_StringToInt(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public int[] MultiChoice_Null { get; set; }
        public int[] MultiChoice_Empty { get; set; }
        public int[] MultiChoice_1 { get; set; }
        public int[] MultiChoice_2 { get; set; }
    }
    [TestMethod]
    public async Task LoadContent_T_Properties_MultiChoice_StringToInt()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Name"": ""Content"",
    ""MultiChoice_Null"": null,
    ""MultiChoice_Empty"": [],
    ""MultiChoice_1"": [
      ""0""
    ],
    ""MultiChoice_2"": [
      ""2"",
      ""42"",
    ],
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_MultiChoice_StringToInt>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
        };
        var content = await repository.LoadContentAsync<TestContent_MultiChoice_StringToInt>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no", requestedUri.PathAndQuery);

        Assert.AreEqual(null, content.MultiChoice_Null);
        Assert.IsNotNull(content.MultiChoice_Empty);
        Assert.AreEqual(0, content.MultiChoice_Empty.Length);
        Assert.IsNotNull(content.MultiChoice_1);
        Assert.AreEqual(1, content.MultiChoice_1.Length);
        Assert.AreEqual(0, content.MultiChoice_1[0]);
        Assert.IsNotNull(content.MultiChoice_2);
        Assert.AreEqual(2, content.MultiChoice_2.Length);
        Assert.AreEqual(2, content.MultiChoice_2[0]);
        Assert.AreEqual(42, content.MultiChoice_2[1]);
    }

    private class TestContent_Number : Content
    {
        public TestContent_Number(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public long Number_Long_Null { get; set; }
        public long Number_Long_Null_WithDefault { get; set; } = 42L;
        public long? Number_Long_Nullable_Null { get; set; }
        public long Number_Long { get; set; }
        public decimal Number_Decimal { get; set; }
        public decimal? Number_Decimal_Nullable { get; set; }
        public double Number_Double { get; set; }
        public double? Number_Double_Nullable { get; set; }
        public float Number_Single { get; set; }
        public float? Number_Single_Nullable { get; set; }
    }
    [TestMethod]
    public async Task LoadContent_T_Properties_Number()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Number_Long_Null"": null,
    ""Number_Long_Nullable_Null"": null,
    ""Number_Long_Null_WithDefault"": null,
    ""Number_Long"": 9223372036854775807,
    ""Number_Decimal"": 1234567.8987,
    ""Number_Decimal_Nullable"": null,
    ""Number_Double"": 1234567.8987,
    ""Number_Double_Nullable"": null,
    ""Number_Single"": 1234567.8987,
    ""Number_Single_Nullable"": null,
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_Number>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
        };
        var content = await repository.LoadContentAsync<TestContent_Number>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no", requestedUri.PathAndQuery);

        Assert.AreEqual(0L, content.Number_Long_Null);
        Assert.AreEqual(42L, content.Number_Long_Null_WithDefault);
        Assert.AreEqual(null, content.Number_Long_Nullable_Null);
        Assert.AreEqual(9223372036854775807L, content.Number_Long);
        Assert.AreEqual(1234567.8987m, content.Number_Decimal);
        Assert.AreEqual(null, content.Number_Decimal_Nullable);
        Assert.AreEqual(1234567.8987d, content.Number_Double);
        Assert.AreEqual(null, content.Number_Double_Nullable);
        Assert.AreEqual(1234567.8987f, content.Number_Single);
        Assert.AreEqual(null, content.Number_Single_Nullable);
    }

    private class TestContent_Binaries : Content
    {
        public TestContent_Binaries(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public Binary Binary { get; set; }
        public Binary Secondary { get; set; }
    }
    [TestMethod]
    public async Task LoadContent_T_Properties_Binary()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Binary"": {
      ""__mediaresource"": {
        ""edit_media"": null,
        ""media_src"": ""/binaryhandler.ashx?nodeid=999999&propertyname=Binary"",
        ""content_type"": ""image/png"",
        ""media_etag"": null
      }
    },
    ""Secondary"": {
      ""__mediaresource"": {
        ""edit_media"": null,
        ""media_src"": ""/binaryhandler.ashx?nodeid=999999&propertyname=Secondary"",
        ""content_type"": ""application/octet-stream"",
        ""media_etag"": null
      }
    }
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_Binaries>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
        };
        var content = await repository.LoadContentAsync<TestContent_Binaries>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no", requestedUri.PathAndQuery);

        Assert.AreEqual(null, content.Binary.EditMedia);
        Assert.AreEqual("/binaryhandler.ashx?nodeid=999999&propertyname=Binary", content.Binary.MediaSrc);
        Assert.AreEqual("image/png", content.Binary.ContentType);
        Assert.AreEqual(null, content.Binary.MediaEtag);
        Assert.AreEqual(null, content.Secondary.EditMedia);
        Assert.AreEqual("/binaryhandler.ashx?nodeid=999999&propertyname=Secondary", content.Secondary.MediaSrc);
        Assert.AreEqual("application/octet-stream", content.Secondary.ContentType);
        Assert.AreEqual(null, content.Secondary.MediaEtag);
    }

    /* =================================================================== REFERENCES */

    private class TestContentForReferences : Content
    {
        public TestContentForReferences(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public MyContent? Manager { get; set; }
        public MyContent2? Owner { get; set; }
        public IEnumerable<MyContent3>? AllowedChildTypes { get; set; }

        public MyContent3[]? Reference_Array { get; set; }
        public List<MyContent3>? Reference_List { get; set; }
    }

    [TestMethod]
    public async Task LoadContent_T_References_Deferred()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Name"": ""Content"",
    ""Type"": ""Workspace"",
    ""Owner"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/Owner""
      }
    },
    ""Manager"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/Manager""
      }
    },
    ""AllowedChildTypes"": {
      ""__deferred"": {
        ""uri"": ""/odata.svc/Root('Content')/AllowedChildTypes""
      }
    }
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
            services.RegisterGlobalContentType<MyContent2>();
            services.RegisterGlobalContentType<MyContent3>();
            services.RegisterGlobalContentType<TestContentForReferences>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
            Select = new[] { "Name", "Type", "Owner", "Manager", "AllowedChildTypes" }
        };
        var content = await repository.LoadContentAsync<TestContentForReferences>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$select=Name,Type,Owner,Manager,AllowedChildTypes", requestedUri.PathAndQuery);

        Assert.IsNull(content.Manager);
        Assert.IsNull(content.Owner);
        Assert.IsNull(content.AllowedChildTypes);
    }

    [TestMethod]
    public async Task LoadContent_T_References_Expanded_Simple()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"", ""Type"": ""Workspace"",
    // single reference
    ""Owner"": { ""Path"": ""/Root/IMS/BuiltIn/Portal/Admin"" },
    // null reference
    ""Manager"": null
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
            services.RegisterGlobalContentType<MyContent2>();
            services.RegisterGlobalContentType<MyContent3>();
            services.RegisterGlobalContentType<TestContentForReferences>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
            Expand = new[] { "Owner", "Manager" },
            Select = new[] { "Name", "Type", "Owner/Path", "Manager/Path" }
        };
        var content = await repository.LoadContentAsync<TestContentForReferences>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$expand=Owner,Manager&$select=Name,Type,Owner/Path,Manager/Path", requestedUri.PathAndQuery);

        Assert.IsNull(content.Manager);
        Assert.IsNotNull(content.Owner);
    }
    [TestMethod]
    public async Task LoadContent_T_References_Expanded_Multi_IEnumerable()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"", ""Type"": ""Workspace"",
    // multi reference
    ""AllowedChildTypes"": [
      { ""Path"": ""/Root/System/Schema/ContentTypes/GenericContent/Folder"" },
      { ""Path"": ""/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder"" },
      { ""Path"": ""/Root/System/Schema/ContentTypes/GenericContent/File"" }
    ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
            services.RegisterGlobalContentType<MyContent2>();
            services.RegisterGlobalContentType<MyContent3>();
            services.RegisterGlobalContentType<TestContentForReferences>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
            Expand = new[] { "AllowedChildTypes" },
            Select = new[] { "Name", "Type", "AllowedChildTypes/Path" }
        };
        var content = await repository.LoadContentAsync<TestContentForReferences>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$expand=AllowedChildTypes&$select=Name,Type,AllowedChildTypes/Path", requestedUri.PathAndQuery);

        Assert.IsNull(content.Manager);
        Assert.IsNull(content.Owner);
        Assert.IsNotNull(content.AllowedChildTypes);
        var referredItems = content.AllowedChildTypes.ToArray();
        Assert.IsTrue(referredItems.All(x => x is MyContent3));

        Assert.AreEqual("/Root/System/Schema/ContentTypes/GenericContent/Folder", referredItems[0].Path);
        Assert.AreEqual("/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder", referredItems[1].Path);
        Assert.AreEqual("/Root/System/Schema/ContentTypes/GenericContent/File", referredItems[2].Path);
    }
    [TestMethod]
    public async Task LoadContent_T_References_Expanded_Multi_Array()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"", ""Type"": ""Workspace"",
    // multi reference
    ""Reference_Array"": [
      { ""Path"": ""/Root/Path1"" },
      { ""Path"": ""/Root/Path2"" },
      { ""Path"": ""/Root/Path3"" }
    ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
            services.RegisterGlobalContentType<MyContent2>();
            services.RegisterGlobalContentType<MyContent3>();
            services.RegisterGlobalContentType<TestContentForReferences>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
            Expand = new[] { "AllowedChildTypes" },
            Select = new[] { "Name", "Type", "AllowedChildTypes/Path" }
        };
        var content = await repository.LoadContentAsync<TestContentForReferences>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$expand=AllowedChildTypes&$select=Name,Type,AllowedChildTypes/Path", requestedUri.PathAndQuery);

        Assert.IsNull(content.Manager);
        Assert.IsNull(content.Owner);
        Assert.IsNotNull(content.Reference_Array);
        var referredItems = content.Reference_Array;
        Assert.IsTrue(referredItems.All(x => x is MyContent3));
        Assert.AreEqual("/Root/Path1", referredItems[0].Path);
        Assert.AreEqual("/Root/Path2", referredItems[1].Path);
        Assert.AreEqual("/Root/Path3", referredItems[2].Path);
    }
    [TestMethod]
    public async Task LoadContent_T_References_Expanded_Multi_List()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{ ""d"": { ""Name"": ""Content"", ""Type"": ""Workspace"",
    // multi reference
    ""Reference_List"": [
      { ""Path"": ""/Root/Path1"" },
      { ""Path"": ""/Root/Path2"" },
      { ""Path"": ""/Root/Path3"" }
    ]
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
            services.RegisterGlobalContentType<MyContent2>();
            services.RegisterGlobalContentType<MyContent3>();
            services.RegisterGlobalContentType<TestContentForReferences>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
            Expand = new[] { "AllowedChildTypes" },
            Select = new[] { "Name", "Type", "AllowedChildTypes/Path" }
        };
        var content = await repository.LoadContentAsync<TestContentForReferences>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$expand=AllowedChildTypes&$select=Name,Type,AllowedChildTypes/Path", requestedUri.PathAndQuery);

        Assert.IsNull(content.Manager);
        Assert.IsNull(content.Owner);
        Assert.IsNotNull(content.Reference_List);
        var referredItems = content.Reference_List;
        Assert.IsTrue(referredItems.All(x => x is MyContent3));

        Assert.AreEqual("/Root/Path1", referredItems[0].Path);
        Assert.AreEqual("/Root/Path2", referredItems[1].Path);
        Assert.AreEqual("/Root/Path3", referredItems[2].Path);
    }

    /* =================================================================== CUSTOM PROPERTIES */

    private class CustomType1
    {
        public string Property1 { get; set; }
        public int Property2 { get; set; }
    }
    private class TestContent_CustomProperties : Content
    {
        public TestContent_CustomProperties(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public CustomType1 Field_CustomType1 { get; set; }
        public CustomType1 Field_TypeMismatch { get; set; }
        public bool Field_StringToBool { get; set; }
        public Dictionary<string, int> Field_StringToDictionary { get; set; }

        protected override bool TryConvertToProperty(string propertyName, JToken jsonValue, out object propertyValue)
        {
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
    }
    [TestMethod]
    public async Task LoadContent_T_Properties_Custom()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Field_CustomType1"": {
      ""property1"": ""value1"",
      ""property2"": 42,
    },
    ""Field_TypeMismatch"": {
      ""property3"": ""value3"",
      ""property4"": 44,
    },
    ""Field_StringToBool"": ""1"",
    ""Field_StringToDictionary"": ""Name1:111,Name2:222,Name3:333""
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_CustomProperties>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest {Path = "/Root/Content"};
        var content = await repository.LoadContentAsync<TestContent_CustomProperties>(request, CancellationToken.None);

        // ASSERT
        // Field_CustomType1 is converted automatically
        Assert.IsNotNull(content.Field_CustomType1);
        Assert.AreEqual("value1", content.Field_CustomType1.Property1);
        Assert.AreEqual(42, content.Field_CustomType1.Property2);
        Assert.IsNotNull(content.Field_TypeMismatch);
        // Field_TypeMismatch is instantiated but not filled because the response data is not compatible with it.
        Assert.AreEqual(null, content.Field_TypeMismatch.Property1);
        Assert.AreEqual(0, content.Field_TypeMismatch.Property2);
        Assert.IsNotNull(content.Field_StringToDictionary);
        // Field_StringToBool is converted by the overridden ConvertToProperty method.
        Assert.AreEqual(true, content.Field_StringToBool);
        // Field_StringToDictionary is converted by the overridden ConvertToProperty method.
        Assert.AreEqual(3, content.Field_StringToDictionary.Count);
        Assert.AreEqual(111, content.Field_StringToDictionary["Name1"]);
        Assert.AreEqual(222, content.Field_StringToDictionary["Name2"]);
        Assert.AreEqual(333, content.Field_StringToDictionary["Name3"]);
    }

    private class TestContent_CustomProperties_WrongConversion : TestContent_CustomProperties
    {
        public TestContent_CustomProperties_WrongConversion(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
        protected override bool TryConvertToProperty(string propertyName, JToken jsonValue, out object propertyValue)
        {
            if (propertyName == nameof(Field_StringToBool))
            {
                propertyValue = "Returns a wrong value but bool expected.";
                return true;
            }
            return base.TryConvertToProperty(propertyName, jsonValue, out propertyValue);
        }
    }
    [TestMethod]
    public async Task LoadContent_T_Properties_Error_TypeMismatch()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Field_CustomType1"": {
      ""property1"": ""value1"",
      ""property2"": 42,
    },
    ""Field_TypeMismatch"": {
      ""property3"": ""value3"",
      ""property4"": 44,
    },
    ""Field_StringToBool"": ""1"",
    ""Field_StringToDictionary"": ""Name1:111,Name2:222,Name3:333""
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_CustomProperties_WrongConversion>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        var request = new LoadContentRequest { Path = "/Root/Content" };
        try
        {
            // ACT
            var _ = await repository.LoadContentAsync<TestContent_CustomProperties_WrongConversion>(request, CancellationToken.None);
            Assert.Fail("The expected ApplicationException was not thrown.");
        }
        catch (ApplicationException ex)
        {
            // ASSERT
            Assert.AreEqual("The property 'Field_StringToBool' cannot be set." +
                            " See inner exception for details.", ex.Message);
            Assert.AreEqual("Object of type 'System.String' " +
                            "cannot be converted to type 'System.Boolean'.", ex.InnerException?.Message);
        }
    }

    /* =================================================================== ERROR */

    [TestMethod]
    public async Task LoadContent_T_Error_UnknownType()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Type"": ""MyContent1"",
    ""Name"": ""Content-1""
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest {ContentId = 999543, Select = new []{ "Id", "Type", "Name" }};
        MyContent2? content = null;
        try
        {
            content = await repository.LoadContentAsync<MyContent2>(request, CancellationToken.None);
            Assert.Fail("The expected ApplicationException was not thrown.");
        }
        catch (ApplicationException ex)
        {
            // ASSERT
            Assert.AreEqual("The content type is not registered: MyContent2", ex.Message);
            Assert.AreEqual("No service for type 'SenseNet.Client.Tests.UnitTests.RepositoryTests+MyContent2' " +
                            "has been registered.", ex.InnerException?.Message);
        }

        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/content(999543)?metadata=no&$select=Id,Type,Name", requestedUri.PathAndQuery);
    }

    [TestMethod]
    public async Task LoadContent_T_Error_ErrorResponse()
    {
        // ALIGN
        var restCaller = CreateRestCallerFor(@"{
  ""error"": {
    ""code"": ""NotSpecified"",
    ""exceptiontype"": ""ParserException"",
    ""message"": {
      ""lang"": """",
      ""value"": ""Unknown field: Name1., blah blah...""
    },
    ""innererror"": null
  }
}
");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new QueryContentRequest
        {
            ContentQuery = "Name1:'admin*'",
            Select = new[] { "Path" },
            AutoFilters = FilterStatus.Disabled
        };
        try
        {
            var _ = await repository.QueryAsync(request, CancellationToken.None);
            Assert.Fail("The expected ClientException was not thrown.");
        }
        catch (ClientException ex)
        {
            // ASSERT
            Assert.AreEqual("Unknown field: Name1., blah blah...", ex.Message);
            Assert.IsNull(ex.InnerException);
        }

        var requestedUri = (Uri)restCaller.ReceivedCalls().ToArray()[1].GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root?metadata=no&$select=Path&enableautofilters=false&query=Name1%3A%27admin%2A%27", requestedUri.PathAndQuery);
    }
}
