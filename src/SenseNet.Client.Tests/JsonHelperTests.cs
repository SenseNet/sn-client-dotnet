using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class JsonHelperTests
    {
        [TestMethod]
        public void TestSerialize()
        {
            var testObject = new KeyValuePair<string, int>("id", 6);

            var response = JsonHelper.Serialize(testObject);

            Assert.IsInstanceOfType(response, typeof(string));
        }

        [TestMethod]
        public void TestDeserializeAsType()
        {
            string testObject = "{\"Key\":\"id\",\"Value\":6}";

            KeyValuePair<string, int> response = JsonHelper.Deserialize<KeyValuePair<string, int>>(testObject);

            Assert.IsNotInstanceOfType(response, typeof(string));

            Assert.AreEqual("id", response.Key);
            Assert.AreEqual(6, response.Value);
        }

        [TestMethod]
        public void TestDeserialize()
        {
            string testObject = "{\"Key\":\"id\",\"Value\":6}";

            var response = JsonHelper.Deserialize(testObject);

            Assert.IsNotInstanceOfType(response, typeof(string));

            Assert.AreEqual("id", response["Key"].ToString());
            Assert.AreEqual(6, Convert.ToInt32(response["Value"]));
        }

        [TestMethod]
        public void TestGetJsonPostModel()
        {
            var testObject = new KeyValuePair<string, int>("id", 6);

            var response = JsonHelper.GetJsonPostModel(testObject);

            Assert.IsInstanceOfType(response, typeof(string));
            Assert.IsTrue(response.StartsWith("models=["));
        }
    }
}