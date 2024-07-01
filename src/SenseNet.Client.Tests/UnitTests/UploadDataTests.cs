using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SenseNet.Client.Tests.UnitTests
{
    [TestClass]
    public class UploadDataTests : TestBase
    {
        private CancellationToken _cancel = new CancellationToken();

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
            var pairs = JsonConvert.SerializeObject(uploadData.ToKeyValuePairs().ToDictionary(x => x.Key, x => x.Value));

            Assert.AreEqual(dict, pairs);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task Upload_Text_TooBig()
        {
            var restCaller = CreateRestCallerForProcessWebRequestResponse(null);
            var repositories = GetRepositoryCollection(
                services => { services.AddSingleton(restCaller); });
            var repository = await repositories.GetRepositoryAsync(FakeServer, _cancel)
                .ConfigureAwait(false);

            var orig = ClientContext.Current.ChunkSizeInBytes;

            try
            {
                ClientContext.Current.ChunkSizeInBytes = 10;
                await repository.UploadAsync(new UploadRequest
                {
                    ParentId = 1,
                    ContentName = "example.txt",
                    ContentType = ""
                }, "too long text", _cancel);
            }
            finally
            {
                ClientContext.Current.ChunkSizeInBytes = orig;
            }
        }
    }
}
