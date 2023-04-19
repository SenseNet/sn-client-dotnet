namespace SenseNet.Client.IntegrationTests;

[TestClass]
public class UploadDownloadTests : IntegrationTestBase
{
    private static readonly string _fileContent = "Lorem ipsum dolor sit amet...";
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


    [TestMethod]
    public async Task IT_Upload_ByPath()
    {
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootPath = uploadFolder.Path;
        var fileName = Guid.NewGuid() + ".txt";

        // ACTION
        UploadResult uploadedContent;
        await using (var uploadStream = Tools.GenerateStreamFromString(_fileContent))
        {
            var request = new UploadRequest {ParentPath = uploadRootPath, ContentName = fileName };
            uploadedContent = await repository.UploadAsync(request, uploadStream, _cancel).ConfigureAwait(false);
        }

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(_fileContent, downloadedFileContent);
    }
    [TestMethod]
    public async Task IT_Upload_ById()
    {
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootId = uploadFolder.Id;
        var fileName = Guid.NewGuid() + ".txt";

        // ACT
        UploadResult uploadedContent;
        await using (var uploadStream = Tools.GenerateStreamFromString(_fileContent))
        {
            var request = new UploadRequest { ParentId = uploadRootId, ContentName = fileName };
            uploadedContent = await repository.UploadAsync(request, uploadStream, _cancel).ConfigureAwait(false);
        }

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(_fileContent, downloadedFileContent);
    }

    [TestMethod]
    public async Task IT_Upload_Text_ByPath()
    {
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootPath = uploadFolder.Path;
        var fileName = Guid.NewGuid() + ".txt";

        // ACT
        var request = new UploadRequest { ParentPath = uploadRootPath, ContentName = fileName };
        var uploadedContent = await repository.UploadAsync(request, _fileContent, _cancel).ConfigureAwait(false);

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(_fileContent, downloadedFileContent);
    }
    [TestMethod]
    public async Task IT_Upload_Text_ById()
    {
        var repository = await GetRepositoryCollection().GetRepositoryAsync("local", _cancel).ConfigureAwait(false);
        var uploadFolder = await EnsureUploadFolder(repository).ConfigureAwait(false);
        var uploadRootId = uploadFolder.Id;
        var fileName = Guid.NewGuid() + ".txt";

        // ACTION
        var request = new UploadRequest { ParentId = uploadRootId, ContentName = fileName };
        var uploadedContent = await repository.UploadAsync(request, _fileContent, _cancel).ConfigureAwait(false);

        // ASSERT
        var downloadedFileContent = await DownloadAsString(repository, uploadedContent.Id, _cancel)
            .ConfigureAwait(false);
        Assert.AreEqual(_fileContent, downloadedFileContent);
    }
}