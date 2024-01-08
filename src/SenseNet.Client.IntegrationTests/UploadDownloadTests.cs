using Microsoft.Extensions.Logging;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Testing;

namespace SenseNet.Client.IntegrationTests;

[TestClass]
public class UploadDownloadTests : IntegrationTestBase
{
    private readonly CancellationToken _cancel = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

    private async Task<Content> EnsureUploadFolder(IRepository repository)
    {
        var uploadRootPath = "/Root/UploadTests";
        var uploadFolder = await repository.LoadContentAsync(uploadRootPath, _cancel).ConfigureAwait(false);
        if (uploadFolder == null)
        {
            uploadFolder = repository.CreateContent("/Root", "SystemFolder", "UploadTests");
            await uploadFolder.SaveAsync(_cancel).ConfigureAwait(false);
        }

        return uploadFolder;
    }

    private Task GetStreamResponseAsync(IRepository repository, int contentId, Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
    {
        return GetStreamResponseAsync(repository, contentId, null, null, responseProcessor, cancel);
    }
    private async Task GetStreamResponseAsync(IRepository repository, int contentId, string? version, string? propertyName, Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
    {
        var url = $"/binaryhandler.ashx?nodeid={contentId}&propertyname={propertyName ?? "Binary"}";
        if (!string.IsNullOrEmpty(version))
            url += "&version=" + version;

        await repository.ProcessWebResponseAsync(url, HttpMethod.Get, null, null, responseProcessor, cancel)
            .ConfigureAwait(false);
    }

    private async Task<string?> DownloadAsString(IRepository repository, int contentId, CancellationToken cancel)
    {
        string? downloadedFileContent = null;
        await GetStreamResponseAsync(repository, contentId, async (response, cancellationToken) =>
        {
            if (response == null)
                return;
            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            using (var reader = new StreamReader(stream))
                downloadedFileContent = await reader.ReadToEndAsync().ConfigureAwait(false);
        }, cancel);
        return downloadedFileContent;
    }

    /* =============================================================================== UPLOAD */

    [TestMethod]
    public async Task IT_Upload_ByPath()
    {
        var fileContent = "Lorem ipsum dolor sit amet...";
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootPath = uploadFolder.Path;
        var fileName = Guid.NewGuid() + ".txt";

        // ACTION
        UploadResult uploadedContent;
        await using (var uploadStream = Tools.GenerateStreamFromString(fileContent))
        {
            var request = new UploadRequest {ParentPath = uploadRootPath, ContentName = fileName };
            uploadedContent = await repository.UploadAsync(request, uploadStream, _cancel).ConfigureAwait(false);
        }

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(fileContent, downloadedFileContent);
    }
    [TestMethod]
    public async Task IT_Upload_ById()
    {
        var fileContent = "Lorem ipsum dolor sit amet...";
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootId = uploadFolder.Id;
        var fileName = Guid.NewGuid() + ".txt";

        // ACT
        UploadResult uploadedContent;
        await using (var uploadStream = Tools.GenerateStreamFromString(fileContent))
        {
            var request = new UploadRequest { ParentId = uploadRootId, ContentName = fileName };
            uploadedContent = await repository.UploadAsync(request, uploadStream, _cancel).ConfigureAwait(false);
        }

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(fileContent, downloadedFileContent);
    }

    [TestMethod]
    public async Task IT_Upload_ChunksAndProgress()
    {
        var fileContent = "111111111122222222223333333333444";
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootPath = uploadFolder.Path;
        var fileName = Guid.NewGuid() + ".txt";

        UploadResult uploadedContent;
        var progressValues = new List<int>();
        using (new Swindler<int>(
                   hack: 10,
                   getter: () => ClientContext.Current.ChunkSizeInBytes,
                   setter: value => ClientContext.Current.ChunkSizeInBytes = value))
        {
            // ACTION
            await using (var uploadStream = Tools.GenerateStreamFromString(fileContent))
            {
                var request = new UploadRequest { ParentPath = uploadRootPath, ContentName = fileName };
                uploadedContent = await repository.UploadAsync(request, uploadStream,
                    progress => { progressValues.Add(progress); }, _cancel).ConfigureAwait(false);
            }

        }

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(fileContent, downloadedFileContent);
        var actualProgress = string.Join(",", progressValues.Select(x => x.ToString()));
        Assert.AreEqual("10,20,30,33", actualProgress);
    }

    [TestMethod]
    public async Task IT_Upload_Text_ByPath()
    {
        var fileContent = "Lorem ipsum dolor sit amet...";
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootPath = uploadFolder.Path;
        var fileName = Guid.NewGuid() + ".txt";

        // ACT
        var request = new UploadRequest { ParentPath = uploadRootPath, ContentName = fileName };
        var uploadedContent = await repository.UploadAsync(request, fileContent, _cancel).ConfigureAwait(false);

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(fileContent, downloadedFileContent);
    }
    [TestMethod]
    public async Task IT_Upload_Text_ById()
    {
        var fileContent = "Lorem ipsum dolor sit amet...";
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootId = uploadFolder.Id;
        var fileName = Guid.NewGuid() + ".txt";

        // ACTION
        var request = new UploadRequest { ParentId = uploadRootId, ContentName = fileName };
        var uploadedContent = await repository.UploadAsync(request, fileContent, _cancel).ConfigureAwait(false);

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(fileContent, downloadedFileContent);
    }

    /* =============================================================================== DOWNLOAD */

    #region nested content classes

    public class ContentType : Content
    {
        public ContentType(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public Binary? Binary { get; set; }
    }
    public class File : Content
    {
        public File(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public string Version { get; set; }
        public Binary Binary { get; set; }
    }

    #endregion

    [TestMethod]
    public async Task IT_Download_HighLevel()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token;
        var repository = await GetRepositoryCollection(services =>
        {
            services.RegisterGlobalContentType<ContentType>();
        }).GetRepositoryAsync("local", cancel).ConfigureAwait(false);
        var content = await repository.LoadContentAsync<ContentType>(
                new LoadContentRequest {Path = "/Root/System/Schema/ContentTypes/GenericContent/File"}, cancel)
            .ConfigureAwait(false);
        if(content?.Binary == null)
            Assert.Fail("Content or Binary not found.");
        string? text = null;
        StreamProperties? properties = null;
        var streamLength = 0L;

        // ACT
        await content.Binary.DownloadAsync(async (stream, props) =>
        {
            properties = props;
            streamLength = stream.Length;
            using var reader = new StreamReader(stream);
            text = await reader.ReadToEndAsync().ConfigureAwait(false);
        }, cancel).ConfigureAwait(false);

        // ASSERT
        Assert.IsNotNull(text);
        Assert.IsTrue(text.Contains("<ContentType name=\"File\""));
        Assert.IsNotNull(properties);
        Assert.AreEqual("text/xml", properties.MediaType);
        Assert.AreEqual("File.ContentType", properties.FileName);
        Assert.AreEqual(streamLength, properties.ContentLength);
    }

    [TestMethod]
    public async Task IT_Download_LowLevel_ById()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token;
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", cancel).ConfigureAwait(false);
        var content = await repository.LoadContentAsync(
                new LoadContentRequest
                {
                    Path = "/Root/System/Schema/ContentTypes/GenericContent/File",
                    Select = new[] {"Id"}
                }, cancel)
            .ConfigureAwait(false);
        var contentId = content.Id;

        // ACT
        string? text = null;
        StreamProperties? properties = null;
        long streamLength = 0L;
        var request = new DownloadRequest { ContentId = contentId };
        await repository.DownloadAsync(request, async (stream, props) =>
        {
            properties = props;
            streamLength = stream.Length;
            using var reader = new StreamReader(stream);
            text = await reader.ReadToEndAsync().ConfigureAwait(false);
        }, cancel).ConfigureAwait(false);

        // ASSERT
        Assert.IsNotNull(text);
        Assert.IsTrue(text.Contains("<ContentType name=\"File\""));
        Assert.IsNotNull(properties);
        Assert.AreEqual("text/xml", properties.MediaType);
        Assert.AreEqual("File.ContentType", properties.FileName);
        Assert.AreEqual(streamLength, properties.ContentLength);
    }

    [TestMethod]
    public async Task IT_Download_LowLevel_ByPath()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token;
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        string? text = null;
        StreamProperties? properties = null;
        long streamLength = 0L;
        var request = new DownloadRequest { Path = "/Root/System/Schema/ContentTypes/GenericContent/File" };
        await repository.DownloadAsync(request, async (stream, props) =>
        {
            properties = props;
            streamLength = stream.Length;
            using var reader = new StreamReader(stream);
            text = await reader.ReadToEndAsync().ConfigureAwait(false);
        }, cancel).ConfigureAwait(false);

        // ASSERT
        Assert.IsNotNull(text);
        Assert.IsTrue(text.Contains("<ContentType name=\"File\""));
        Assert.IsNotNull(properties);
        Assert.AreEqual("text/xml", properties.MediaType);
        Assert.AreEqual("File.ContentType", properties.FileName);
        Assert.AreEqual(streamLength, properties.ContentLength);
    }


    [TestMethod]
    public async Task IT_Download_OlderVersions()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository =
            await GetRepositoryCollection(
                    services => { services.RegisterGlobalContentType<File>(); })
                .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
        var rootPath = "/Root/Content";
        var fileName = "MyFile";
        var filePath = $"{rootPath}/{fileName}";

        try
        {
            UploadResult uploadedResult;
            var loadFileRequest = new LoadContentRequest
            {
                Path = filePath,
                Select = new[] {"Id", "Path", "Name", "Type", "Version", "VersioningMode", "Binary"}
            };
            var uploadRequest = new UploadRequest {ParentPath = rootPath, ContentName = fileName};

            // Prepare file versions
            var file = repository.CreateContent<File>(rootPath, null, fileName);
            file.VersioningMode = VersioningMode.MajorAndMinor;
            await file.SaveAsync(cancel).ConfigureAwait(false);

            await file.CheckOutAsync(cancel).ConfigureAwait(false); // V0.2.L

            await using (var uploadStream = Tools.GenerateStreamFromString("File text 1"))
                uploadedResult =
                    await repository.UploadAsync(uploadRequest, uploadStream, cancel).ConfigureAwait(false);

            await file.CheckInAsync(cancel).ConfigureAwait(false); //V0.2.D

            await file.CheckOutAsync(cancel).ConfigureAwait(false); // V0.3.L

            await using (var uploadStream = Tools.GenerateStreamFromString("File text 2"))
                uploadedResult =
                    await repository.UploadAsync(uploadRequest, uploadStream, cancel).ConfigureAwait(false);

            await repository.GetResponseStringAsync(
                new ODataRequest(repository.Server) {ContentId = file.Id, ActionName = "Publish"}, // V1.0.A
                HttpMethod.Post, cancel).ConfigureAwait(false);

            Assert.AreEqual(file.Id, uploadedResult.Id);
            file = await repository.LoadContentAsync<File>(loadFileRequest, cancel).ConfigureAwait(false);
            Assert.AreEqual("V1.0.A", file.Version);

            await file.CheckOutAsync(cancel).ConfigureAwait(false); // V1.1.L

            await using (var uploadStream = Tools.GenerateStreamFromString("File text 3"))
                uploadedResult =
                    await repository.UploadAsync(uploadRequest, uploadStream, cancel).ConfigureAwait(false);

            await file.CheckInAsync(cancel).ConfigureAwait(false); //V1.1.D

            // ACT: Download versions
            string? lastDraftText = null; // V1.1
            string? lastMajorText = null; // V1.0
            string? file_V0_2Text = null; // V0.2
            await repository.DownloadAsync(
                request: new DownloadRequest {ContentId = file.Id},
                responseProcessor: async (stream, _) =>
                {
                    using var reader = new StreamReader(stream);
                    lastDraftText = await reader.ReadToEndAsync();
                }, cancel).ConfigureAwait(false);
            await repository.DownloadAsync(
                request: new DownloadRequest {ContentId = file.Id, Version = "LastMajor"},
                responseProcessor: async (stream, _) =>
                {
                    using var reader = new StreamReader(stream);
                    lastMajorText = await reader.ReadToEndAsync();
                }, cancel).ConfigureAwait(false);
            await repository.DownloadAsync(
                request: new DownloadRequest {ContentId = file.Id, Version = "V0.2"},
                responseProcessor: async (stream, _) =>
                {
                    using var reader = new StreamReader(stream);
                    file_V0_2Text = await reader.ReadToEndAsync();
                }, cancel).ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("File text 3", lastDraftText);
            Assert.AreEqual("File text 2", lastMajorText);
            Assert.AreEqual("File text 1", file_V0_2Text);
        }
        catch (Exception e)
        {
            var q = 1;
        }
        finally
        {
            await repository.DeleteContentAsync(filePath, true, cancel);
        }
    }
}