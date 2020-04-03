using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class ODataRequestTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NoServer_Error()
        {
            ClientContext.Current.RemoveAllServers();

            // this will fail, because there is no server configured or provided
            var _ = new ODataRequest();
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ServerWithoutUrl_Error()
        {
            ClientContext.Current.RemoveAllServers();

            // this will fail, because the server does not contain a url
            var _ = new ODataRequest(new ServerContext());
        }
        [TestMethod]
        public void ServerWithUrl()
        {
            ClientContext.Current.RemoveAllServers();

            // this should work: the url is defined
            var _ = new ODataRequest(new ServerContext
            {
                Url = "example.com"
            });
        }

        [TestMethod]
        public void QueryString_Parameters()
        {
            var request = new ODataRequest(new ServerContext
            {
                Url = "https://example.com",
            })
            {
                Path = "/Root/MyContent",
                IsCollectionRequest = false
            };

            request.Parameters.Add("Id", "1");
            request.Parameters.Add("Name", "Value");

            var expected = "https://example.com/OData.svc/Root('MyContent')?Id=1&Name=Value&metadata=no";

            // ACTION
            var actual = request.ToString();

            // ASSERT
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void QueryString_ParameterArray()
        {
            var request = new ODataRequest(new ServerContext
            {
                Url = "https://example.com",
            })
            {
                Path = "/Root/MyContent",
                IsCollectionRequest = false
            };

            request.Parameters.Add("Id", "1");
            request.Parameters.Add("Id", "2");
            request.Parameters.Add("Id", "3");
            request.Parameters.Add("Name", "Value");

            var expected = "https://example.com/OData.svc/Root('MyContent')?Id=1&Id=2&Id=3&Name=Value&metadata=no";

            // ACTION
            var actual = request.ToString();

            // ASSERT
            Assert.AreEqual(expected, actual);
        }
    }
}
