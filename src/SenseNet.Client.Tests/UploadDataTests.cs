using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class UploadDataTests
    {
        [TestMethod]
        public void UploadData_DictionaryAndKeyValuePairsEquality()
        {
            var uploadData = new UploadData
            {
                ContentType = "content-type",
                FileText = "file-text",
                ContentId = 42,
                FileName = "filename",
                PropertyName = "property-name",
                FileLength = 4242,
                ChunkToken = "chunk-token",
                Overwrite = true,
                UseChunk = false
            };

            var dict = JsonConvert.SerializeObject(uploadData.ToDictionary().ToDictionary(x => x.Key, x => x.Value.ToString()));
            var pairs = JsonConvert.SerializeObject(uploadData.ToKeyValuePairs().ToDictionary(x=>x.Key, x=>x.Value));

            Assert.AreEqual(dict, pairs);
        }
    }
}
