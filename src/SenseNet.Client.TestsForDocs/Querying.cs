using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client.TestsForDocs.Infrastructure;
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
        public async Task Docs_Querying_Wildcard_QuestionMark()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/truck", "Folder", repository, cancel);
                await EnsureContentAsync("/Root/Content/trunk", "Folder", repository, cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "tru?k"}, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // ASSERT
                var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(n => n).Distinct());
                Assert.AreEqual("truck, trunk", actual);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] {"/Root/Content/truck", "/Root/Content/trunk"},
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query" example="wildcard-search-multiple" />
        [TestMethod]
        [Description("Wildcard search 2")]
        public async Task Docs_Querying_Wildcard_Asterisk()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "app*" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/
            // Real test:
            result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "adm*" }, cancel);

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
        public async Task Docs_Querying_FuzzySearch()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/truck", "Folder", repository, cancel);
                await EnsureContentAsync("/Root/Content/trunk", "Folder", repository, cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "Description:abbreviate~0.8"}, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/
                // Real test:
                result = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "Name:truck~0.799"}, cancel);

                // ASSERT
                Assert.IsTrue(result.Count > 1);
                var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(n => n).Distinct());
                Assert.AreEqual("truck, trunk", actual);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] {"/Root/Content/truck", "/Root/Content/trunk"},
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query" example="proximity-search" />
        [TestMethod]
        [Description("Proximity search")]
        public async Task Docs_Querying_ProximitySearch()
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
        public async Task Docs_Querying_Escaping1()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = @"Name:\(apps\) .AUTOFILTERS:OFF" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.AreEqual("(apps)", names.Single());
        }

        /// <tab category="querying" article="query" example="special-character-apostrophe" />
        [TestMethod]
        [Description("Escaping special characters 2")]
        public async Task Docs_Querying_Escaping2()
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
        public async Task Docs_Querying_QuickQuery()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Id:<42 .QUICK" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(10 < names.Length);
        }

        /* ====================================================================================== Query by Id or Path */

        /// <tab category="querying" article="query-by-id-path" example="byId" />
        [TestMethod]
        [Description("Query a content by its Id")]
        public async Task Docs_Querying_Id()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Id:1607" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/
            // Real test
            result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Id:6" }, cancel);

            // ASSERT
            var content = result.Single();
            Assert.AreEqual(6, content.Id);
            Assert.AreEqual("Visitor", content.Name);
        }

        /// <tab category="querying" article="query-by-id-path" example="byMultipleIds" />
        [TestMethod]
        [Description("Query multiple content by their Ids")]
        public async Task Docs_Querying_MoreId()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Id:(1607 1640 1645)" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/
            // Real test
            result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Id:(1 2 3)" }, cancel);

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.Id.ToString()).OrderBy(x => x));
            Assert.AreEqual("1, 2, 3", actual);
        }

        /// <tab category="querying" article="query-by-id-path" example="inFolder" />
        [TestMethod]
        [Description("Search in a folder")]
        public async Task Docs_Querying_InFolder()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "InFolder:'/Root/Content/IT/Document_Library/Calgary'" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Contains("BusinessPlan.docx"));
        }

        /// <tab category="querying" article="query-by-id-path" example="inTree" />
        [TestMethod]
        [Description("Search in a branch of the content tree")]
        public async Task Docs_Querying_InTree()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "InTree:'/Root/Content/IT/Document_Library'" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Contains("Document_Library"));
            Assert.IsTrue(names.Contains("BusinessPlan.docx"));
        }

        /* ====================================================================================== Query by a field */

        /// <tab category="querying" article="query-by-field" example="byShortText" />
        [TestMethod]
        [Description("Query by a text field 1")]
        public async Task Docs_Querying_Name()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Name:BusinessPlan.docx" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var name = result.Select(c => c.Name).Distinct().Single();
            Assert.AreEqual("BusinessPlan.docx", name);
        }

        /// <tab category="querying" article="query-by-field" example="byLongText" />
        [TestMethod]
        [Description("Query by a text field 2")]
        public async Task Docs_Querying_Description_Wildcard()
        {
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);
                folder["Description"] = "My company works here.";
                await folder.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "Description:*company*"}, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // ASSERT
                var name = result.Select(c => c.Name).Distinct().Single();
                Assert.AreEqual("Folder1", name);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-field" example="byNumber" />
        [TestMethod]
        [Description("Query by a number field")]
        public async Task Docs_Querying_NumberField()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TaskCompletion:<50" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/
            // Real test
            result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Id:<50" }, cancel);

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(10 < names.Length);
        }

        /// <tab category="querying" article="query-by-field" example="byBoolean" />
        [TestMethod]
        [Description("Query by a boolean field")]
        public async Task Docs_Querying_BooleanField()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "IsCritical:true" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/
            // Real test
            result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "IsActive:true" }, cancel);

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(2 < names.Length);
            Assert.IsTrue(names.Contains("Content"));
            Assert.IsTrue(names.Contains("IT"));
            Assert.IsTrue(names.Contains("Trash"));
        }

        /// <tab category="querying" article="query-by-field" example="byChoiceLocalized" />
        [TestMethod]
        [Description("Query by choice field (localized value)")]
        public async Task Docs_Querying_ChoiceField_LocalizedValue()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/Memos1", "MemoList", repository, cancel);
                var memo = await EnsureContentAsync("/Root/Content/Memos1/Memo1", "Memo", repository, cancel);
                memo["MemoType"] = "iaudit";
                await memo.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "MemoType:'Internal audit'" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

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
        public async Task Docs_Querying_ChoiceField_Value()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/Memos1", "MemoList", repository, cancel);
                var memo = await EnsureContentAsync("/Root/Content/Memos1/Memo1", "Memo", repository, cancel);
                memo["MemoType"] = "iaudit";
                await memo.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "MemoType:$iaudit" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

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

        /* ====================================================================================== Fulltext Search */

        /// <tab category="querying" article="query" example="fullText" />
        [TestMethod]
        [Description("Fulltext search")]
        public async Task Docs_Querying_FullTextSearch()
        {
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);
                folder["DisplayName"] = "-- Lorem ipsum dolor sit amet --";
                folder["Description"] = "-- Lorem ipsum dolor sit amet --";
                await folder.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "Lorem" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Contains("Folder1"));
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /* ====================================================================================== Query by date */

        /// <tab category="querying" article="query-by-date" example="byExactDate" />
        [TestMethod]
        [Description("Query by an exact date")]
        public async Task Docs_Querying_Date_Day()
        {
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);
                folder["CreationDate"] = new DateTime(2019, 2, 15, 0, 0, 0, DateTimeKind.Utc);
                await folder.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "CreationDate:'2019-02-15'" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Contains("Folder1"));
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-date" example="byExactDateTime" />
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_Date_Second()
        {
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);
                folder["CreationDate"] = new DateTime(2019, 2, 15, 9, 30, 0, DateTimeKind.Utc);
                await folder.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "StartDate:'2019-02-15 09:30:00'" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/
                // Real query
                result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "CreationDate:'2019-02-15 09:30:00'" }, cancel);

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Contains("Folder1"));
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-date" example="byDateBefore" />
        [TestMethod]
        [Description("Query before or after a specific date")]
        public async Task Docs_Querying_Date_LessThan()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "CreationDate:<'2019-01-10'" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
        }

        /// <tab category="querying" article="query-by-date" example="byDateAfter" />
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_Date_GreaterThan()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "ModificationDate:>'2019-01-10'" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
        }

        /// <tab category="querying" article="query-by-date" example="byExclusiveRange" />
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_Date_Range_Exclusive()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "CreationDate:['2010-08-30' TO '2010-10-30']" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // Real test
            var startCollection = await repository.QueryAsync(
                new QueryContentRequest
                {
                    ContentQuery = "InTree:/Root",
                    OrderBy = new[] { "CreationDate" },
                    Top = 1
                }, cancel);
            var endCollection = await repository.QueryAsync(
                new QueryContentRequest
                {
                    ContentQuery = "InTree:/Root",
                    OrderBy = new[] { "CreationDate desc" },
                    Top = 1
                }, cancel);
            var startDate = ((JValue)startCollection.Single()["CreationDate"]).Value<DateTime>();
            var endDate = ((JValue) endCollection.Single()["CreationDate"]).Value<DateTime>();
            var start = $"'{startDate.Year}-{startDate.Month}-{startDate.Day}'";
            var end = $"'{endDate.Year}-{endDate.Month}-{endDate.Day}'";
            result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = $"CreationDate:[{start} TO {end}]" }, cancel);

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
        }

        /// <tab category="querying" article="query-by-date" example="byInclusiveRange" />
        [TestMethod]
        [Description("Query by a date range")]
        public async Task Docs_Querying_Date_Range_Inclusive()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "CreationDate:{'2010-08-30' TO '2010-10-30'}" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // Real test
            var startCollection = await repository.QueryAsync(
                new QueryContentRequest
                {
                    ContentQuery = "InTree:/Root",
                    OrderBy = new[] { "CreationDate" },
                    Top = 1
                }, cancel);
            var endCollection = await repository.QueryAsync(
                new QueryContentRequest
                {
                    ContentQuery = "InTree:/Root",
                    OrderBy = new[] { "CreationDate desc" },
                    Top = 1
                }, cancel);
            var startDate = ((JValue)startCollection.Single()["CreationDate"]).Value<DateTime>();
            var endDate = ((JValue)endCollection.Single()["CreationDate"]).Value<DateTime>();
            var start = $"'{startDate.Year}-{startDate.Month}-{startDate.Day}'";
            var end = $"'{endDate.Year}-{endDate.Month}-{endDate.Day}'";
            result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = $"CreationDate:{{{start} TO {end}}}" }, cancel);

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
        }

        /// <tab category="querying" article="query-by-date" example="byMixedRange" />
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_Date_Range_Mixed()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "CreationDate:['2010-08-30' TO '2010-10-30'}" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // Real test
            var startCollection = await repository.QueryAsync(
                new QueryContentRequest
                {
                    ContentQuery = "InTree:/Root",
                    OrderBy = new[] { "CreationDate" },
                    Top = 1
                }, cancel);
            var endCollection = await repository.QueryAsync(
                new QueryContentRequest
                {
                    ContentQuery = "InTree:/Root",
                    OrderBy = new[] { "CreationDate desc" },
                    Top = 1
                }, cancel);
            var startDate = ((JValue)startCollection.Single()["CreationDate"]).Value<DateTime>();
            var endDate = ((JValue)endCollection.Single()["CreationDate"]).Value<DateTime>();
            var start = $"'{startDate.Year}-{startDate.Month}-{startDate.Day}'";
            var end = $"'{endDate.Year}-{endDate.Month}-{endDate.Day}'";
            result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = $"CreationDate:[{start} TO {end}}}" }, cancel);

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
        }

        /// <tab category="querying" article="query-by-date" example="byYesterday" />
        [TestMethod]
        [Description("Querying with dynamic template parameters 1")]
        public async Task Docs_Querying_Date_Template_Yesterday()
        {
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);
                var date = DateTime.UtcNow.AddDays(-1.0);
                folder["ModificationDate"] = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
                await folder.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "ModificationDate:@@Yesterday@@" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Length > 0);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-date" example="byNextMonth" />
        [TestMethod]
        [Description("Querying with dynamic template parameters 2")]
        public async Task Docs_Querying_Date_Template_NextMonth()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/Events1", "EventList", repository, cancel);
                var @event = await EnsureContentAsync("/Root/Content/Events1/Event1", "CalendarEvent", repository, cancel);
                var date = DateTime.UtcNow.AddMonths(1);
                @event["StartDate"] = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                @event["EndDate"] = new DateTime(date.Year, date.Month, 22, 0, 0, 0, DateTimeKind.Utc);
                await @event.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "StartDate:@@NextMonth@@" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Length > 0);
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
        public async Task Docs_Querying_Date_Template_PreviousYear()
        {
            try
            {
                var folder = await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);
                var date = DateTime.UtcNow.AddYears(-1);
                folder["CreationDate"] = new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                await folder.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "CreationDate:@@PreviousYear@@" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Length > 0);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-by-date" example="byLifespan" />
        [TestMethod]
        [Description("Query by lifespan validity")]
        public async Task Docs_Querying_Date_LifespanOn()
        {
            try
            {
                await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "TypeIs:Article .LIFESPAN:ON"}, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // Real test
                result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "TypeIs:Folder .LIFESPAN:ON" }, cancel);

                // ASSERT
                var names = result.Select(c => c.Name).Distinct().ToArray();
                Assert.IsTrue(names.Length > 0);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] {"/Root/Content/Folder1"},
                    true, cancel).ConfigureAwait(false);
            }
        }

        /* ====================================================================================== Query by related content */

        /// <tab category="querying" article="query-by-references" example="byManager" />
        [TestMethod]
        [Description("Query by related content")]
        public async Task Docs_Querying_NestedQuery()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Manager:{{Name:'businesscat'}}" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // Real test
            result = await repository.QueryAsync(
                new QueryContentRequest {ContentQuery = "ModifiedBy:{{Name:'admin'}}"}, cancel);

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
        }

        /* ====================================================================================== Query by type */

        /// <tab category="querying" article="query-by-type" example="byExactType" />
        [TestMethod]
        [Description("Query by a type")]
        public async Task Docs_Querying_Type()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:DocumentLibrary" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
        }

        /// <tab category="basic-concepts" article="query-by-type" example="byTypeFamily" />
        [TestMethod]
        [Description("Query by a type and its subtypes")]
        public async Task Docs_Querying_TypeIs()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:Folder" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 0);
        }

        /* ====================================================================================== Ordering */

        /// <tab category="querying" article="query-ordering" example="lowestToHighest" />
        [TestMethod]
        [Description("Order by a field - lowest to highest")]
        public async Task Docs_Querying_OrderBy_Ascending()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Folder .SORT:Name" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            Assert.IsTrue(result.Count() > 2);
            var names = result.Select(c => c.Name).Distinct().ToArray();
            for (var i = 1; i < names.Length; i++)
                Assert.IsTrue(String.Compare(names[i - 1], names[i], StringComparison.Ordinal) <= 0);
        }

        /// <tab category="querying" article="query-ordering" example="highestToLowest" />
        [TestMethod]
        [Description("Order by a field - highest to lowest")]
        public async Task Docs_Querying_OrderBy_Descending()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Folder .REVERSESORT:Name" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            Assert.IsTrue(result.Count() > 2);
            var names = result.Select(c => c.Name).Distinct().ToArray();
            for (var i = 1; i < names.Length; i++)
                Assert.IsTrue(String.Compare(names[i - 1], names[i], StringComparison.Ordinal) >= 0);
        }

        /// <tab category="querying" article="query-ordering" example="byMultipleFields" />
        [TestMethod]
        [Description("Order by multiple fields")]
        public async Task Docs_Querying_OrderBy_MultipleField()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Folder .SORT:Name .SORT:Index" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 2);
        }

        /// <tab category="querying" article="query-ordering" example="multipleFieldsAndDirections" />
        [TestMethod]
        [Description("Order by multiple fields in different directions")]
        public async Task Docs_Querying_OrderBy_DifferendDirections()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "Type:Folder .SORT:Name .REVERSESORT:Index" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 2);
        }

        /// <tab category="querying" article="query-ordering" example="byDate" />
        [TestMethod]
        [Description("Order by date")]
        public async Task Docs_Querying_OrderBy_Date()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:File .SORT:ModificationDate" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 2);
        }

        /* ====================================================================================== Paging */

        /// <tab category="querying" article="query-paging" example="top" />
        [TestMethod]
        [Description("Limit result count")]
        public async Task Docs_Querying_Top()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:Folder .TOP:10" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 2);
        }

        /// <tab category="querying" article="query-paging" example="skip-and-top" />
        [TestMethod]
        [Description("Jump to page")]
        public async Task Docs_Querying_TopSkip()
        {
            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "TypeIs:Folder .SKIP:3 .TOP:3" }, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var names = result.Select(c => c.Name).Distinct().ToArray();
            Assert.IsTrue(names.Length > 2);
        }

        /* ====================================================================================== Multiple predicates */

        /// <tab category="querying" article="query-multiple-predicates" example="or" />
        [TestMethod]
        [Description("Operators 1")]
        public async Task Docs_Querying_Operators_Or()
        {
            try
            {
                var folder1 = await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);
                folder1["Description"] = "cherry apple banana";
                await folder1.SaveAsync(cancel);
                var folder2 = await EnsureContentAsync("/Root/Content/Folder2", "Folder", repository, cancel);
                folder2["Description"] = "cherry melon walnut";
                await folder2.SaveAsync(cancel);
                var folder3 = await EnsureContentAsync("/Root/Content/Folder3", "Folder", repository, cancel);
                folder3["Description"] = "cherry pear banana";
                await folder3.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "melon OR apple" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // ASSERT
                var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(x => x).Distinct());
                Assert.AreEqual("Folder1, Folder2", actual);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1", "/Root/Content/Folder2", "/Root/Content/Folder3" },
                    true, cancel).ConfigureAwait(false);
            }
        }

        /// <tab category="querying" article="query-multiple-predicates" example="and" />
        [TestMethod]
        [Description("Operators 2")]
        public async Task Docs_Querying_Operators_And()
        {
            // In memory index does not implement well any stemmer.
            // See 'private List<string> GetValues(SnTerm field)' in InMemoryIndex.cs
            // Use this query for in memory index tests:
            //var result = await repository.QueryAsync(
            //    new QueryContentRequest { ContentQuery = "Description:*document* AND Description:*library*" }, cancel);
            try
            {
                var folder1 = await EnsureContentAsync("/Root/Content/Folder1", "Folder", repository, cancel);
                folder1["Description"] = "cherry apple banana";
                await folder1.SaveAsync(cancel);
                var folder2 = await EnsureContentAsync("/Root/Content/Folder2", "Folder", repository, cancel);
                folder2["Description"] = "cherry melon walnut";
                await folder2.SaveAsync(cancel);
                var folder3 = await EnsureContentAsync("/Root/Content/Folder3", "Folder", repository, cancel);
                folder3["Description"] = "cherry pear banana";
                await folder3.SaveAsync(cancel);

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "+EventType:Demo AND +EventType:Meeting" }, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/

                // Real test
                result = await repository.QueryAsync(
                    new QueryContentRequest { ContentQuery = "Description:'cherry' AND Description:'banana'" }, cancel);

                // ASSERT
                var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(x => x).Distinct());
                Assert.AreEqual("Folder1, Folder3", actual);
            }
            finally
            {
                await repository.DeleteContentAsync(
                    new[] { "/Root/Content/Folder1", "/Root/Content/Folder2", "/Root/Content/Folder3" },
                    true, cancel).ConfigureAwait(false);
            }

        }

        /// <tab category="querying" article="query-multiple-predicates" example="plus" />
        [TestMethod]
        [Description("Operators 3")]
        public async Task Docs_Querying_Operators_Must()
        {
            //UNDONE:-- BUG in the doc: 'AND' operator need to be deleted
            // ACTION for doc
            var result = await Content.QueryAsync("+EventType:Demo AND +EventType:Meeting");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="querying" article="query-multiple-predicates" example="not" />
        [TestMethod]
        [Description("Operators 4")]
        public async Task Docs_Querying_Operators_Not()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("apple NOT melon");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="querying" article="query-multiple-predicates" example="minus" />
        [TestMethod]
        [Description("Operators 5")]
        public async Task Docs_Querying_Operators_MustNot()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("upgrade -demo");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="querying" article="query-multiple-predicates" example="grouping" />
        [TestMethod]
        [Description("Grouping")]
        public async Task Docs_Querying_Operators_Grouping()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("(EventType:Demo AND EventType:Meeting) OR EventType:Deadline");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Query system content */

        /// 
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_AutofiltersOff()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Type:ContentType .AUTOFILTERS:OFF");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Template parameters */

        /// 
        [TestMethod]
        [Description("Using template parameters 1")]
        public async Task Docs_Querying_Template_CurrentUser()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("SharedWith:@@CurrentUser@@");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Using template parameters 2")]
        public async Task Docs_Querying_Template_Today()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("CalendarEvent AND StartDate:@@Today@@");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Templates with properties 1")]
        public async Task Docs_Querying_Template_NextWeekCurrentUser()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("+TypeIs:Task +DueDate:@@NextWeek@@ +AssignedTo:'@@CurrentUser@@'");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Templates with properties 2")]
        public async Task Docs_Querying_Template_PropertyChain()
        {
            //UNDONE:- the test is not implemented well: server returns http 500 if the current user's manager is null
            /*
            // ACTION for doc
            var result = await Content.QueryAsync("TypeIs:User +CreationDate:<@@CurrentUser.Manager.CreationDate@@");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");
            */

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Template expressions 1")]
        public async Task Docs_Querying_Template_MinusDays()
        {
            //UNDONE:--- BUG in doc: unwanted leading equal sign
            /*
            // ACTION for doc
            var result = await Content.QueryAsync("=CreationDate:>@@CurrentDate-5days@@");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");
            */

            // IMPROVED TEST
            // ACTION
            var result = await Content.QueryAsync("CreationDate:>@@CurrentDate-5days@@");

            // ASSERT
            Assert.IsTrue(result.Count() > 0);
        }

        /// 
        [TestMethod]
        [Description("Template expressions 2")]
        public async Task Docs_Querying_Template_AddDays()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("CreationDate:<@@CurrentDate.AddDays(-5)@@");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }
    }
}
