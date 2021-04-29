using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client;
using SenseNet.Diagnostics;

namespace SenseNet.Clients.IntegrationTests
{
    [TestClass]
    public class BasicConcepts : ClientIntegrationTestBase
    {
        [TestMethod]
        [Description("Get a single content by Id")]
        public async Task IntT_BasicConcepts_GetSingleContentById()
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
        public async Task IntT_BasicConcepts_GetSingleContentByPath()
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
        public async Task IntT_BasicConcepts_GetSingleProperty()
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
        public async Task IntT_BasicConcepts_GetSinglePropertyValue()
        {
            var url = ClientContext.Current.Server.Url;

            var response =
                // ACTION for doc
                //UNDONE:- Feature request: Content.GetPropertyValueAsync: await Content.GetPropertyValueAsync("/Root/IMS", "DisplayName");
                await RESTCaller.GetResponseStringAsync(new Uri(url + "/OData.svc/Root/Content/('IT')/DisplayName/$value"));

            // ASSERT
            Assert.AreEqual("IT", response.RemoveWhitespaces());
        }
        //[TestMethod]
        //[Description("Accessing binary stream")]
        //public async Task IntT_BasicConcepts_GetBinaryStream()
        //{
        //    // ACTION for doc

        //    // ASSERT
        //}
        [TestMethod]
        [Description("Children of a content (collection)")]
        public async Task IntT_BasicConcepts_GetChildren()
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
        public async Task IntT_BasicConcepts_ChildrenCount()
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
        public async Task IntT_BasicConcepts_ChildrenInlineCount()
        {
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
        [TestMethod]
        [Description("Select")]
        public async Task IntT_BasicConcepts_Select()
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
        [Description("Expand")]
        public async Task IntT_BasicConcepts_Expand_CreatedBy()
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
        [Description("Expand")]
        public async Task IntT_BasicConcepts_Expand_CreatedByCreatedBy()
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
        [Description("")]
        public async Task IntT_BasicConcepts_Expand_CreatedByName()
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
        [Description("")]
        public async Task IntT_BasicConcepts_Expand_AllowedChildTypes()
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
        [Description("")]
        public async Task IntT_BasicConcepts_Expand_Actions()
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
        /*
        [TestMethod]
        [Description("")]
        public async Task IntT_BasicConcepts_()
        {
            // ACTION for doc

            // ASSERT
            Assert.Inconclusive();
        }
        */
    }
}
