using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    // ReSharper disable once InconsistentNaming
    public class RESTExtensionsTests
    {
        [TestMethod]
        public void TestAppendParameter()
        {
            var testStringBuilder = new StringBuilder();
            var testKey = "testKey";
            var testValue = "testValue";

            testStringBuilder.AppendParameter(testKey, testValue);

            Assert.AreEqual("testKey=testValue", testStringBuilder.ToString());
        }

        [TestMethod]
        public void TestAppendParameterLengthOverZero()
        {
            var testStringBuilder = new StringBuilder("https://www.example.com/search?limit=20");
            var testKey = "testKey";
            var testValue = "testValue";

            testStringBuilder.AppendParameter(testKey, testValue);

            Assert.AreEqual("https://www.example.com/search?limit=20&testKey=testValue", testStringBuilder.ToString());
        }

        [TestMethod]
        public void TestAppendParameterWithNullKey()
        {
            var testStringBuilder = new StringBuilder();
            var testValue = "testValue";

            testStringBuilder.AppendParameter(null, testValue);

            Assert.AreEqual("", testStringBuilder.ToString());
        }

        [TestMethod]
        public void TestAppendParameterWithNullValue()
        {
            var testStringBuilder = new StringBuilder();
            var testKey = "testKey";

            testStringBuilder.AppendParameter(testKey, null);

            Assert.AreEqual("", testStringBuilder.ToString());
        }
    }
}