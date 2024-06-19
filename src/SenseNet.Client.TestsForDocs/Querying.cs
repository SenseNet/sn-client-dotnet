using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.TestsForDocs.Infrastructure;
using System.Linq;
// ReSharper disable RedundantAssignment
// ReSharper disable InconsistentNaming

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Querying : ClientIntegrationTestBase
    {
        private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        private IRepository repository => GetRepositoryCollection().GetRepositoryAsync("local", cancel)
            .GetAwaiter().GetResult();

        /* ====================================================================================== General */

        /// <tab category="querying" article="query" example="wildcard-search-single" />
        [TestMethod]
        [Description("Wildcard search 1")]
        public async Task Docs2_Querying_Wildcard_QuestionMark()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest {ContentQuery = "Type:Car AND Name:'AA?E642'" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Type:Car AND Name:'AA?E642'
            */

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(n => n).Distinct());
            Assert.AreEqual("AACE642, AASE642", actual);
        }

        /// <tab category="querying" article="query" example="wildcard-search-multiple" />
        [TestMethod]
        [Description("Wildcard search 2")]
        public async Task Docs2_Querying_Wildcard_Asterisk()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Name:'adm*'" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Name:'adm*'
            */

            // ASSERT
            Assert.IsTrue(result.Count > 2);
            var names = result.Select(c => c.Name).OrderBy(n => n).Distinct().ToArray();
            Assert.IsTrue(names.Contains("Admin"));
            Assert.IsTrue(names.Contains("Admin.png"));
            Assert.IsTrue(names.Contains("Administrators"));
        }

        /// <tab category="querying" article="query" example="fuzzy-search" />
        [TestMethod]
        [Description("Fuzzy search")]
        public async Task Docs2_Querying_FuzzySearch()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest {ContentQuery = "Name:AACE642~0.85" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Name:AACE642~0.85
            */

            // ASSERT
            Assert.IsTrue(result.Count > 1);
            var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(n => n).Distinct());
            Assert.AreEqual("AACE642, AASE642", actual);
        }

        /// <tab category="querying" article="query" example="proximity-search" />
        //UNDONE:Docs2: the test is not implemented
        [TestMethod]
        [Description("Proximity search")]
        public async Task Docs2_Querying_ProximitySearch()
        {
Assert.Inconclusive();
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/folder1", "Folder", repository, cancel);
                folder["DisplayName"] = "-- Lorem ipsum dolor sit amet --";
                folder["Description"] = "-- Lorem ipsum dolor sit amet --";
                await folder.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "Description:'Lorem amet'~3" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // ASSERT
                Assert.AreEqual("Folder1", result.First().Name);
            }
            finally
            {
                var c = await repository
                    .LoadContentAsync("/Root/Content/folder1", cancel);
                if (c != null)
                    await c.DeleteAsync(true, cancel);
            }
        }

        /// <tab category="querying" article="query" example="special-characters-escaping" />
        [TestMethod]
        [Description("Escaping special characters 1")]
        public async Task Docs2_Querying_Escaping1()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = @"Name:\(apps\) .AUTOFILTERS:OFF" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Name:\(apps\).AUTOFILTERS:OFF
            */
            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.AreEqual("(apps)", names.Single());
        }

        /// <tab category="querying" article="query" example="special-character-apostrophe" />
        [TestMethod]
        [Description("Escaping special characters 2")]
        public async Task Docs2_Querying_Escaping2()
        {
            try
            {
                // This name is not possible: (1+1):2. The forbidden characters are replaced.
                var content = await EnsureContentAsync("/Root/Content/(1-1)-2", "Folder", repository, cancel);
                content["DisplayName"] = "(1+1):2";
                await content.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result1 = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "DisplayName:'(1+1):2'" }, cancel);
                // or
                var result2 = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "DisplayName:\"(1+1):2\"" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?metadata=no&query=DisplayName:'(1+1):2'
                or
                GET https://localhost:44362/OData.svc/Root?metadata=no&query=DisplayName:"(1+1):2"
                */

                // ASSERT
                var names1 = result1.Select(c => c["DisplayName"].ToString()).Distinct().ToArray();
                Assert.AreEqual("(1+1):2", names1.Single());
                var names2 = result2.Select(c => c["DisplayName"].ToString()).Distinct().ToArray();
                Assert.AreEqual("(1+1):2", names2.Single());
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/(1-1)-2" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query" example="quick-query" />
        [TestMethod]
        [Description("Quick queries")]
        public async Task Docs2_Querying_QuickQuery()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Car .QUICK" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Type:Car .QUICK
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(8 <= names.Length);
        }

        /* ====================================================================================== Fulltext Search */

        /// <tab category="querying" article="query" example="fullText" />
        [TestMethod]
        [Description("Fulltext search")]
        public async Task Docs2_Querying_FullTextSearch()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "California" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=California
            */

            // ASSERT
            var displayNames = result.Select(c => c.DisplayName).Distinct().ToArray();
            Assert.IsTrue(displayNames.Contains("Ferrari California"));
        }

        /* ====================================================================================== Query by Id or Path */

        /// <tab category="querying" article="query-by-id-path" example="byId" />
        [TestMethod]
        [Description("Query a content by its Id")]
        public async Task Docs2_Querying_Id()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Id:6" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Id:6
            */

            // ASSERT
            var content = result.Single();
            Assert.AreEqual(6, content.Id);
            Assert.AreEqual("Visitor", content.Name);
        }

        /// <tab category="querying" article="query-by-id-path" example="byMultipleIds" />
        [TestMethod]
        [Description("Query multiple content by their Ids")]
        public async Task Docs2_Querying_MoreId()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Id:(7 8 11)" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Id:(7 8 11)
            */

            // ASSERT
            var actual = string.Join(", ", result.OrderBy(x => x.Id).Select(c => c.Id.ToString()));
            Assert.AreEqual("7, 8, 11", actual);
            var names = string.Join(", ", result.Select(c => c.Name).OrderBy(x => x));
            Assert.AreEqual("Administrators, Everyone, Operators", names);
        }

        /// <tab category="querying" article="query-by-id-path" example="inFolder" />
        [TestMethod]
        [Description("Search in a folder")]
        public async Task Docs2_Querying_InFolder()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "InFolder:'/Root/Content/Cars'" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=InFolder:'/Root/Content/Cars'
            */

            // ASSERT
            var parentPaths = result.Select(c => c.ParentPath).Distinct().ToArray();
            Assert.AreEqual("/Root/Content/Cars", parentPaths.Single());
        }

        /// <tab category="querying" article="query-by-id-path" example="inTree" />
        [TestMethod]
        [Description("Search in a branch of the content tree")]
        public async Task Docs2_Querying_InTree()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "InTree:'/Root/Content/Cars'" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=InTree:'/Root/Content/Cars'
            */

            // ASSERT
            var types = result.Select(c => c.Type).Distinct().OrderBy(x => x).ToArray();
            Assert.AreEqual("Car, Folder", string.Join(", ", types));
        }

        /* ====================================================================================== Query by a field */

        /// <tab category="querying" article="query-by-field" example="byShortText" />
        [TestMethod]
        [Description("Query by a text field 1")]
        public async Task Docs2_Querying_Name()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Color:Yellow" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Color:Yellow
            */

            // ASSERT
            var name = result.Select(c => c.DisplayName).Distinct().First();
            Assert.AreEqual("Fiat 126", name);
        }

        /// <tab category="querying" article="query-by-field" example="byLongText" />
        [TestMethod]
        [Description("Query by a text field 2")]
        public async Task Docs2_Querying_Description_Wildcard()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest {ContentQuery = "DisplayName:*Astra*"}, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=DisplayName:*Astra*
            */

            // ASSERT
            var displayNames = result.Select(c => c.DisplayName).Distinct();
            Assert.IsTrue(displayNames.Contains("Opel Astra H"));
        }

        /// <tab category="querying" article="query-by-field" example="byNumber" />
        [TestMethod]
        [Description("Query by a number field")]
        public async Task Docs2_Querying_NumberField()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Price:<1000000" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Price:<1000000
            */

            // ASSERT
            var prices = result.Select(c => ((JValue) c["Price"]).Value<decimal>()).Distinct().OrderBy(x => x)
                .ToArray();
            Assert.AreEqual(120000m, prices.First());
        }

        /// <tab category="querying" article="query-by-field" example="byBoolean" />
        [TestMethod]
        [Description("Query by a boolean field")]
        public async Task Docs2_Querying_BooleanField()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "InFolder:/Root/Content/Cars AND IsFolder:true" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=InFolder:/Root/Content/Cars AND IsFolder:true
            */

            // ASSERT
            var name = result.Select(c => c.Name).Distinct().First();
            Assert.AreEqual("out-of-order", name);
        }

        /// <tab category="querying" article="query-by-field" example="byChoiceLocalized" />
        [TestMethod]
        [Description("Query by choice field (localized value)")]
        public async Task Docs2_Querying_ChoiceField_LocalizedValue()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/Memos1", "MemoList", repository, cancel);
                var memo = await EnsureContentAsync("/Root/Content/Memos1/Memo1", "Memo", repository, cancel);
                memo["MemoType"] = "iaudit";
                await memo.SaveAsync(cancel);

                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "MemoType:'Internal audit'" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?metadata=no&query=MemoType:'Internal audit'
                */

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Contains("Memo1"));
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Memos1" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-field" example="byChoice" />
        [TestMethod]
        [Description("Query by choice field (value)")]
        public async Task Docs2_Querying_ChoiceField_Value()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest {ContentQuery = "Style:$roadster"}, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Style:$roadster
            */

            // ASSERT
            var displayName = result.Select(c => c.DisplayName).Distinct().Single();
            Assert.AreEqual("Ferrari California", displayName);
        }

        /* ====================================================================================== Query by date */

        /// <tab category="querying" article="query-by-date" example="byExactDate" />
        [TestMethod]
        [Description("Query by an exact date")]
        public async Task Docs2_Querying_Date_Day()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "StartingDate:'2021-04-22'" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=StartingDate:'2021-04-22'
            */

            // ASSERT
            var displayName = result.Select(c => c.DisplayName).Distinct().First();
            Assert.AreEqual("Skoda Octavia", displayName);
        }

        /// <tab category="querying" article="query-by-date" example="byExactDateTime" />
        [TestMethod]
        [Description("")]
        public async Task Docs2_Querying_Date_Second()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "StartingDate:'2023-12-29 09:30:00'" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=StartingDate:'2023-12-29 09:30:00'
            */

            // ASSERT
            var displayName = result.Select(c => c.DisplayName).Distinct().First();
            Assert.AreEqual("Nissan GTR R32", displayName);
        }

        /// <tab category="querying" article="query-by-date" example="byDateBefore" />
        [TestMethod]
        [Description("Query before or after a specific date")]
        public async Task Docs2_Querying_Date_LessThan()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "StartingDate:<'2019-01-10'" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=StartingDate:<'2019-01-10'
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
            var types = result.Select(c => c.Type).Distinct().ToArray();
            Assert.AreEqual("Car", types.Single());
        }

        /// <tab category="querying" article="query-by-date" example="byDateAfter" />
        [TestMethod]
        [Description("Query before or after a specific date")]
        public async Task Docs2_Querying_Date_GreaterThan()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "StartingDate:>'2019-01-10'" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=StartingDate:>'2019-01-10'
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
            var types = result.Select(c => c.Type).Distinct().ToArray();
            Assert.AreEqual("Car", types.Single());
        }

        /// <tab category="querying" article="query-by-date" example="byExclusiveRange" />
        [TestMethod]
        [Description("")]
        public async Task Docs2_Querying_Date_Range_Exclusive()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "StartingDate:['2010-01-01' TO '2016-01-01']" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=StartingDate:['2010-01-01' TO '2016-01-01']
            */

            // ASSERT
            var displayNames = result.Select(c => c.DisplayName).Distinct().ToArray();
            Assert.AreEqual("Opel Astra H, Renault Thalia", string.Join(", ", displayNames));
        }

        /// <tab category="querying" article="query-by-date" example="byInclusiveRange" />
        [TestMethod]
        [Description("Query by a date range")]
        public async Task Docs2_Querying_Date_Range_Inclusive()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "StartingDate:{'2010-01-01' TO '2016-01-01'}" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=StartingDate:{'2010-01-01' TO '2016-01-01'}
            */

            // ASSERT
            var displayNames = result.Select(c => c.DisplayName).Distinct().ToArray();
            Assert.AreEqual("Opel Astra H, Renault Thalia", string.Join(", ", displayNames));
        }

        /// <tab category="querying" article="query-by-date" example="byMixedRange" />
        [TestMethod]
        [Description("")]
        public async Task Docs2_Querying_Date_Range_Mixed()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "StartingDate:['2010-01-01' TO '2016-01-01'}" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=StartingDate:['2010-01-01' TO '2016-01-01'}
            */

            // ASSERT
            var displayNames = result.Select(c => c.DisplayName).Distinct().ToArray();
            Assert.AreEqual("Opel Astra H, Renault Thalia", string.Join(", ", displayNames));
        }

        /// <tab category="querying" article="query-by-date" example="byYesterday" />
        [TestMethod]
        [Description("Querying with dynamic template parameters 1")]
        public async Task Docs2_Querying_Date_Template_Yesterday()
        {
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/Folder1_Yesterday", "Folder", repository, cancel);
                var date = DateTime.UtcNow.AddDays(-1.0);
                folder["ModificationDate"] = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
                await folder.SaveAsync(cancel);

                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "ModificationDate:@@Yesterday@@" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?query=ModificationDate:@@Yesterday@@
                */

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Contains("Folder1_Yesterday"));
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1_Yesterday" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-date" example="byNextMonth" />
        [TestMethod]
        [Description("Querying with dynamic template parameters 2")]
        public async Task Docs2_Querying_Date_Template_NextMonth()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/Events1", "EventList", repository, cancel);
                var @event = await EnsureContentAsync("/Root/Content/Events1/Event1_NextMonth", "CalendarEvent", repository, cancel);
                var date = DateTime.UtcNow.AddMonths(1);
                @event["StartDate"] = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                @event["EndDate"] = new DateTime(date.Year, date.Month, 22, 0, 0, 0, DateTimeKind.Utc);
                await @event.SaveAsync(cancel);

                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "StartDate:@@NextMonth@@" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?query=StartDate:@@NextMonth@@
                */

                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Contains("Event1_NextMonth"));
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Events1" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-date" example="byPreviousYear" />
        [TestMethod]
        [Description("Querying with dynamic template parameters 3")]
        public async Task Docs2_Querying_Date_Template_PreviousYear()
        {
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/Folder1_PreviousYear", "Folder", repository, cancel);
                var date = DateTime.UtcNow.AddYears(-1);
                folder["CreationDate"] = new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                await folder.SaveAsync(cancel);

                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "CreationDate:@@PreviousYear@@" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?query=CreationDate:@@PreviousYear@@
                */

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Contains("Folder1_PreviousYear"));
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1_PreviousYear" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-date" example="byLifespan" />
        [TestMethod]
        [Description("Query by lifespan validity")]
        public async Task Docs2_Querying_Date_LifespanOn()
        {
            await EnsureContentAsync("/Root/Content/Articles", "Folder", repository, cancel);
            await EnsureContentAsync("/Root/Content/Articles/Article1", "Article", c =>
            {
                var now = DateTime.UtcNow;
                c["EnableLifespan"] = true;
                c["ValidFrom"] = now.AddDays(-3);
                c["ValidTill"] = now.AddDays(-1);
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Articles/Article2", "Article", c =>
            {
                var now = DateTime.UtcNow;
                c["EnableLifespan"] = true;
                c["ValidFrom"] = now.AddDays(-1);
                c["ValidTill"] = now.AddDays(1);
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Articles/Article3", "Article", c =>
            {
                var now = DateTime.UtcNow;
                c["EnableLifespan"] = true;
                c["ValidFrom"] = now.AddDays(1);
                c["ValidTill"] = now.AddDays(3);
            }, repository, cancel);
            try
            {
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "TypeIs:Article .LIFESPAN:ON"}, cancel);
                /*</doc>*/
                /*RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?metadata=no&query=TypeIs:Article .LIFESPAN:ON
                */

                // ASSERT
                var names = string.Join(", ", result.Select(c => c.Name).OrderBy(x => x));
                Assert.AreEqual("Article2", names);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Articles" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /* ====================================================================================== Query by related content */

        /// <tab category="querying" article="query-by-references" example="byManager" />
        [TestMethod]
        [Description("Query by related content")]
        public async Task Docs2_Querying_NestedQuery()
        {
            var jjohnson = await repository.LoadContentAsync("/Root/IMS/Public/jjohnson", cancel);
            var content = await repository.LoadContentAsync("/Root/Content/Cars/OT6578", cancel);
            content["ModifiedBy"] = jjohnson.Path;
            await content.SaveAsync(cancel);

            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "ModifiedBy:{{Name:'jjohnson'}}" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=ModifiedBy:{{Name:'jjohnson'}}
            */

            // ASSERT
            var displayName = result.Select(c => c.DisplayName).Distinct().First();
            Assert.AreEqual("Toyota AE86", displayName);
        }

        /* ====================================================================================== Query by type */

        /// <tab category="querying" article="query-by-type" example="byExactType" />
        [TestMethod]
        [Description("Query by a type")]
        public async Task Docs2_Querying_Type()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Car" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Type:Car
            */

            // ASSERT
            Assert.IsTrue(result.Count() > 8);
            var types = result.Select(c => c.Type).Distinct().ToArray();
            Assert.AreEqual("Car", types.Single());
        }

        /// <tab category="basic-concepts" article="query-by-type" example="byTypeFamily" />
        [TestMethod]
        [Description("Query by a type and its subtypes")]
        public async Task Docs2_Querying_TypeIs()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:Folder" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=TypeIs:Folder
            */

            // ASSERT
            var types = result.Select(c => c.Type).Distinct().OrderBy(x => x).ToArray();
            Assert.IsTrue(types.Contains("Folder"));
            Assert.IsTrue(types.Contains("DocumentLibrary"));
            Assert.IsTrue(types.Contains("Workspace"));
        }

        /* ====================================================================================== Ordering */

        /// <tab category="querying" article="query-ordering" example="lowestToHighest" />
        [TestMethod]
        [Description("Order by a field - lowest to highest")]
        public async Task Docs2_Querying_OrderBy_Ascending()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Car .SORT:Name" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Type:Car .SORT:Name
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(result.Count() > 2);
            for (var i = 1; i < names.Length; i++)
                Assert.IsTrue(String.Compare(names[i - 1], names[i], StringComparison.Ordinal) <= 0);
        }

        /// <tab category="querying" article="query-ordering" example="highestToLowest" />
        [TestMethod]
        [Description("Order by a field - highest to lowest")]
        public async Task Docs2_Querying_OrderBy_Descending()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Car .REVERSESORT:Name" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Type:Car .REVERSESORT:Name
            */

            // ASSERT
            Assert.IsTrue(result.Count() > 2);
            var names = result.Select(c => c.Name).Distinct().ToArray();
            for (var i = 1; i < names.Length; i++)
                Assert.IsTrue(String.Compare(names[i - 1], names[i], StringComparison.Ordinal) >= 0);
        }

        /// <tab category="querying" article="query-ordering" example="byMultipleFields" />
        [TestMethod]
        [Description("Order by multiple fields")]
        public async Task Docs2_Querying_OrderBy_MultipleField()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Car .SORT:Color .SORT:Name" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Type:Car .SORT:Color .SORT:Name
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 2);
        }

        /// <tab category="querying" article="query-ordering" example="multipleFieldsAndDirections" />
        [TestMethod]
        [Description("Order by multiple fields in different directions")]
        public async Task Docs2_Querying_OrderBy_DifferendDirections()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Car .REVERSESORT:Color .SORT:Name" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Type:Car .REVERSESORT:Color .SORT:Name
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 2);
        }

        /// <tab category="querying" article="query-ordering" example="byDate" />
        [TestMethod]
        [Description("Order by date")]
        public async Task Docs2_Querying_OrderBy_Date()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Car .SORT:StartingDate" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Type:Car .SORT:StartingDate
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 2);
        }

        /* ====================================================================================== Paging */

        /// <tab category="querying" article="query-paging" example="top" />
        [TestMethod]
        [Description("Limit result count")]
        public async Task Docs2_Querying_Top()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:Car .TOP:5" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=TypeIs:Car .TOP:5
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.AreEqual(5, names.Length);
        }

        /// <tab category="querying" article="query-paging" example="skip-and-top" />
        [TestMethod]
        [Description("Jump to page")]
        public async Task Docs2_Querying_TopSkip()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:Car .SKIP:3 .TOP:3" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=TypeIs:Car .SKIP:3 .TOP:3
            */

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.AreEqual(3, names.Length);
        }

        /* ====================================================================================== Multiple predicates */

        /// <tab category="querying" article="query-multiple-predicates" example="or" />
        [TestMethod]
        [Description("Operators 1")]
        public async Task Docs2_Querying_Operators_Or()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest {ContentQuery = "Color:White OR Color:Red"}, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Color:White OR Color:Red
            */

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c["Color"].ToString()).OrderBy(x => x).Distinct());
            Assert.AreEqual("Red, White", actual);
        }

        /// <tab category="querying" article="query-multiple-predicates" example="and" />
        [TestMethod]
        [Description("Operators 2")]
        public async Task Docs2_Querying_Operators_And()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Color:White AND Style:Sedan" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Color:White AND Style:Sedan
            */

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.DisplayName).OrderBy(x => x).Distinct());
            Assert.AreEqual("Toyota AE86", actual);
        }

        /// <tab category="querying" article="query-multiple-predicates" example="plus" />
        [TestMethod]
        [Description("Operators 3")]
        public async Task Docs2_Querying_Operators_Must()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "+Color:White +Style:Sedan" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=+Color:White +Style:Sedan
            */

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.DisplayName).OrderBy(x => x).Distinct());
            Assert.AreEqual("Toyota AE86", actual);
        }

        /// <tab category="querying" article="query-multiple-predicates" example="not" />
        [TestMethod]
        [Description("Operators 4")]
        public async Task Docs2_Querying_Operators_Not()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Color:White AND NOT Style:Sedan" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=Color:White AND NOT Style:Sedan
            */

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.DisplayName).OrderBy(x => x).Distinct());
            Assert.AreEqual("Skoda Octavia", actual);
        }

        /// <tab category="querying" article="query-multiple-predicates" example="minus" />
        [TestMethod]
        [Description("Operators 5")]
        public async Task Docs2_Querying_Operators_MustNot()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "+Color:White -Style:Sedan" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?query=+Color:White -Style:Sedan
            */

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.DisplayName).OrderBy(x => x).Distinct());
            Assert.AreEqual("Skoda Octavia", actual);
        }

        /// <tab category="querying" article="query-multiple-predicates" example="grouping" />
        [TestMethod]
        [Description("Grouping")]
        public async Task Docs2_Querying_Operators_Grouping()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Color:White AND (Style:Sedan OR Price:<10000000)" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?metadata=no&query=Color:White AND (Style:Sedan OR Price:<10000000)
            */

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.DisplayName).OrderBy(x => x).Distinct());
            Assert.AreEqual("Skoda Octavia, Toyota AE86", actual);
        }

        /* ====================================================================================== Query system content */

        /// 
        [TestMethod]
        [Description("Query system content")]
        public async Task Docs2_Querying_AutofiltersOff()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:Folder .AUTOFILTERS:OFF" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?metadata=no&query=TypeIs:Folder .AUTOFILTERS:OFF
            */

            // ASSERT
            var types = result.Select(c => c.Type).Distinct().ToArray();
            Assert.IsTrue(types.Contains("Folder"));
            Assert.IsTrue(types.Contains("SystemFolder"));
        }

        /* ====================================================================================== Template parameters */

        /// <tab category="querying" article="query-template-parameters" example="sharedWithCurrentUser" />
        [TestMethod]
        [Description("Using template parameters 1")]
        public async Task Docs2_Querying_Template_CurrentUser()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Car AND ModifiedBy:@@CurrentUser@@" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?metadata=no&query=Type:Car AND ModifiedBy:@@CurrentUser@@
            */

            // ASSERT
            Assert.IsTrue(result.Count() > 8);
        }

        /// <tab category="querying" article="query-template-parameters" example="todaysEvents" />
        [TestMethod]
        [Description("Using template parameters 2")]
        public async Task Docs2_Querying_Template_Today()
        {
            await EnsureContentAsync("/Root/Content/Tasks", "TaskList", repository, cancel);
            await EnsureContentAsync("/Root/Content/Tasks/Task1", "Task", c =>
            {
                c["StartDate"] = DateTime.Now.AddDays(-2);
                c["DueDate"] = DateTime.Now.AddDays(2);
                c["AssignedTo"] = 1;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Tasks/Task2", "Task", c =>
            {
                c["StartDate"] = DateTime.Now;
                c["DueDate"] = DateTime.Now.AddDays(7);
                c["AssignedTo"] = 1;
            }, repository, cancel);
            try
            {
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "TypeIs:Task AND StartDate:>@@Today@@" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?metadata=no&query=TypeIs:Task AND StartDate:>@@Today@@
                */

                // ASSERT
                var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(x => x).Distinct());
                Assert.AreEqual("Task2", actual);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Tasks", true, cancel);
            }
        }

        /// <tab category="querying" article="query-template-parameters" example="nextWeekTasksOfAUser" />
        [TestMethod]
        [Description("Templates with properties 1")]
        public async Task Docs2_Querying_Template_NextWeekCurrentUser()
        {
            await EnsureContentAsync("/Root/Content/Tasks", "TaskList", repository, cancel);
            await EnsureContentAsync("/Root/Content/Tasks/Task1", "Task", c =>
            {
                c["StartDate"] = DateTime.Now.AddDays(1);
                c["DueDate"] = DateTime.Now.AddDays(7);
                c["AssignedTo"] = 1;
            }, repository, cancel);
            try
            {
                /*<doc>*/
                var result = await repository.QueryAsync(new QueryContentRequest
                    { ContentQuery = "+TypeIs:Task +DueDate:>@@NextWeek@@ +AssignedTo:'@@CurrentUser@@'" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?metadata=no&query=+TypeIs:Task +DueDate:@@NextWeek@@ +AssignedTo:'@@CurrentUser@@'
                */

                // ASSERT
                var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(x => x).Distinct());
                Assert.AreEqual("Task1", actual);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Tasks", true, cancel);
            }
        }

        /// <tab category="querying" article="query-template-parameters" example="chainingProperties" />
        //UNDONE:Docs2:- the test is not implemented well: server returns http 500 if the current user's manager is null
        [TestMethod]
        [Description("Templates with properties 2")]
        public async Task Docs2_Querying_Template_PropertyChain()
        {
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:Task +CreationDate:<@@CurrentUser.Manager.CreationDate@@" }, cancel);
            /*</doc>*/
            /* RAW REQUEST:
            GET https://localhost:44362/OData.svc/Root?metadata=no&query=TypeIs:User +CreationDate:<@@CurrentUser.Manager.CreationDate@@
            */

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.DisplayName).OrderBy(x => x).Distinct());
            Assert.AreEqual("____", actual);
        }

        /// <tab category="querying" article="query-template-parameters" example="template-expressions" />
        [TestMethod]
        [Description("Template expressions 1")]
        public async Task Docs2_Querying_Template_MinusDays()
        {
            await EnsureContentAsync("/Root/Content/Tasks", "TaskList", repository, cancel);
            await EnsureContentAsync("/Root/Content/Tasks/Task1", "Task", c =>
            {
                c["StartDate"] = DateTime.Now.AddDays(-2);
                c["DueDate"] = DateTime.Now.AddDays(2);
                c["AssignedTo"] = 1;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Tasks/Task2", "Task", c =>
            {
                c["StartDate"] = DateTime.Now.AddDays(-7);
                c["DueDate"] = DateTime.Now;
                c["AssignedTo"] = 1;
            }, repository, cancel);
            try
            {
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "TypeIs:Task AND StartDate:<@@CurrentDate-5days@@" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?metadata=no&query=TypeIs:Task AND StartDate:>@@CurrentDate-5days@@
                */

                // ASSERT
                var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(x => x).Distinct());
                Assert.AreEqual("Task2", actual);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Tasks", true, cancel);
            }
        }

        /// <tab category="querying" article="query-template-parameters" example="template-expressions-methodlike" />
        [TestMethod]
        [Description("Template expressions 2")]
        public async Task Docs2_Querying_Template_AddDays()
        {
            await EnsureContentAsync("/Root/Content/Tasks", "TaskList", repository, cancel);
            await EnsureContentAsync("/Root/Content/Tasks/Task1", "Task", c =>
            {
                c["StartDate"] = DateTime.Now.AddDays(-2);
                c["DueDate"] = DateTime.Now.AddDays(2);
                c["AssignedTo"] = 1;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Tasks/Task2", "Task", c =>
            {
                c["StartDate"] = DateTime.Now.AddDays(-7);
                c["DueDate"] = DateTime.Now;
                c["AssignedTo"] = 1;
            }, repository, cancel);
            try
            {
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "TypeIs:Task AND StartDate:<@@CurrentDate.AddDays(-5)@@" }, cancel);
                /*</doc>*/
                /* RAW REQUEST:
                GET https://localhost:44362/OData.svc/Root?metadata=no&query=TypeIs:Task AND StartDate:<@@CurrentDate.AddDays(-5)@@
                */

                // ASSERT
                var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(x => x).Distinct());
                Assert.AreEqual("Task2", actual);
            }
            finally
            {
                await repository.DeleteContentAsync("/Root/Content/Tasks", true, cancel);
            }
        }
    }
}
