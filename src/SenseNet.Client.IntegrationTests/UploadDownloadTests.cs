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

    private async Task<string> DownloadAsString(IRepository repository, int contentId, CancellationToken cancel)
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
        StreamProperties properties = null;
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
        Assert.AreEqual("", properties.MediaType);
        Assert.AreEqual("", properties.FileName);
        Assert.AreEqual(streamLength, properties.ContentLength);
    }

    [TestMethod]
    public async Task IT_Download_LowLevel_ByPath()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token;
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        string? text = null;
        StreamProperties properties = null;
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
        Assert.AreEqual(" ", properties.MediaType);
        Assert.AreEqual("", properties.FileName);
        Assert.AreEqual(streamLength, properties.ContentLength);
    }

}