using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Querying : ClientIntegrationTestBase
    {
        [TestMethod]
        [Description("Wildcard search 1")]
        public async Task Docs_Querying_Wildcard_QuertionMark()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("query=tru?k");

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Wildcard search 2")]
        public async Task Docs_Querying_Wildcard_Asterisk()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("query=app*");

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        [TestMethod]
        [Description("Fuzzy search")]
        public async Task Docs_Querying_FuzzySearch()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Description:abbreviate~0.8");

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Proximity search")]
        public async Task Docs_Querying_ProximitySearch()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Description:'Lorem amet'~3");

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Escaping special characters 1")]
        public async Task Docs_Querying_Escaping1()
        {
            // ACTION for doc
            var result = await Content.QueryAsync(@"Name:\(apps\) .AUTOFILTERS:OFF");

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Escaping special characters 2")]
        public async Task Docs_Querying_Escaping2()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("InFolder:\"/Root/Content/IT/(1+1):2\"");

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Quick queries")]
        public async Task Docs_Querying_QuickQuery()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Id:<42 .QUICK");

            // foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Query by Id or Path */

        [TestMethod]
        [Description("Query a content by its Id")]
        public async Task Docs_Querying_Id()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Id:1607");

            // foreach (dynamic content in result)
            //     Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Query multiple content by their Ids")]
        public async Task Docs_Querying_MoreId()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("Id:(1607 1640 1645)");

            // foreach (dynamic content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Search in a folder")]
        public async Task Docs_Querying_InFolder()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("InFolder:'/Root/Content/IT/Document_Library/Calgary'");

            // foreach (dynamic content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Search in a branch of the content tree")]
        public async Task Docs_Querying_Intree()
        {
            // ACTION for doc
            var result = await Content.QueryAsync("InTree:'/Root/Content/IT/Document_Library'");

            // foreach (dynamic content in result)
            //    Console.WriteLine(content.Name);

            // ASSERT
            Assert.Inconclusive();
        }

        /* ====================================================================================== Query by a field */

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
        [TestMethod]
        [Description("Query by choice field (localized value)")]
        public async Task Docs_Querying_ChoiceField_LocalizedValue()
        {
            throw new NotImplementedException();
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }
        [TestMethod]
        [Description("Query by choice field (value)")]
        public async Task Docs_Querying_ChoiceField_Value()
        {
            throw new NotImplementedException();
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }

        /*
        [TestMethod]
        [Description("")]
        public async Task Docs_Querying_()
        {
           // ACTION for doc

           // ASSERT
           Assert.Inconclusive();
        }
        */
    }
}
