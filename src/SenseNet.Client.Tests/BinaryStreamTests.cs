using System;
using System.IO;
using System.Net.Http;
using System.Threading;
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

        [TestMethod]
        public async Task Download()
        {
            var content = await Content.LoadAsync("/Root/System/Schema/ContentTypes/GenericContent");

            string ctd = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using(var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                    ctd = reader.ReadToEnd();
            }, CancellationToken.None);

            Assert.IsTrue(ctd.Contains("<ContentType name=\"GenericContent\""));
        }


        [TestMethod]
        public async Task Upload()
        {
            var uploadRootPath = "/Root/UploadTests";
            var fileContent = "Lorem ipsum dolor sit amet...";
            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            var fileName = Guid.NewGuid().ToString() + ".txt";

            using (var uploadStream = Tools.GenerateStreamFromString(fileContent))
                // ACTION
                await Content.UploadAsync(uploadRootPath, fileName, uploadStream, "File");

            // ASSERT
            var filePath = RepositoryPath.Combine(uploadRootPath, fileName);
            var content = await Content.LoadAsync(filePath);

            string downloadedFileContent = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                    downloadedFileContent = reader.ReadToEnd();
            }, CancellationToken.None);

            Assert.AreEqual(fileContent, downloadedFileContent);
        }

        [TestMethod]
        public async Task Upload_Text()
        {
            var uploadRootPath = "/Root/UploadTests";
            var fileContent = "Lorem ipsum dolor sit amet...";
            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            //var file = Content.CreateNew(uploadFolder.Path, "File", Guid.NewGuid().ToString());
            //await file.SaveAsync().ConfigureAwait(false);

            var fileName = Guid.NewGuid().ToString() + ".txt";

            // ACTION
            var uploaded = await Content.UploadTextAsync(uploadRootPath, fileName, fileContent, "File");

            // ASSERT
            var filePath = RepositoryPath.Combine(uploadRootPath, fileName);
            var content = await Content.LoadAsync(filePath);

            string downloadedFileContent = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                    downloadedFileContent = reader.ReadToEnd();
            }, CancellationToken.None);

            Assert.AreEqual(fileContent, downloadedFileContent);
        }

        [TestMethod]
        public async Task Upload_LowLevelApi()
        {
            var uploadRootPath = "/Root/UploadTests";
            var fileContent = "Lorem ipsum dolor sit amet...";
            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            //var file = Content.CreateNew(uploadFolder.Path, "File", Guid.NewGuid().ToString());
            //await file.SaveAsync().ConfigureAwait(false);

            var fileName = Guid.NewGuid().ToString() + ".txt";
            var uploadStream = Tools.GenerateStreamFromString(fileContent);

            UploadData uploadData = new UploadData
            {
                FileName = fileName,
                ContentType = "File"
            };

            // ACTION
            var uploaded = await RESTCaller.UploadAsync(uploadStream, uploadData, uploadFolder.Path).ConfigureAwait(false);

            // ASSERT
            var filePath = RepositoryPath.Combine(uploadRootPath, fileName);
            var content = await Content.LoadAsync(filePath);

            string downloadedFileContent = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                    downloadedFileContent = reader.ReadToEnd();
            }, CancellationToken.None);

            Assert.AreEqual(fileContent, downloadedFileContent);
        }
    }
}
