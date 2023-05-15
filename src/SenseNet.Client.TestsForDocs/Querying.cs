using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Querying : ClientIntegrationTestBase
    {
        private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        IRepository repository => GetRepositoryCollection().GetRepositoryAsync("local", cancel)
            .GetAwaiter().GetResult();

        /* ====================================================================================== General */

        /// <tab category="querying" article="query" example="wildcard-search-single" />
        [TestMethod]
        [Description("Wildcard search 1")]
        public async Task Docs_Querying_Wildcard_QuestionMark()
        {
            await EnsureContentAsync("/Root/Content/truck", "Folder");
            await EnsureContentAsync("/Root/Content/trunk", "Folder");

            // ACTION for doc
            /*<doc>*/
            var result = await repository.QueryAsync(
                new QueryContentRequest { ContentQuery = "tru?k"}, cancel);

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");
            /*</doc>*/

            // ASSERT
            var actual = string.Join(", ", result.Select(c => c.Name).OrderBy(n => n).Distinct());
            Assert.AreEqual("truck, trunk", actual);
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
                await EnsureContentAsync("/Root/Content/truck", "Folder");
                await EnsureContentAsync("/Root/Content/trunk", "Folder");

                // ACTION for doc
                /*<doc>*/
                var result = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "Description:abbreviate~0.8"}, cancel);

                // foreach (dynamic content in result)
                //    Console.WriteLine($"{content.Id} {content.Name}");
                /*</doc>*/
                // Real test:
                result = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "Name:truck~0.5"}, cancel);

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
                var folder = await EnsureContentAsync("/Root/Content/folder1", "Folder");
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
                var content = await EnsureContentAsync("/Root/Content/(1-1)-2", "Folder");
                content["DisplayName"] = "(1+1):2";
                await content.SaveAsync(cancel);

                // ACTION for doc
                //var result = await Content.QueryAsync("InFolder:\"/Root/Content/IT/(1+1):2\"");
                /*<doc>*/
                var result1 = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "InFolder:'/Root/Content/IT/(1+1):2'"}, cancel);
                // or
                var result2 = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "InFolder:\"/Root/Content/IT/(1+1):2\""}, cancel);
                /*</doc>*/
                // Real test:
                result1 = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "DisplayName:'(1+1):2'" }, cancel);
                result2 = await repository.QueryAsync(
                    new QueryContentRequest {ContentQuery = "DisplayName:\"(1+1):2\"" }, cancel);

                // ASSERT
                var names1 = result1.Select(c => c["DisplayName"].ToString()).Distinct().ToArray();
                Assert.AreEqual("(1+1):2", names1.Single());
                var names2 = result1.Select(c => c["DisplayName"].ToString()).Distinct().ToArray();
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
            Assert.IsTrue(10 < result.Count);
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

        /// 
        [TestMethod]
        [Description("Query by a text field 1")]
        public async Task Docs_Querying_Name()
        {
            // ACTION for doc
            var result1 = await Content.QueryAsync("Name:BusinessPlan.docx");

            // foreach (dynamic content in result1)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query by a text field 2")]
        public async Task Docs_Querying_Description_Wildcard()
        {
            // ACTION for doc
            var result2 = await Content.QueryAsync("Description:*company*");

            //foreach (dynamic content in result2)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query by a number field")]
        public async Task Docs_Querying_NumberField()
        {
            // ACTION for doc

            var result = await Content.QueryAsync("TaskCompletion:<50");

            //foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query by a boolean field")]
        public async Task Docs_Querying_BooleanField()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("IsCritical:true");

            //foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query by choice field (localized value)")]
        public async Task Docs_Querying_ChoiceField_LocalizedValue()
        {
            //UNDONE: Not documented and not implemented
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query by choice field (value)")]
        public async Task Docs_Querying_ChoiceField_Value()
        {
            //UNDONE: Not documented and not implemented
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Fulltext Search */

        /// 
        [TestMethod]
        [Description("Fulltext search")]
        public async Task Docs_Querying_FullTextSearch()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Lorem");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Query by date */

        /// 
        [TestMethod]
        [Description("Query by an exact date")]
        public async Task Docs_Querying_Date_Day()
        {
            //UNDONE: Not documented and not implemented. REST:/OData.svc/Root/Content?query=CreationDate%3A'2019-02-15' ("Query by an exact date": don't work)
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_Date_Second()
        {
            //UNDONE: Not documented and not implemented. REST:/OData.svc/Root/Content?query=StartDate%3A'2019-02-15 09%3A30%3A00'
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query before or after a specific date")]
        public async Task Docs_Querying_Date_LessThan()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("CreationDate:<'2019-01-10'");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_Date_GreaterThan()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("ModificationDate:>'2019-01-10'");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query by a date range")]
        public async Task Docs_Querying_Date_Range_Inclusive()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("CreationDate:{'2010-08-30' TO '2010-10-30'}");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_Date_Range_Exclusive()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("CreationDate:['2010-08-30' TO '2010-10-30']");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_Date_Range_Mixed()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("CreationDate:['2010-08-30' TO '2010-10-30'}");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Querying with dynamic template parameters 1")]
        public async Task Docs_Querying_Date_Template_Yesterday()
        {
            //UNDONE: Not documented and not implemented. REST:/OData.svc/Root/Content?query=ModificationDate%3A@Yesterday@
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Querying with dynamic template parameters 2")]
        public async Task Docs_Querying_Date_Template_NextMonth()
        {
            //UNDONE: Not documented and not implemented. REST:/OData.svc/Root/Content?query=StartDate%3A>@NextMonth@
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Querying with dynamic template parameters 3")]
        public async Task Docs_Querying_Date_Template_PreviousYear()
        {
            //UNDONE: Not documented and not implemented. REST:/OData.svc/Root/Content?query=CreationDate%3A@PreviousYear@
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query by lifespan validity")]
        public async Task Docs_Querying_Date_LifespanOn()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("TypeIs:Article .LIFESPAN:ON");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Query by related content */

        /// 
        [TestMethod]
        [Description("Query by related content")]
        public async Task Docs_Querying_NestedQuery()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Manager:{{Name:'businesscat'}}");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Query by related content */

        /// 
        [TestMethod]
        [Description("Query by a type")]
        public async Task Docs_Querying_Type()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Type:DocumentLibrary");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Query by a type and its subtypes")]
        public async Task Docs_Querying_TypeIs()
        {
            // ACTION for doc

            var result = await Content.QueryAsync("TypeIs:Folder");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Ordering */

        /// 
        [TestMethod]
        [Description("Order by a field - lowest to highest")]
        public async Task Docs_Querying_OrderBy_Ascending()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Type:Folder .SORT:Name");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Order by a field - highest to lowest")]
        public async Task Docs_Querying_OrderBy_Descending()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Folder .REVERSESORT:Name");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Order by multiple fields")]
        public async Task Docs_Querying_OrderBy_MultipleField()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Type:Folder .SORT:Name .SORT:Index");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Order by multiple fields in different directions")]
        public async Task Docs_Querying_OrderBy_DifferendDirections()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Type:Folder .SORT:Name .REVERSESORT:Index");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Order by date")]
        public async Task Docs_Querying_OrderBy_Date()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("TypeIs:File .SORT:ModificationDate");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Paging */

        /// 
        [TestMethod]
        [Description("Limit result count")]
        public async Task Docs_Querying_Top()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Type:Folder .TOP:10");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Jump to page")]
        public async Task Docs_Querying_TopSkip()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Type:Folder .SKIP:3 .TOP:3");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Multiple predicates */

        /// 
        [TestMethod]
        [Description("Operators 1")]
        public async Task Docs_Querying_Operators_Or()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("apple OR melon");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /// 
        [TestMethod]
        [Description("Operators 2")]
        public async Task Docs_Querying_Operators_And()
        {
            // ALIGN
            await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder");
            await EnsureContentAsync("/Root/Content/IT/Document_Library/Munich", "Folder", c =>
            {
                c["Description"] = "Document library of IT";
            });

            /*
            // WARNING This code cannot run if the Ingredients field does not exist.
            // ACTION for doc
            var result = await Content.QueryAsync("Ingredients:apple AND Ingredients:melon");

            // foreach (dynamic content in result)
            //     Console.WriteLine($"{content.Id} {content.Name}");
            */

            // IMPROVED TEST
            // ACTION
            var c = await Content.LoadAsync("/Root/Content/IT/Document_Library");
            var x = c["Description"];

            // In memory index does not implement well any stemmer. See 'private List<string> GetValues(SnTerm field)' in InMemoryIndex.cs
            var result = await Content.QueryAsync("Description:*document* AND Description:*library*");
            // ASSERT
            Assert.IsTrue(result.Count() > 0);
        }

        /// 
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

        /// 
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

        /// 
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

        /// 
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
