using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class ToolsTests
    {
        [TestMethod]
        public void GenerateStreamFromString()
        {
            var response = Tools.GenerateStreamFromString("test");

            Assert.IsInstanceOfType(response, typeof(System.IO.Stream));
        }
    }
}