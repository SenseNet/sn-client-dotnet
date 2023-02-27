using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Tests.UnitTests;

// ReSharper disable StringLiteralTypo

namespace SenseNet.Client.Tests.IntegrationTests
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
            var content = await Content.LoadAsync("/Root/System/Schema/ContentTypes/GenericContent").ConfigureAwait(false);

#pragma warning disable 618
            var request = RESTCaller.GetStreamRequest(content.Id);
#pragma warning restore 618
            var response = await request.GetResponseAsync().ConfigureAwait(false);

            string ctd = null;
            using (var stream = response.GetResponseStream())
                if(stream != null)
                    using (var reader = new StreamReader(stream))
                        ctd = reader.ReadToEnd();

            Assert.IsNotNull(ctd);
            Assert.IsTrue(ctd.Contains("<ContentType name=\"GenericContent\""));
        }

        [TestMethod]
        public async Task Download()
        {
            var content = await Content.LoadAsync("/Root/System/Schema/ContentTypes/GenericContent").ConfigureAwait(false);

            string ctd = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using(var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                    ctd = reader.ReadToEnd();
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(ctd.Contains("<ContentType name=\"GenericContent\""));
        }

        string _fileContent = "Lorem ipsum dolor sit amet...";

        [TestMethod]
        public async Task Upload_ByPath()
        {
            var uploadRootPath = "/Root/UploadTests";
            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            var fileName = Guid.NewGuid() + ".txt";

            using (var uploadStream = Tools.GenerateStreamFromString(_fileContent))
                // ACTION
                await Content.UploadAsync(uploadRootPath, fileName, uploadStream, "File").ConfigureAwait(false);

            // ASSERT
            var filePath = RepositoryPath.Combine(uploadRootPath, fileName);
            var content = await Content.LoadAsync(filePath).ConfigureAwait(false);

            string downloadedFileContent = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                    downloadedFileContent = reader.ReadToEnd();
            }, CancellationToken.None);

            Assert.AreEqual(_fileContent, downloadedFileContent);
        }
        [TestMethod]
        public async Task Upload_ById()
        {
            var uploadRootPath = "/Root/UploadTests";
            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            var uploadRootId = uploadFolder.Id;

            var fileName = Guid.NewGuid() + ".txt";

            using (var uploadStream = Tools.GenerateStreamFromString(_fileContent))
                // ACTION
                await Content.UploadAsync(uploadRootId, fileName, uploadStream, "File").ConfigureAwait(false);

            // ASSERT
            var filePath = RepositoryPath.Combine(uploadRootPath, fileName);
            var content = await Content.LoadAsync(filePath).ConfigureAwait(false);

            string downloadedFileContent = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                    downloadedFileContent = reader.ReadToEnd();
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(_fileContent, downloadedFileContent);
        }

        [TestMethod]
        public async Task Upload_Text_ByPath()
        {
            var uploadRootPath = "/Root/UploadTests";
            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            var fileName = Guid.NewGuid() + ".txt";

            // ACTION
            await Content.UploadTextAsync(uploadRootPath, fileName, _fileContent, CancellationToken.None, "File")
                .ConfigureAwait(false);

            // ASSERT
            var filePath = RepositoryPath.Combine(uploadRootPath, fileName);
            var content = await Content.LoadAsync(filePath).ConfigureAwait(false);

            string downloadedFileContent = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                    downloadedFileContent = reader.ReadToEnd();
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(_fileContent, downloadedFileContent);
        }
        [TestMethod]
        public async Task Upload_Text_ById()
        {
            var uploadRootPath = "/Root/UploadTests";
            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            var uploadRootId = uploadFolder.Id;

            var fileName = Guid.NewGuid() + ".txt";

            // ACTION
            await Content.UploadTextAsync(uploadRootId, fileName, _fileContent, CancellationToken.None, "File")
                .ConfigureAwait(false);

            // ASSERT
            var filePath = RepositoryPath.Combine(uploadRootPath, fileName);
            var content = await Content.LoadAsync(filePath).ConfigureAwait(false);

            string downloadedFileContent = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                    downloadedFileContent = reader.ReadToEnd();
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(_fileContent, downloadedFileContent);
        }

        [TestMethod]
        public async Task Upload_LowLevelApi()
        {
            var uploadRootPath = "/Root/UploadTests";
            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            //var file = Content.CreateNew(uploadFolder.Path, "File", Guid.NewGuid().ToString());
            //await file.SaveAsync().ConfigureAwait(false);

            var fileName = Guid.NewGuid().ToString() + ".txt";
            var uploadStream = Tools.GenerateStreamFromString(_fileContent);

            UploadData uploadData = new UploadData
            {
                FileName = fileName,
                ContentType = "File"
            };

            // ACTION
            await RESTCaller.UploadAsync(uploadStream, uploadData, uploadFolder.Path).ConfigureAwait(false);

            // ASSERT
            var filePath = RepositoryPath.Combine(uploadRootPath, fileName);
            var content = await Content.LoadAsync(filePath).ConfigureAwait(false);

            string downloadedFileContent = null;
            await RESTCaller.GetStreamResponseAsync(content.Id, async response =>
            {
                if (response == null)
                    return;
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                    downloadedFileContent = reader.ReadToEnd();
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(_fileContent, downloadedFileContent);
        }
    }
}
