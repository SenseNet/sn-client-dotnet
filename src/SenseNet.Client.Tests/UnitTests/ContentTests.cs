using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Extensions.DependencyInjection;
using static SenseNet.Client.Tests.UnitTests.RepositoryTests;
using Microsoft.Extensions.Configuration;
using AngleSharp.Dom;
using System.Net.Mime;
using AngleSharp.Io;
using Microsoft.IdentityModel.Tokens;
using NSubstitute.Core;
using SenseNet.Diagnostics;
using SenseNet.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class ContentTests
{
    /* =================================================================== GENERAL PROPERTIES */

    private class TestGenericContent : Content
    {
        public TestGenericContent(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public int Id { get; set; }
        public int ParentId { get; set; }
        public int OwnerId { get; set; }
        public Content Owner { get; set; }
        public int VersionId { get; set; }
        public string Type { get; set; } // NodeType
        //public string TypeIs { get; set; } // NodeType
        public string Icon { get; set; } // ShortText
        public string Name { get; set; } // ShortText
        public int CreatedById { get; set; }
        public int ModifiedById { get; set; }
        public string Version { get; set; } // Version
        public string Path { get; set; } // ShortText
        public int Depth { get; set; }
        //public string InTree { get; set; } // ShortText
        //public string InFolder { get; set; } // ShortText
        public bool IsSystemContent { get; set; }
        public bool IsFolder { get; set; }
        public string DisplayName { get; set; } // ShortText
        public string Description { get; set; } // RichText
        public bool? Hidden { get; set; }
        public int Index { get; set; }
        public bool? EnableLifespan { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTill { get; set; }
        public Content[] AllowedChildTypes { get; set; }
        public Content[] EffectiveAllowedChildTypes { get; set; }
        public string VersioningMode { get; set; } // VersioningMode
        public string InheritableVersioningMode { get; set; } // InheritableVersioningMode
        public Content CreatedBy { get; set; }
        public Content VersionCreatedBy { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? VersionCreationDate { get; set; }
        public Content ModifiedBy { get; set; }
        public Content VersionModifiedBy { get; set; }
        public DateTime? ModificationDate { get; set; }
        public DateTime? VersionModificationDate { get; set; }
        public string ApprovingMode { get; set; } // ApprovingMode
        public string InheritableApprovingMode { get; set; } // InheritableApprovingMode
        public bool? Locked { get; set; }
        public Content CheckedOutTo { get; set; }
        public bool? TrashDisabled { get; set; }
        public string SavingState { get; set; } // Choice
        public string ExtensionData { get; set; } // LongText
        public Content BrowseApplication { get; set; }
        public bool? Approvable { get; set; }
        public bool? IsTaggable { get; set; }
        public string Tags { get; set; } // LongText
        public bool? IsRateable { get; set; }
        public string RateStr { get; set; } // ShortText
        public string RateAvg { get; set; } // Number
        public int RateCount { get; set; }
        public string Rate { get; set; } // Rating
        public bool? Publishable { get; set; }
        public Content[] Versions { get; set; }
        public string CheckInComments { get; set; } // LongText
        public string RejectReason { get; set; } // LongText
        public Content Workspace { get; set; }
        public string BrowseUrl { get; set; } // ShortText
        public string Sharing { get; set; } // Sharing
        public string SharedWith { get; set; } // Sharing
        public string SharedBy { get; set; } // Sharing
        public string SharingMode { get; set; } // Sharing
        public string SharingLevel { get; set; } // Sharing
    }
    private class TestFolder : TestGenericContent
    {
        public TestFolder(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
        
        public string PreviewEnabled { get; set; } // Choice
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
    public async Task Content_T_Properties_General_ByRealRequest()
    {
        // ALIGN
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{
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
}"));

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestGenericContent>();
            services.RegisterGlobalContentType<TestFolder>();
            services.RegisterGlobalContentType<TestWorkspace>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
        };
        var content = await repository.LoadContentAsync<TestWorkspace>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First()!;
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
        Assert.AreEqual("0", content.VersioningMode); // VersioningMode
        Assert.AreEqual("0", content.InheritableVersioningMode); // InheritableVersioningMode
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
        //Assert.AreEqual(null, content.CheckedOutTo); // Reference
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

    private class TestContent_MultiChoice_StringToString : Content
    {
        public TestContent_MultiChoice_StringToString(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public string[] MultiChoice_Null { get; set; }
        public string[] MultiChoice_Empty { get; set; }
        public string[] MultiChoice_1 { get; set; }
        public string[] MultiChoice_2 { get; set; }
    }
    [TestMethod]
    public async Task Content_T_Properties_MultiChoice_StringToString()
    {
        // ALIGN
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{
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
}"));

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_MultiChoice_StringToString>();
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
        };
        var content = await repository.LoadContentAsync<TestContent_MultiChoice_StringToString>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First()!;
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
    public async Task Content_T_Properties_MultiChoice_StringToInt()
    {
        // ALIGN
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{
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
}"));

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<TestContent_MultiChoice_StringToInt>();
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
        };
        var content = await repository.LoadContentAsync<TestContent_MultiChoice_StringToInt>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First()!;
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

    /* =================================================================== REFERENCES */

    private class TestContentForReferences : Content
    {
        public TestContentForReferences(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        [ReferenceField]
        public MyContent? Manager { get; set; }
        [ReferenceField]
        public MyContent2? Owner { get; set; }
        [ReferenceField]
        public IEnumerable<MyContent3>? AllowedChildTypes { get; set; }
    }

    [TestMethod]
    public async Task Content_T_References_Deferred()
    {
        // ALIGN
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{
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
}"));

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
            services.RegisterGlobalContentType<MyContent2>();
            services.RegisterGlobalContentType<MyContent3>();
            services.RegisterGlobalContentType<TestContentForReferences>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        // ACT
        var request = new LoadContentRequest()
        {
            Path = "/Root/Content",
            Select = new[] { "Name", "Type", "Owner", "Manager", "AllowedChildTypes" }
        };
        var content = await repository.LoadContentAsync<TestContentForReferences>(request, CancellationToken.None);

        // ASSERT
        var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$select=Name,Type,Owner,Manager,AllowedChildTypes", requestedUri.PathAndQuery);

        Assert.IsNull(content.Manager);
        Assert.IsNull(content.Owner);
        Assert.IsNull(content.AllowedChildTypes);
    }

    [TestMethod]
    public async Task Content_T_References_Expanded_Simple()
    {
        // ALIGN
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"{ ""d"": { ""Name"": ""Content"", ""Type"": ""Workspace"",
    // single reference
    ""Owner"": { ""Path"": ""/Root/IMS/BuiltIn/Portal/Admin"" },
    // null reference
    ""Manager"": null
  }
}"));

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<MyContent>();
            services.RegisterGlobalContentType<MyContent2>();
            services.RegisterGlobalContentType<MyContent3>();
            services.RegisterGlobalContentType<TestContentForReferences>("Workspace");
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
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
        var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First()!;
        Assert.IsNotNull(requestedUri);
        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$expand=Owner,Manager&$select=Name,Type,Owner/Path,Manager/Path", requestedUri.PathAndQuery);

        Assert.IsNull(content.Manager);
        Assert.IsNotNull(content.Owner);
    }
//    [TestMethod]
//    public async Task Content_T_References_Expanded_Multi()
//    {
//        // ALIGN
//        var restCaller = Substitute.For<IRestCaller>();
//        restCaller
//            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<ServerContext>(), Arg.Any<CancellationToken>())
//            .Returns(Task.FromResult(@"{ ""d"": { ""Name"": ""Content"", ""Type"": ""Workspace"",
//    // multi reference
//    ""AllowedChildTypes"": [
//      { ""Path"": ""/Root/System/Schema/ContentTypes/GenericContent/Folder"" },
//      { ""Path"": ""/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder"" },
//      { ""Path"": ""/Root/System/Schema/ContentTypes/GenericContent/File"" }
//    ]
//  }
//}"));

//        var repositories = GetRepositoryCollection(services =>
//        {
//            services.AddSingleton(restCaller);
//            services.RegisterGlobalContentType<MyContent>();
//            services.RegisterGlobalContentType<MyContent2>();
//            services.RegisterGlobalContentType<MyContent3>();
//            services.RegisterGlobalContentType<TestContentForReferences>("Workspace");
//        });
//        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
//            .ConfigureAwait(false);

//        // ACT
//        var request = new LoadContentRequest()
//        {
//            Path = "/Root/Content",
//            Expand = new[] { "Owner", "Manager", "AllowedChildTypes" },
//            Select = new[] { "Name", "Type", "AllowedChildTypes/Path" }
//        };
//        var content = await repository.LoadContentAsync<TestContentForReferences>(request, CancellationToken.None);

//        // ASSERT
//        var requestedUri = (Uri)restCaller.ReceivedCalls().Single().GetArguments().First()!;
//        Assert.IsNotNull(requestedUri);
//        Assert.AreEqual("/OData.svc/Root('Content')?metadata=no&$expand=AllowedChildTypes&$select=Name,Type,AllowedChildTypes/Path", requestedUri.PathAndQuery);

//        Assert.IsNull(content.Manager);
//        Assert.IsNotNull(content.Owner);
//        Assert.IsNotNull(content.AllowedChildTypes);
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
