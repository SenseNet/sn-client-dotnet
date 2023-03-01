using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests.UnitTests
{
    [TestClass]
    public class ToolsTests
    {
        [TestMethod]
        public void GenerateStreamFromString()
        {
            using (var stream = Tools.GenerateStreamFromString("test"))
            {
                using (var sr = new StreamReader(stream))
                {
                    var result = sr.ReadToEnd();

                    Assert.AreEqual("test", result);
                }
            }
        }
    }
}