using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests.UnitTests
{
    [TestClass]
    public class SetPermissionRequestTests
    {
        [TestMethod]
        public void TestSetPermissionRequest()
        {
            var testRequest = new Security.SetPermissionRequest
            {
                Identity = "testIdentity",
                LocalOnly = true
            };

            var response = testRequest.Copy();

            Assert.AreEqual("testIdentity", response.Identity);
            Assert.IsTrue(response.LocalOnly.GetValueOrDefault(false));
        }
    }
}