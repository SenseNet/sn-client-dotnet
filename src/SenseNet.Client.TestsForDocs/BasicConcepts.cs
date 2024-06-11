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
        private IRepository repository =>
            GetRepositoryCollection(services =>
                {
                    services.RegisterGlobalContentType<MyContent>();
                })
                .GetRepositoryAsync("local", cancel).GetAwaiter().GetResult();

        /* ====================================================================================== Entry */

        /// <tab category="basic-concepts" article="entry" example="byId" />
        [TestMethod]
        [Description("Get a single content by Id")]
        public async Task Docs2_BasicConcepts_GetSingleContentById()
        {
            var content =
                /*<doc>*/await repository.LoadContentAsync(1368, cancel)/*</doc>*/.ConfigureAwait(false);
            Assert.IsNotNull(content);
            Assert.AreEqual(1368, content.Id);

            /*<doc>*/
            // or
            /*</doc>*/

            var myContent =
                /*<doc>*/await repository.LoadContentAsync<Folder>(1368, cancel)/*</doc>*/.ConfigureAwait(false);
            Assert.IsNotNull(myContent);
            Assert.AreEqual(1368, myContent.Id);
        }

        /// <tab category="basic-concepts" article="entry" example="byPath" />
        [TestMethod]
        [Description("Get a single content by Path")]
        public async Task Docs2_BasicConcepts_GetSingleContentByPath()
        {
            var content =
                /*<doc>*/await repository.LoadContentAsync("/Root/Content/Cars", cancel)/*</doc>*/
                    .ConfigureAwait(false);
            Assert.IsNotNull(content);
            Assert.AreEqual("/Root/Content/Cars", content.Path);
            /*<doc>*/
            // or
            /*</doc>*/
            var myContent =
                /*<doc>*/await repository.LoadContentAsync<Folder>("/Root/Content/Cars", cancel)/*</doc>*/.ConfigureAwait(false);
            Assert.IsNotNull(myContent);
            Assert.AreEqual("/Root/Content/Cars", myContent.Path);
        }

        /// <tab category="basic-concepts" article="entry" example="property" />
        [TestMethod]
        [Description("Addressing a single property of a content")]
        public async Task Docs2_BasicConcepts_GetSingleProperty()
        {
            var response =
                // ACTION for doc
                /*<doc>*/
                await repository.GetResponseStringAsync(new ODataRequest
                {
                    Path = "/Root/Content/Cars",
                    PropertyName = "Description"
                }, HttpMethod.Get, cancel);
                /*</doc>*/
            // ASSERT
            Assert.AreEqual("{\"d\":{\"Description\":\"Thisfoldercontainsourcars.\"}}", response.RemoveWhitespaces());
        }

        /// <tab category="basic-concepts" article="entry" example="propertyValue" />
        [TestMethod]
        [Description("Addressing a property value")]
        public async Task Docs2_BasicConcepts_GetSinglePropertyValue()
        {
            var response =
                // ACTION for doc
                /*<doc>*/
                await repository.GetResponseStringAsync(
                    new Uri(repository.Server.Url + "/OData.svc/Root/Content/('Cars')/Description/$value"),
                    HttpMethod.Get,
                    postData: null,
                    additionalHeaders: null,
                    cancel);
                /*</doc>*/

            // ASSERT
            Assert.AreEqual("This folder contains our cars.", response);
        }

        /* ====================================================================================== Collection */

        /// <tab category="basic-concepts" article="collection" example="children" />
        [TestMethod]
        [Description("Children of a content (collection)")]
        public async Task Docs2_BasicConcepts_GetChildren()
        {
            var children1 =
                /*<doc>*/
                await repository.LoadCollectionAsync(new LoadCollectionRequest { Path = "/Root/Content/Cars" }, cancel)
                /*</doc>*/
                .ConfigureAwait(false);

            var childArray1 = children1.ToArray();
            Assert.IsTrue(childArray1.Length > 0);
            Assert.AreEqual("/Root/Content/Cars", childArray1[0].ParentPath);
        }

        /// <tab category="basic-concepts" article="collection" example="count" />
        [TestMethod]
        [Description("Count of a collection")]
        public async Task Docs2_BasicConcepts_ChildrenCount()
        {
            var count =
                /*<doc>*/await repository.GetContentCountAsync(new LoadCollectionRequest {Path = "/Root/Content/Cars"}, cancel)
                    /*</doc>*/
                    .ConfigureAwait(false);

            var children = 
                await repository.LoadCollectionAsync(new LoadCollectionRequest { Path = "/Root/Content/Cars" }, cancel)
                    .ConfigureAwait(false);
            Assert.AreNotEqual(0, count);
            Assert.AreEqual(children.ToArray().Length, count);
        }

        /// <tab category="basic-concepts" article="collection" example="inlinecount" />
        [TestMethod]
        [Description("$inlinecount query option")]
        public async Task Docs2_BasicConcepts_ChildrenInlineCount()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                Top = 3,
                Skip = 4,
                InlineCount = InlineCountOptions.AllPages
            }, cancel);
            Console.WriteLine($"TotalCount: {result.TotalCount}, Count: {result.Count}");
            /*</doc>*/

            // ASSERT
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(13, result.TotalCount);
        }

        /* ====================================================================================== Select and expand */

        /// <tab category="basic-concepts" article="select-expand" example="select" />
        [TestMethod]
        [Description("Select")]
        public async Task Docs2_BasicConcepts_Select()
        {
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/Cars/AAKE452",
                Select = new[] { "Make", "Model", "Color" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var make = content.Make.ToString();
            var model = content.Model.ToString();
            var color = content.Color.ToString();
            Assert.IsNotNull(make);
            Assert.IsNotNull(model);
            Assert.IsNotNull(color);
        }

        /// <tab category="basic-concepts" article="select-expand" example="expand" />
        [TestMethod]
        [Description("Expand 1")]
        public async Task Docs2_BasicConcepts_Expand_CreatedBy()
        {
            // ACTION for doc
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/Cars/OT1234",
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
                Path = "/Root/Content/Cars/OT1234",
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
                Path = "/Root/Content/Cars/OT1234",
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
                Path = "/Root/Content/Cars",
                Expand = new[] { "AllowedChildTypes" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            Assert.AreEqual(0, content.AllowedChildTypes.Length);

            dynamic content2 = await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content",
                Expand = new[] { "AllowedChildTypes" }
            }, cancel).ConfigureAwait(false);

            Assert.IsTrue(10 < (int)content2.AllowedChildTypes.Length);
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
                Path = "/Root/Content/Cars",
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
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                OrderBy = new[] {"DisplayName"}
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var names = result.Select(x => ((dynamic)x).DisplayName.ToString()).ToArray();
            Assert.AreEqual(13, names.Length);
            for (int i = 1; i < names.Length; i++)
                Assert.IsTrue(string.CompareOrdinal(names[i], names[i - 1]) >= 0, $"names[{i}] < names[{i - 1}]");
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="orderExplicitDirection" />
        [TestMethod]
        [Description("Order by a field in an explicit direction")]
        public async Task Docs2_BasicConcepts_OrderBy_Id_Asc()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                OrderBy = new[] { "Price asc" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var prices = result
                .Where(x => ((dynamic)x).Price != null)
                .Select(x => (int)((dynamic)x).Price)
                .ToArray();
            Assert.IsTrue(prices.Length > 2);
            for (int i = 1; i < prices.Length; i++)
                Assert.IsTrue(prices[i] >= prices[i - 1], $"prices[{i}] < prices[{i - 1}]");
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="reverseOrder" />
        [TestMethod]
        [Description("Order by a field in reverse order")]
        public async Task Docs2_BasicConcepts_OrderBy_CreationDate_Desc()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars", 
                OrderBy = new[] { "StartingDate desc" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var dates = result
                .Where(x=> ((dynamic)x).StartingDate != null)
                .Select(x => (DateTime)((dynamic)x).StartingDate)
                .ToArray();
            Assert.IsTrue(dates.Length > 2);
            for (int i = 1; i < dates.Length; i++)
                Assert.IsTrue(dates[i] <= dates[i - 1], $"dates[{i}] > dates[{i - 1}]");
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="orderByMultipleFields" />
        [TestMethod]
        [Description("Order by a multiple fields")]
        public async Task Docs2_BasicConcepts_OrderBy_DisplayNameAndName()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                OrderBy = new[] { "StartingDate desc", "DisplayName", "Name" }
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var dates = result
                .Where(x => ((dynamic) x).StartingDate != null)
                .Select(x => (DateTime) ((dynamic) x).StartingDate)
                .ToArray();
            Assert.IsTrue(dates.Length > 2);
            for (int i = 1; i < dates.Length; i++)
                Assert.IsTrue(dates[i] <= dates[i - 1], $"dates[{i}] > dates[{i - 1}]");
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="top" />
        [TestMethod]
        [Description("Top")]
        public async Task Docs2_BasicConcepts_Top()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                Top = 5,
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            Assert.IsTrue(result.Count() <= 5);
        }

        /// <tab category="basic-concepts" article="ordering-paging" example="skip" />
        [TestMethod]
        [Description("Skip")]
        public async Task Docs2_BasicConcepts_Skip()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                Skip = 5,
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var ids = result.Select(c => c.Id).ToArray();
            var children = await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
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
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                Top = 3,
                Skip = 3
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var ids = result.Select(c => c.Id).ToArray();
            var children = await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
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
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                ChildrenFilter = "Price gt 1000000.0m" // The "m" suffix is required in case of Number fields
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            // ASSERT
            var prices = result.Select(x => (decimal)((dynamic)x).Price);
            foreach (var price in prices)
                Assert.IsTrue(price > 1_000_000);
        }

        /// <tab category="basic-concepts" article="search-filter" example="substringof" />
        [TestMethod]
        [Description("Filtering by Field value 2")]
        public async Task Docs2_BasicConcepts_Filter_substringof()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                ChildrenFilter = "substringof('Supra', DisplayName) eq true"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(0 < contents.Length);
            Assert.IsTrue(contents.All(x => x.DisplayName?.Contains("Supra") ?? false));
        }

        /// <tab category="basic-concepts" article="search-filter" example="startswith" />
        [TestMethod]
        [Description("Filtering by Field value 3")]
        public async Task Docs2_BasicConcepts_Filter_startswith()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                ChildrenFilter = "startswith(DisplayName, 'Toyota') eq true"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(0 < contents.Length);
            Assert.IsTrue(contents.All(x => x.DisplayName?.StartsWith("Toyota") ?? false));
        }

        /// <tab category="basic-concepts" article="search-filter" example="endswith" />
        [TestMethod]
        [Description("Filtering by Field value 4")]
        public async Task Docs2_BasicConcepts_Filter_endswith()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                ChildrenFilter = "endswith(DisplayName, 'Octavia') eq true"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(0 < contents.Length);
            Assert.IsTrue(contents.All(x => x.DisplayName?.EndsWith("Octavia") ?? false));
        }

        /// <tab category="basic-concepts" article="search-filter" example="byDate" />
        [TestMethod]
        [Description("Filtering by Date")]
        public async Task Docs2_BasicConcepts_Filter_DateTime()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                ChildrenFilter = "StartingDate gt datetime'2020-01-12T03:55:00'"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(2 < contents.Length);
            var date = new DateTime(2019, 03, 26, 03, 55, 00);
            Assert.IsTrue(contents.All(x => ((JValue)x["StartingDate"]).Value<DateTime>() > date));
        }

        /// <tab category="basic-concepts" article="search-filter" example="byExactType" />
        [TestMethod]
        [Description("Filtering by an exact Type")]
        public async Task Docs2_BasicConcepts_Filter_ContentType()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                ChildrenFilter = "ContentType eq 'Car'"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(2 < contents.Length);
            var types = contents.Select(c => c["Type"].ToString()).Distinct().OrderBy(x => x).ToArray();
            Assert.AreEqual("Car", string.Join(",", types));
        }

        /// <tab category="basic-concepts" article="search-filter" example="byTypeFamily" />
        [TestMethod]
        [Description("Filtering by Type family")]
        public async Task Docs2_BasicConcepts_Filter_isof()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                ChildrenFilter = "isof('Folder')"
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(1 < contents.Length);
            var types = contents.Select(c => c["Type"].ToString()).Distinct().OrderBy(x => x).ToArray();
            Assert.AreEqual("Folder,SystemFolder", string.Join(",", types));
        }

        /* ====================================================================================== Metadata */

        /// <tab category="basic-concepts" article="metadata" example="metadata" />
        [TestMethod]
        [Description("Metadata")]
        public async Task Docs2_BasicConcepts_MetadataFormat()
        {
            var content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
                {
                    Path = "/Root/Content/Cars",
                    Metadata = MetadataFormat.None // None, Minimal, Full (default)
                }, cancel)/*</doc>*/.ConfigureAwait(false);

            Assert.IsNull(content["__metadata"]);
        }

        /// <tab category="basic-concepts" article="metadata" example="global-metadata" />
        [TestMethod]
        [Description("$metadata 1")]
        public async Task Docs2_BasicConcepts_GlobalMetadata()
        {
            var response =
                // ACTION for doc
                /*<doc>*/
                await repository.GetResponseStringAsync(
                    new Uri(repository.Server.Url + "/OData.svc/$metadata"),
                    HttpMethod.Get,
                    postData: null,
                    additionalHeaders: null,
                    cancel);
                /*</doc>*/

            // ASSERT
            Assert.IsTrue(response.Contains("<edmx:Edmx"));
            Assert.IsTrue(response.Contains("<EntityType Name=\"Car\" BaseType=\"GenericContent\">"));
        }

        /// <tab category="basic-concepts" article="metadata" example="doclib-metadata" />
        [TestMethod]
        [Description("$metadata 2")]
        public async Task Docs2_BasicConcepts_LocalMetadata()
        {
            var response =
                // ACTION for doc
                /*<doc>*/
                await repository.GetResponseStringAsync(
                    new Uri(repository.Server.Url + "/OData.svc/Root/Content/Cars/$metadata"),
                    HttpMethod.Get,
                    postData: null,
                    additionalHeaders: null,
                    cancel);
            /*</doc>*/

            // ASSERT
            Assert.IsTrue(response.Contains("<edmx:Edmx"));
            Assert.IsTrue(response.Contains("<EntityType Name=\"Folder\" BaseType=\"GenericContent\">"));
            Assert.IsFalse(response.Contains("<EntityType Name=\"GenericContent\""));
        }

        /* ====================================================================================== Autofilters */

        /// <tab category="basic-concepts" article="system-content" example="autofilter" />
        [TestMethod]
        [Description("Accessing system content")]
        public async Task Docs2_BasicConcepts_AutoFilters()
        {
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                AutoFilters = FilterStatus.Disabled
            }, cancel)/*/<doc>*/.ConfigureAwait(false);

            var contents = result.ToArray();
            Assert.IsTrue(contents.Any());
            var types = contents.Select(c => c["Type"].ToString()).Distinct().OrderBy(x => x).ToArray();
            Assert.IsTrue(types.Contains("SystemFolder"));
        }

        /* ====================================================================================== Lifespan */

        /// <tab category="basic-concepts" article="lifespan" example="lifespanfilter" />
        [TestMethod] //TODO: Missing assertion in this test
        [Description("Filter content by lifespan validity")]
        public async Task Docs2_BasicConcepts_LifespanFilter()
        {
            // ACTION for doc
            var result = /*<doc>*/await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = "/Root/Content/Cars",
                LifespanFilter = FilterStatus.Enabled
            }, cancel)/*/<doc>*/.ConfigureAwait(false);

            //Assert.Inconclusive("TODO: Missing assertion in this test");
        }

        /* ====================================================================================== Actions */

        /// <tab category="basic-concepts" article="action" example="actions" />
        [TestMethod]
        [Description("Exploring actions")]
        public async Task Docs2_BasicConcepts_Actions()
        {
            dynamic content = /*<doc>*/await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/Cars",
                Expand = new[] { "Actions" },
                Select = new[] { "Actions" },
            }, cancel)/*</doc>*/.ConfigureAwait(false);

            var actions = content["Actions"];
            Assert.IsTrue(10 < actions.Count);
        }

        /// <tab category="basic-concepts" article="action" example="scenario" />
        [TestMethod]
        [Description("Scenario")]
        public async Task Docs2_BasicConcepts_Scenario()
        {
            // ACTION for doc
            /*<doc>*/
            dynamic content = await repository.LoadContentAsync(new LoadContentRequest
            {
                Path = "/Root/Content/Cars",
                Expand = new[] { "Actions" },
                Select = new[] { "Actions" },
                Parameters = { { "scenario", "ContextMenu" } }
            }, cancel);

            var actionNames = new List<string>();
            foreach (var item in content.Actions)
                actionNames.Add(item.Name.ToString());
            /*</doc>*/

            // ASSERT
            Assert.IsTrue(actionNames.Count > 1);
            Assert.IsTrue(actionNames.Contains("Browse"));
        }

        /* ====================================================================================== Schema */

        /// <tab category="basic-concepts" article="schema" example="getSchema" />
        [TestMethod]
        [Description("Get schema")]
        public async Task Docs2_BasicConcepts_GetSchema()
        {
            // ACTION for doc
            /*<doc>*/
            string schema = await repository.GetResponseStringAsync(
                new ODataRequest {Path = "/Root", ActionName = "GetSchema"}, HttpMethod.Get, cancel);
            /*</doc>*/

            // ASSERT
            var replaced = schema.Substring(0, 50)
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\r", "")
                .Replace("\n", "");
            Assert.IsTrue(replaced.StartsWith("[{\"ContentTypeName\":"));
        }

        /// <tab category="basic-concepts" article="schema" example="getBinary" />
        [TestMethod]
        [Description("Change the schema")]
        public async Task Docs2_BasicConcepts_GetCtd()
        {
            // ACTION for doc
            /*<doc>*/
            var carContentType = await repository.LoadContentAsync(
                "/Root/System/Schema/ContentTypes/GenericContent/Car", cancel);

            string? ctd = null;
            await repository.ProcessWebResponseAsync(
                relativeUrl: $"/binaryhandler.ashx?nodeid={carContentType.Id}&propertyname=Binary",
                HttpMethod.Get,
                additionalHeaders: null,
                httpContent: null,
                responseProcessor: async (message, cancellation) =>
                {
                    ctd = await message.Content.ReadAsStringAsync(cancellation);
                },
                cancel);
            /*</doc>*/

            string? ctd2 = null;
            await repository.ProcessWebResponseAsync(
                relativeUrl: $"/binaryhandler.ashx?nodepath={carContentType.Path}&propertyname=Binary",
                HttpMethod.Get,
                additionalHeaders: null,
                httpContent: null,
                responseProcessor: async (message, cancellation) =>
                {
                    ctd2 = await message.Content.ReadAsStringAsync(cancellation);
                },
                cancel);

            // ASSERT
            Assert.IsNotNull(ctd);
            Assert.IsTrue(ctd.StartsWith("<?xml") || ctd.StartsWith("<ContentType"));
            Assert.IsTrue(ctd.Contains("name=\"Car\"") || ctd.Contains("name='Car'"));
            Assert.IsTrue(ctd.Trim().EndsWith("</ContentType>"));

            Assert.AreEqual(ctd, ctd2);
        }
    }
}
