using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Client;

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
        /*
        [TestMethod]
        [Description("")]
        public async Task IntT_BasicConcepts_()
        {
            // ACTION for doc

            // ASSERT
        }
        */
    }
}
