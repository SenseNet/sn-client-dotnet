using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.TestsForDocs.Infrastructure;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class BasicConcepts : ClientIntegrationTestBase
    {
        /* ====================================================================================== Entry */

        [TestMethod]
        [Description("Get a single content by Id")]
        public async Task Docs_BasicConcepts_GetSingleContentById()
        {
            var content =
                // ACTION for doc
                await Content.LoadAsync(1284);

            // ASSERT
            Assert.IsNotNull(content);
            Assert.AreEqual(1284, content.Id);
        }
        [TestMethod]
        [Description("Get a single content by Path")]
        public async Task Docs_BasicConcepts_GetSingleContentByPath()
        {
            var content =
                // ACTION for doc
                await Content.LoadAsync("/Root/Content/IT");

            // ASSERT
            Assert.IsNotNull(content);
            Assert.AreEqual("/Root/Content/IT", content.Path);
        }
        [TestMethod]
        [Description("Addressing a single property of a content")]
        public async Task Docs_BasicConcepts_GetSingleProperty()
        {
            var response =
                // ACTION for doc
                //UNDONE:- Feature request: Content.GetPropertyAsync: await Content.GetPropertyAsync("/Root/IMS", "DisplayName");
                await RESTCaller.GetResponseStringAsync("/Root/Content/IT", "DisplayName");

            // ASSERT
            Assert.AreEqual("{\"d\":{\"DisplayName\":\"IT\"}}", response.RemoveWhitespaces());
        }
        [TestMethod]
        [Description("Addressing a property value")]
        public async Task Docs_BasicConcepts_GetSinglePropertyValue()
        {
            var url = ClientContext.Current.Server.Url;

            var response =
                // ACTION for doc
                //UNDONE:- Feature request: Content.GetPropertyValueAsync: await Content.GetPropertyValueAsync("/Root/IMS", "DisplayName");
                await RESTCaller.GetResponseStringAsync(new Uri(url + "/OData.svc/Root/Content/('IT')/DisplayName/$value"));

            // ASSERT
            Assert.AreEqual("IT", response.RemoveWhitespaces());
        }

        /* ====================================================================================== Collection */

        [TestMethod]
        [Description("Children of a content (collection)")]
        public async Task Docs_BasicConcepts_GetChildren()
        {
            var result = 
                // ACTION for doc
                await Content.LoadCollectionAsync("/Root/Content");

            // ASSERT
            var children = result.ToArray();
            Assert.IsTrue(children.Length > 0);
            Assert.AreEqual("/Root/Content", children[0].ParentPath);
        }
        [TestMethod]
        [Description("Count of a collection")]
        public async Task Docs_BasicConcepts_ChildrenCount()
        {
            var children =
                await Content.LoadCollectionAsync("/Root/Content");

            var count =
                // ACTION for doc
                await Content.GetCountAsync("/Root/Content", null);

            // ASSERT
            Assert.AreNotEqual(0, count);
            Assert.AreEqual(children.ToArray().Length, count);
        }
        [TestMethod]
        [Description("$inlinecount query option")]
        public async Task Docs_BasicConcepts_ChildrenInlineCount()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            //UNDONE:- Feature request: should returns a collection with property: TotalCount
            //var result3 = await Content.LoadCollectionAsync(new ODataRequest
            //{
            //    Path = "/Root/IMS/BuiltIn/Portal",
            //    Top = 3,
            //    Skip = 4,
            //    InlineCount = InlineCountOptions.AllPages // Default, AllPages, None
            //});

            // ACTION for doc
            var result = await RESTCaller.GetResponseJsonAsync(new ODataRequest
            {
                IsCollectionRequest = true,
                Path = "/Root/Content/IT",
                Top = 3,
                Skip = 4,
                Parameters = { { "$inlinecount", "allpages" } }
            });

            // ASSERT
            // { "d": { "__count": 1, "results": [] }}

            var array = ((JToken) result).SelectTokens("$.d.results.*").ToArray();
            var inlineCount = ((JToken)result).SelectToken("$.d.__count").Value<int>();

            Assert.AreNotEqual(0, inlineCount);
            Assert.AreNotEqual(array.Length, inlineCount);
        }

        /* ====================================================================================== Select and expand */

        [TestMethod]
        [Description("Select")]
        // GetContentAsync GetResponseStringAsync GetResponseJsonAsync
        public async Task Docs_BasicConcepts_Select()
        {
            // ACTION for doc
            dynamic content = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Select = new[] { "DisplayName", "Description" }
            });

            // ASSERT-1
            var responseString = await RESTCaller.GetResponseStringAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Select = new[] { "DisplayName", "Description" }
            });
            Assert.AreEqual("{\"d\":{\"DisplayName\":\"IT\",\"Description\":null}}", responseString.RemoveWhitespaces());

            // ASSERT-2
            var responseJson = await RESTCaller.GetResponseJsonAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Select = new[] { "DisplayName", "Description" }
            });
            var displayName = responseJson.d.DisplayName.ToString();
            var description = responseJson.d.Description.ToString();
            Assert.AreEqual(displayName, content.DisplayName.ToString());
            Assert.AreEqual(description, content.Description.ToString());
        }
        [TestMethod]
        [Description("Expand 1")]
        public async Task Docs_BasicConcepts_Expand_CreatedBy()
        {
            // ACTION for doc
            dynamic content = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "CreatedBy" },
            });
            //Console.WriteLine(content.CreatedBy.Name);

            // ASSERT
            Assert.AreEqual("Admin", content.CreatedBy.Name.ToString());
        }
        [TestMethod]
        [Description("Expand 2")]
        public async Task Docs_BasicConcepts_Expand_CreatedByCreatedBy()
        {
            // ACTION for doc
            dynamic content = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "CreatedBy/CreatedBy" },
            });
            //Console.WriteLine(content.CreatedBy.CreatedBy.Name);

            // ASSERT
            Assert.AreEqual("Admin", content.CreatedBy.CreatedBy.Name.ToString());
        }
        [TestMethod]
        [Description("Expand 3")]
        public async Task Docs_BasicConcepts_Expand_CreatedByName()
        {
            // ACTION for doc
            dynamic content = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "CreatedBy" },
                Select = new[] { "Name", "CreatedBy/Name" }
            });
            //Console.WriteLine(content.CreatedBy.Name);

            // ASSERT
            Assert.AreEqual("Admin", content.CreatedBy.Name.ToString());
        }
        [TestMethod]
        [Description("Expand 4")]
        public async Task Docs_BasicConcepts_Expand_AllowedChildTypes()
        {
            // ACTION for doc
            dynamic content = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "AllowedChildTypes" }
            });
            //Console.WriteLine(content.AllowedChildTypes.Count);

            // ASSERT
            Assert.AreEqual(0, content.AllowedChildTypes.Count);

            // ACTION-2
            dynamic content2 = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content",
                Expand = new[] { "AllowedChildTypes" }
            });

            // ASSERT-2
            Assert.AreEqual(10, content2.AllowedChildTypes.Count);
            Assert.AreEqual("Folder", content2.AllowedChildTypes[0].Name.ToString());
        }
        [TestMethod]
        [Description("Expand 5")]
        public async Task Docs_BasicConcepts_Expand_Actions()
        {
            // ACTION for doc
            dynamic content = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "Actions" }
            });
            //Console.WriteLine(content.Actions.Count);

            // ASSERT
            Assert.AreEqual(59, content.Actions.Count);
            Assert.AreEqual("Add", content.Actions[0].Name.ToString());
        }

        /* ====================================================================================== Ordering and Pagination */

        [TestMethod]
        [Description("")]
        public async Task Docs_BasicConcepts_OrderBy_DisplayName()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$orderby", "DisplayName" } }
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.DisplayName);

            // ASSERT
            var names = result.Select(x => ((dynamic) x).DisplayName.ToString()).ToArray();
            Assert.IsTrue(names.Length > 2);
            for (int i = 1; i < names.Length; i++)
                Assert.IsTrue(string.CompareOrdinal(names[i], names[i - 1]) >= 0, $"names[{i}] < names[{i - 1}]");
        }
        [TestMethod]
        [Description("Order by a field in an explicit direction")]
        public async Task Docs_BasicConcepts_OrderBy_Id_Asc()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$orderby", "Id asc" } }
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Id);

            // ASSERT
            var ids = result.Select(x => (int)((dynamic)x).Id).ToArray();
            Assert.IsTrue(ids.Length > 2);
            for (int i = 1; i < ids.Length; i++)
                Assert.IsTrue(ids[i] >= ids[i - 1], $"ids[{i}] < ids[{i - 1}]");
        }
        [TestMethod]
        [Description("Order by a field in reverse order")]
        public async Task Docs_BasicConcepts_OrderBy_CreationDate_Desc()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Munich", "Folder");

            var date = DateTime.Today.AddDays(-1);
            var children = await Content.LoadCollectionAsync("/Root/Content/IT/Document_Library");
            foreach (Content child in children)
            {
                child["CreationDate"] = date;
                await child.SaveAsync();
                date = date.AddHours(1);
            }

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$orderby", "CreationDate desc" } }
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.CreationDate);

            // ASSERT
            var dates = result.Select(x => (DateTime)((dynamic)x).CreationDate).ToArray();
            Assert.IsTrue(dates.Length > 2);
            for (int i = 1; i < dates.Length; i++)
                Assert.IsTrue(dates[i] <= dates[i - 1], $"dates[{i}] > dates[{i - 1}]");
        }
        [TestMethod]
        [Description("Order by a multiple fields")]
        public async Task Docs_BasicConcepts_OrderBy_DisplayNameAndName()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Munich", "Folder");

            var date = DateTime.Today.AddDays(-1);
            var children = await Content.LoadCollectionAsync("/Root/Content/IT/Document_Library");
            foreach (Content child in children)
            {
                child["ModificationDate"] = date;
                await child.SaveAsync();
                date = date.AddHours(1);
            }

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                OrderBy = new[] { "ModificationDate desc", "DisplayName", "Name" }
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            var dates = result.Select(x => (DateTime)((dynamic)x).ModificationDate).ToArray();
            Assert.IsTrue(dates.Length > 2);
            for (int i = 1; i < dates.Length; i++)
                Assert.IsTrue(dates[i] <= dates[i - 1], $"dates[{i}] > dates[{i - 1}]");
        }
        [TestMethod]
        [Description("Top")]
        public async Task Docs_BasicConcepts_Top()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Top = 5,
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.IsTrue(result.Count() <= 5);
        }
        [TestMethod]
        [Description("Skip")]
        public async Task Docs_BasicConcepts_Skip()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Skip = 5,
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Pagination")]
        public async Task Docs_BasicConcepts_Pagination()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Top = 3,
                Skip = 3
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Search and filtering */

        [TestMethod]
        [Description("Filtering by Field value 1")]
        public async Task Docs_BasicConcepts_Filter_Id()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$filter", "Id gt 11" } },
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Id);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Filtering by Field value 2")]
        public async Task Docs_BasicConcepts_Filter_substringof()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$filter", "substringof('Lorem', Description) eq true" } },
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Description);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Filtering by Field value 3")]
        public async Task Docs_BasicConcepts_Filter_startswith()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$filter", "startswith(Name, 'Document') eq true" } },
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Filtering by Field value 4")]
        public async Task Docs_BasicConcepts_Filter_endswith()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$filter", "endswith(Name, 'Library') eq true" } },
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Id);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Filtering by Date")]
        public async Task Docs_BasicConcepts_Filter_DateTime()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$filter", "CreationDate gt datetime'2019-03-26T03:55:00'" } },
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.CreationDate);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Filtering by an exact Type")]
        public async Task Docs_BasicConcepts_Filter_ContentType()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Parameters = { { "$filter", "ContentType eq 'Folder'" } },
            });
            //foreach (dynamic content in result)
            //    Console.WriteLine(content.Type);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Filtering by Type family")]
        public async Task Docs_BasicConcepts_Filter_isof()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                ChildrenFilter = "isof('Folder')",
            });
            //foreach (dynamic content in result)
            //    Console.WriteLine(content.Type);

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Metadata */

        [TestMethod]
        [Description("Metadata")]
        public async Task Docs_BasicConcepts_MetadataFormat()
        {
            var content = 
                // ACTION for doc
                await Content.LoadAsync(new ODataRequest
                {
                    Path = "/Root/Content/IT",
                    Metadata = MetadataFormat.None,
                });

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("$metadata 1")]
        public async Task Docs_BasicConcepts_GlobalMetadata()
        {
            var url = ClientContext.Current.Server.Url;

            var response = 
                // ACTION for doc
                await RESTCaller.GetResponseStringAsync(
                    new Uri(url + "/OData.svc/$metadata"));

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("$metadata 2")]
        public async Task Docs_BasicConcepts_LocalMetadata()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            var url = ClientContext.Current.Server.Url;

            var response =
                // ACTION for doc
                await RESTCaller.GetResponseStringAsync(
                    new Uri(url + "/OData.svc/Root/Content/IT/Document_Library/$metadata"));

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Metadata */

        [TestMethod]
        [Description("Accessing system content")]
        public async Task Docs_BasicConcepts_AutoFilters()
        {
            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                AutoFilters = FilterStatus.Disabled,
            });
            //foreach(var content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Lifespan */

        [TestMethod]
        [Description("Filter content by lifespan validity")]
        public async Task Docs_BasicConcepts_LifespanFilter()
        {
            // ACTION for doc
            var result = await Content.LoadCollectionAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                LifespanFilter = FilterStatus.Enabled,
            });
            //foreach (var content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Actions */

        [TestMethod]
        [Description("Exploring actions")]
        public async Task Docs_BasicConcepts_Actions()
        {
            // ACTION for doc
            dynamic content = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "Actions" },
                Select = new[] { "Actions" },
            });
            //foreach(var item in content.Actions)
            //    Console.WriteLine(item.Name);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Scenario")]
        public async Task Docs_BasicConcepts_Scenario()
        {
            //UNDONE:- Feature request: scenario filter of Actions
            //dynamic content8 = await Content.LoadAsync(new ODataRequest
            //{
            //    Path = "/Root/IMS",
            //    Expand = new[] { "Actions" },
            //    Select = new[] { "Name", "Actions" },
            //    Scenario = "UserMenu",
            //});

            // ACTION for doc
            dynamic content = await RESTCaller.GetContentAsync(new ODataRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "Actions" },
                Select = new[] { "Actions" },
                Parameters = { { "scenario", "ContextMenu" } }
            });
            //foreach(var item in content.Actions)
            //    Console.WriteLine(item.Name);

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Schema */

        [TestMethod]
        [Description("Get schema")]
        public async Task Docs_BasicConcepts_GetSchema()
        {
            // ACTION for doc
            string schema = await RESTCaller.GetResponseStringAsync("/Root", "GetSchema");

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Change the schema")]
        public async Task Docs_BasicConcepts_GetCtd()
        {
            /*
            // WARNING This code cannot run if the #1064 does not exist.
            // ACTION for doc
            string ctd = null;
            await RESTCaller.GetStreamResponseAsync(1064, async message =>
            {
                ctd = await message.Content.ReadAsStringAsync();
            }, CancellationToken.None);
            */

            // IMPROVED TEST
            var content = await Content.LoadAsync("/Root/System/Schema/ContentTypes/GenericContent/File").ConfigureAwait(false);
            var fileId = content.Id;

            // ACTION
            string ctd = null;
            await RESTCaller.GetStreamResponseAsync(fileId, async message =>
            {
                ctd = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
            }, CancellationToken.None).ConfigureAwait(false);

            // ASSERT
            Assert.IsTrue(ctd.StartsWith("<?xml") || ctd.StartsWith("<ContentType"));
            Assert.IsTrue(ctd.Trim().EndsWith("</ContentType>"));
        }
    }
}
