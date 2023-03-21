using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class BasicConcepts : ClientIntegrationTestBase
    {
        private class MyContent : Content { public MyContent(IRestCaller rc, ILogger<Content> l) : base(rc, l) { } }
        // ReSharper disable once InconsistentNaming
        private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        // ReSharper disable once InconsistentNaming
        IRepository repository =>
            GetRepositoryCollection(services => { services.RegisterGlobalContentType<MyContent>(); })
                .GetRepositoryAsync("local", cancel).GetAwaiter().GetResult();

        /* ====================================================================================== Entry */

        /// <tab category="basic-concepts" article="entry" example="byId" />
        [TestMethod]
        [Description("Get a single content by Id")]
        public async Task Docs2_BasicConcepts_GetSingleContentById()
        {
            var content =
                /*<doc>*/await repository.LoadContentAsync(1284, cancel)/*</doc>*/.ConfigureAwait(false);
            Assert.IsNotNull(content);
            Assert.AreEqual(1284, content.Id);

            /*<doc>*/
            // or
            /*</doc>*/

            var myContent =
                /*<doc>*/await repository.LoadContentAsync<MyContent>(1284, cancel)/*</doc>*/.ConfigureAwait(false);
            Assert.IsNotNull(myContent);
            Assert.AreEqual(1284, myContent.Id);
        }

        /// <tab category="basic-concepts" article="entry" example="byPath" />
        [TestMethod]
        [Description("Get a single content by Path")]
        public async Task Docs2_BasicConcepts_GetSingleContentByPath()
        {
            var content =
                /*<doc>*/await repository.LoadContentAsync("/Root/Content/IT", cancel)/*</doc>*/
                    .ConfigureAwait(false);
            Assert.IsNotNull(content);
            Assert.AreEqual("/Root/Content/IT", content.Path);
            /*<doc>*/
            // or
            /*</doc>*/
            var myContent =
                /*<doc>*/await repository.LoadContentAsync<MyContent>("/Root/Content/IT", cancel)/*</doc>*/.ConfigureAwait(false);
            Assert.IsNotNull(myContent);
            Assert.AreEqual("/Root/Content/IT", myContent.Path);
        }

        /// <tab category="basic-concepts" article="entry" example="property" />
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

        /// <tab category="basic-concepts" article="entry" example="propertyValue" />
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

        /// <tab category="basic-concepts" article="collection" example="children" />
        [TestMethod]
        [Description("Children of a content (collection)")]
        public async Task Docs2_BasicConcepts_GetChildren()
        {
            var children1 =
                /*<doc>*/
                await repository.LoadCollectionAsync(new LoadCollectionRequest { Path = "/Root/Content" }, cancel)
                /*</doc>*/
                .ConfigureAwait(false);

            var childArray1 = children1.ToArray();
            Assert.IsTrue(childArray1.Length > 0);
            Assert.AreEqual("/Root/Content", childArray1[0].ParentPath);
        }

        /// <tab category="basic-concepts" article="collection" example="count" />
        [TestMethod]
        [Description("Count of a collection")]
        public async Task Docs2_BasicConcepts_ChildrenCount()
        {
            var count =
                /*<doc>*/await repository.GetContentCountAsync(new LoadCollectionRequest {Path = "/Root/Content"}, cancel)
                    /*</doc>*/
                    .ConfigureAwait(false);

            var children = 
                await repository.LoadCollectionAsync(new LoadCollectionRequest { Path = "/Root/Content" }, cancel)
                    .ConfigureAwait(false);
            Assert.AreNotEqual(0, count);
            Assert.AreEqual(children.ToArray().Length, count);
        }

        /// <tab category="basic-concepts" article="collection" example="inlinecount" />
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

            var array = ((JToken)result).SelectTokens("$.d.results.*").ToArray();
            var inlineCount = ((JToken)result).SelectToken("$.d.__count").Value<int>();

            Assert.AreNotEqual(0, inlineCount);
            Assert.AreNotEqual(array.Length, inlineCount);
        }

        /* ====================================================================================== Select and expand */

        /// <tab category="basic-concepts" article="select-expand" example="select" />
        [TestMethod]
        [Description("Select")]
        public async Task Docs2_BasicConcepts_Select()
        {
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/IT",
                Select = new[] { "DisplayName", "Description" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var displayName = content.DisplayName.ToString();
            var description = content.Description.ToString();
            Assert.IsNotNull(displayName);
            Assert.IsNotNull(description);
        }

        /// <tab category="basic-concepts" article="select-expand" example="expand" />
        [TestMethod]
        [Description("Expand 1")]
        public async Task Docs2_BasicConcepts_Expand_CreatedBy()
        {
            // ACTION for doc
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "CreatedBy" },
            }, cancel)/*</doc>*/.ConfigureAwait(false);
            //Console.WriteLine(content.CreatedBy.Name);

            // ASSERT
            Assert.AreEqual("Admin", content.CreatedBy.Name.ToString());
        }

        /// <tab category="basic-concepts" article="select-expand" example="expandExpanded" />
        [TestMethod]
        [Description("Expand 2")]
        public async Task Docs2_BasicConcepts_Expand_CreatedByCreatedBy()
        {
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "CreatedBy/CreatedBy" },
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            Assert.AreEqual("Admin", content.CreatedBy.CreatedBy.Name.ToString());
        }

        /// <tab category="basic-concepts" article="select-expand" example="expandAndSelect" />
        [TestMethod]
        [Description("Expand 3")]
        public async Task Docs2_BasicConcepts_Expand_CreatedByName()
        {
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "CreatedBy" },
                Select = new[] { "Name", "CreatedBy/Name" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            Assert.AreEqual("Admin", content.CreatedBy.Name.ToString());
        }

        /// <tab category="basic-concepts" article="select-expand" example="expandAllowedChildTypes" />
        [TestMethod]
        [Description("Expand 4")]
        public async Task Docs2_BasicConcepts_Expand_AllowedChildTypes()
        {
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "AllowedChildTypes" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            Assert.AreEqual(0, content.AllowedChildTypes.Count);

            dynamic content2 = await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content",
                Expand = new[] { "AllowedChildTypes" }
            }, cancel).ConfigureAwait(false);

            Assert.IsTrue(10 < (int)content2.AllowedChildTypes.Count);
            Assert.AreEqual("Folder", content2.AllowedChildTypes[0].Name.ToString());
        }

        /// <tab category="basic-concepts" article="select-expand" example="expandActions" />
        [TestMethod]
        [Description("Expand 5")]
        public async Task Docs2_BasicConcepts_Expand_Actions()
        {
            // ACTION for doc
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/IT",
                Expand = new[] { "Actions" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            Assert.IsTrue(50 < (int)content.Actions.Count);
            Assert.AreEqual("Add", content.Actions[0].Name.ToString());
        }

        /* ====================================================================================== Ordering and Pagination */

        /// <tab category="basic-concepts" article="ordering-paging" example="orderByOneProperty" />
        [TestMethod]
        [Description("")]
        public async Task Docs2_BasicConcepts_OrderBy_DisplayName()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                OrderBy = new []{"DisplayName"}
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var names = result.Select(x => ((dynamic)x).DisplayName.ToString()).ToArray();
            Assert.IsTrue(names.Length > 2);
            for (int i = 1; i < names.Length; i++)
                Assert.IsTrue(string.CompareOrdinal(names[i], names[i - 1]) >= 0, $"names[{i}] < names[{i - 1}]");
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="orderExplicitDirection" />
        [TestMethod]
        [Description("Order by a field in an explicit direction")]
        public async Task Docs2_BasicConcepts_OrderBy_Id_Asc()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                OrderBy = new[] { "Id asc" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var ids = result.Select(x => (int)((dynamic)x).Id).ToArray();
            Assert.IsTrue(ids.Length > 2);
            for (int i = 1; i < ids.Length; i++)
                Assert.IsTrue(ids[i] >= ids[i - 1], $"ids[{i}] < ids[{i - 1}]");
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="reverseOrder" />
        [TestMethod]
        [Description("Order by a field in reverse order")]
        public async Task Docs2_BasicConcepts_OrderBy_CreationDate_Desc()
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

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                OrderBy = new[] { "CreationDate desc" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var dates = result.Select(x => (DateTime)((dynamic)x).CreationDate).ToArray();
            Assert.IsTrue(dates.Length > 2);
            for (int i = 1; i < dates.Length; i++)
                Assert.IsTrue(dates[i] <= dates[i - 1], $"dates[{i}] > dates[{i - 1}]");
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="orderByMultipleFields" />
        [TestMethod]
        [Description("Order by a multiple fields")]
        public async Task Docs2_BasicConcepts_OrderBy_DisplayNameAndName()
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

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                OrderBy = new[] { "ModificationDate desc", "DisplayName", "Name" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var dates = result.Select(x => (DateTime)((dynamic)x).ModificationDate).ToArray();
            Assert.IsTrue(dates.Length > 2);
            for (int i = 1; i < dates.Length; i++)
                Assert.IsTrue(dates[i] <= dates[i - 1], $"dates[{i}] > dates[{i - 1}]");
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="top" />
        [TestMethod]
        [Description("Top")]
        public async Task Docs2_BasicConcepts_Top()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            for (var i = 0; i < 6; i++)
                await EnsureContentAsync($"/Root/Content/IT/Document_Library/Folder-{i + 1}", "Folder");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Top = 5,
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            Assert.IsTrue(result.Count() <= 5);
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="skip" />
        [TestMethod]
        [Description("Skip")]
        public async Task Docs2_BasicConcepts_Skip()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            for (var i = 0; i < 6; i++)
                await EnsureContentAsync($"/Root/Content/IT/Document_Library/Folder-{i + 1}", "Folder");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Skip = 5,
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var ids = result.Select(c => c.Id).ToArray();
            var children = await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
            }, cancel).ConfigureAwait(false);
            var allIds = children.Select(c => c.Id).ToArray();
            var expected = string.Join(",", allIds.Skip(5).Select(x => x.ToString()));
            var actual = string.Join(",", ids.Select(x => x.ToString()));
            Assert.AreEqual(expected, actual);
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="paging" />
        [TestMethod]
        [Description("Pagination")]
        public async Task Docs2_BasicConcepts_Pagination()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            for (var i = 0; i < 6; i++)
                await EnsureContentAsync($"/Root/Content/IT/Document_Library/Folder-{i + 1}", "Folder");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                Top = 3,
                Skip = 3
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var ids = result.Select(c => c.Id).ToArray();
            var children = await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
            }, cancel).ConfigureAwait(false);
            var allIds = children.Select(c => c.Id).ToArray();
            var expected = string.Join(",", allIds.Skip(3).Take(3).Select(x => x.ToString()));
            var actual = string.Join(",", ids.Select(x => x.ToString()));
            Assert.AreEqual(expected, actual);
        }

        /* ====================================================================================== Search and filtering */

        /// <tab category="basic-concepts" article="search-filter" example="greaterThan" />
        [TestMethod]
        [Description("Filtering by Field value 1")]
        public async Task Docs2_BasicConcepts_Filter_Id()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                ChildrenFilter = "Id gt 11"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            // ASSERT
            var ids = result.Select(x => x.Id);
            foreach (var id in ids)
                Assert.IsTrue(id > 11);
        }

        /// <tab category="basic-concepts" article="search-filter" example="substringof" />
        [TestMethod]
        [Description("Filtering by Field value 2")]
        public async Task Docs2_BasicConcepts_Filter_substringof()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Folder-1", "Folder");
            dynamic folder1 = await repository.LoadContentAsync("/Root/Content/IT/Document_Library/Folder-1", cancel)
                .ConfigureAwait(false);
            folder1.Description = "Lorem ipsum dolor sit amet";
            folder1.SaveAsync(cancel);

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                ChildrenFilter = "substringof('Lorem', Description) eq true"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(0 < contents.Length);
            Assert.IsTrue(contents.All(x => x["Description"].ToString().Contains("Lorem")));
        }

        /// <tab category="basic-concepts" article="search-filter" example="startswith" />
        [TestMethod]
        [Description("Filtering by Field value 3")]
        public async Task Docs2_BasicConcepts_Filter_startswith()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Documents-1", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Documents-2", "Folder");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                ChildrenFilter = "startswith(Name, 'Document') eq true"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(0 < contents.Length);
            Assert.IsTrue(contents.All(x => x.Name.StartsWith("Document")));
        }

        /// <tab category="basic-concepts" article="search-filter" example="endswith" />
        [TestMethod]
        [Description("Filtering by Field value 4")]
        public async Task Docs2_BasicConcepts_Filter_endswith()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Book-Library", "Folder");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                ChildrenFilter = "endswith(Name, 'Library') eq true"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(0 < contents.Length);
            Assert.IsTrue(contents.All(x => x.Name.EndsWith("Library")));
        }

        /// <tab category="basic-concepts" article="search-filter" example="byDate" />
        [TestMethod]
        [Description("Filtering by Date")]
        public async Task Docs2_BasicConcepts_Filter_DateTime()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                ChildrenFilter = "CreationDate gt datetime'2019-03-26T03:55:00'"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(2 < contents.Length);
            var date = new DateTime(2019, 03, 26, 03, 55, 00);
            Assert.IsTrue(contents.All(x => ((JValue)x["CreationDate"]).Value<DateTime>() > date));
        }

        /// <tab category="basic-concepts" article="search-filter" example="byExactType" />
        [TestMethod]
        [Description("Filtering by an exact Type")]
        public async Task Docs2_BasicConcepts_Filter_ContentType()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/SystemFolder-1", "SystemFolder");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                ChildrenFilter = "ContentType eq 'Folder'"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(2 < contents.Length);
            var types = contents.Select(c => c["Type"].ToString()).Distinct().OrderBy(x => x).ToArray();
            Assert.AreEqual("Folder", string.Join(",", types));
        }

        /// <tab category="basic-concepts" article="search-filter" example="byTypeFamily" />
        [TestMethod]
        [Description("Filtering by Type family")]
        public async Task Docs2_BasicConcepts_Filter_isof()
        {
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/SystemFolder-1", "SystemFolder");

            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/IT/Document_Library",
                ChildrenFilter = "isof('Folder')"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(2 < contents.Length);
            var types = contents.Select(c => c["Type"].ToString()).Distinct().OrderBy(x => x).ToArray();
            Assert.AreEqual("Folder,SystemFolder", string.Join(",", types));
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
