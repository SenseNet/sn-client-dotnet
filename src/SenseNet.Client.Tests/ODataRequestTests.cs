using System;
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
    }
}
