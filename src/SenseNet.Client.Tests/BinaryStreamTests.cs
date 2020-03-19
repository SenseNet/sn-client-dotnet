using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class BinaryStreamTests
    {
        [ClassInitialize]
        public static void ClassInitializer(TestContext _)
        {
            Initializer.InitializeServer();
        }

        [TestMethod]
        public async Task Download_OldSchool()
        {
            var content = await Content.LoadAsync("/Root/System/Schema/ContentTypes/GenericContent");

            var request = RESTCaller.GetStreamRequest(content.Id);
            var response = await request.GetResponseAsync();

            string ctd;
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
                ctd = reader.ReadToEnd();

            Assert.IsTrue(ctd.Contains("<ContentType name=\"GenericContent\""));
        }
    }
}
