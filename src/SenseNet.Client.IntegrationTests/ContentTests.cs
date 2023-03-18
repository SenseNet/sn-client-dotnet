using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Extensions.DependencyInjection;
using System.Threading.Channels;

namespace SenseNet.Client.IntegrationTests;

[TestClass]
public class ContentTests : IntegrationTestBase
{
    [TestMethod]
    public async Task IT_Content_Load()
    {
        // ALIGN-1
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", CancellationToken.None).ConfigureAwait(false);
        var expectedPath = "/Root/Content";

        // ACT-1: Load by path
        var content1 = await repository.LoadContentAsync(expectedPath, CancellationToken.None).ConfigureAwait(false);

        // ASSERT-1: not null
        Assert.IsNotNull(content1);

        // ALIGN-2
        var contentId = content1.Id;

        // ACT-2: Load by Id
        var content2 = await repository.LoadContentAsync(contentId, CancellationToken.None).ConfigureAwait(false);

        // ASSERT-2
        Assert.IsNotNull(content2);
        Assert.AreEqual(expectedPath, content2.Path);
    }

    [TestMethod]
    public async Task IT_Content_ExistsCreateDelete()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
        var parentPath = "/Root/Content";
        var contentName = nameof(IT_Content_ExistsCreateDelete);
        var contentTypeName = "Folder";
        var path = $"{parentPath}/{contentName}";

        // OPERATIONS
        // 1 - Delete content if exists for the clean test
        if (await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false))
        {
            await repository.DeleteContentAsync(path, true, cancel).ConfigureAwait(false);
            Assert.IsFalse(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));
        }

        // 2 - Create brand new content and test its existence
        var content = repository.CreateContent(parentPath, contentTypeName, contentName);
        await content.SaveAsync().ConfigureAwait(false);
        Assert.IsTrue(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));

        // 3 - Delete the content and check the repository is clean
        await repository.DeleteContentAsync(path, true, cancel).ConfigureAwait(false);
        Assert.IsFalse(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task IT_Content_Query()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        var request = new QueryContentRequest
        {
            ContentQuery = "TypeIs:User",
            Select = new[] {"Name", "Path", "Type"},
            OrderBy = new[] {"Name"}
        };
        var contents = await repository.QueryAsync(request, cancel).ConfigureAwait(false);

        // ASSERT
        var names = contents.Select(x => x.Name).ToArray();
        Assert.IsTrue(names.Contains("Admin"));
        Assert.IsTrue(names.Contains("Visitor"));
        var types = contents.Select(x => x["Type"].ToString()).Distinct().ToArray();
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("User", types[0]);
    }
    [TestMethod]
    public async Task IT_Content_QueryCount()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT-1: get collection
        var request = new QueryContentRequest
        {
            ContentQuery = "TypeIs:User",
            OrderBy = new[] { "Name" }
        };
        var contents = await repository.QueryAsync(request, cancel).ConfigureAwait(false);
        var expectedCount = contents.Count();

        // ASSERT-1
        Assert.IsTrue(expectedCount > 0);

        // ACT-2
        var actualCount = await repository.QueryCountAsync(request, cancel).ConfigureAwait(false);

        // ASSERT-2
        Assert.AreEqual(expectedCount, actualCount);
    }

    [TestMethod]
    public async Task IT_Content_Query_Depth()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
        var rootName = nameof(IT_Content_Query_Depth);
        var rootPath = $"/Root/Content/{rootName}";
        await CreateStructureForDepthTests(repository, rootName, cancel).ConfigureAwait(false);

        // ACT
        var request = new LoadCollectionRequest
        {
            Path = rootPath,
            ContentQuery = "Name:'*-1'",
            Select = new[] { "Name", "Path", "Type" },
            OrderBy = new[] { "Path" }
        };
        var result = await repository.QueryAsync(request, cancel).ConfigureAwait(false);
        var contents = result.ToArray();

        // ASSERT
        Assert.AreEqual(4, contents.Length);
        Assert.AreEqual($"{rootPath}/Folder-0/File-1", contents[0].Path);
        Assert.AreEqual($"{rootPath}/Folder-1", contents[1].Path);
        Assert.AreEqual($"{rootPath}/Folder-1/File-1", contents[2].Path);
        Assert.AreEqual($"{rootPath}/Folder-2/File-1", contents[3].Path);

        // ACT-2
        request = new LoadCollectionRequest
        {
            Path = $"{rootPath}/Folder-1",
            ContentQuery = "Name:'*-1'",
            Select = new[] { "Name", "Path", "Type" },
            OrderBy = new[] { "Path" }
        };
        result = await repository.QueryAsync(request, cancel).ConfigureAwait(false);
        contents = result.ToArray();

        // ASSERT-2
        Assert.AreEqual(2, contents.Length);
        Assert.AreEqual($"{rootPath}/Folder-1", contents[0].Path);
        Assert.AreEqual($"{rootPath}/Folder-1/File-1", contents[1].Path);

    }
    [TestMethod]
    public async Task IT_Content_Collection_Depth()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
        var rootName = nameof(IT_Content_Query_Depth);
        var rootPath = $"/Root/Content/{rootName}";
        await CreateStructureForDepthTests(repository, rootName, cancel).ConfigureAwait(false);

        // ACT
        var request = new LoadCollectionRequest
        {
            Path = rootPath,
            ContentQuery = "Name:'*-1'",
            Select = new[] { "Name", "Path", "Type" },
            OrderBy = new[] { "Path" }
        };
        var result = await repository.LoadCollectionAsync(request, cancel).ConfigureAwait(false);
        var contents = result.ToArray();

        // ASSERT
        Assert.AreEqual(1, contents.Length);
        Assert.AreEqual($"{rootPath}/Folder-1", contents[0].Path);
    }

    private async Task CreateStructureForDepthTests(IRepository repository, string rootName, CancellationToken cancel)
    {
        var path = "/Root/Content/" + rootName;
        if (await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false))
            await repository.DeleteContentAsync(path, true, cancel).ConfigureAwait(false);

        var rootFolder = repository.CreateContent("/Root/Content", "Folder", rootName);
        await rootFolder.SaveAsync().ConfigureAwait(false);
        for (var i = 0; i < 3; i++)
        {
            var folder = repository.CreateContent(rootFolder.Path, "Folder", "Folder-" + i);
            await folder.SaveAsync().ConfigureAwait(false);
            for (var j = 0; j < 3; j++)
            {
                var file = repository.CreateContent(folder.Path, "File", "File-" + j);
                await file.SaveAsync().ConfigureAwait(false);
            }
        }
    }

    /* ================================================================================================== UPDATE */

    private class TestMemo : Content
    {
        public TestMemo(IRestCaller restCaller, ILogger<TestMemo> logger) : base(restCaller, logger) { }

        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string[] MemoType { get; set; } // generic, iso, iaudit
        public List<Content> SeeAlso { get; set; }
    }

    [TestMethod]
    public async Task IT_Content_T_Update()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository =
            await GetRepositoryCollection(
                    services => { services.RegisterGlobalContentType<TestMemo>("Memo"); })
                .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
        var rootPath = "/Root/Content";
        var containerName = "MyMemos";
        var containerType = "MemoList";
        //var contentTypeName = "Task";
        var containerPath = $"{rootPath}/{containerName}";
        var contentName = "Memo1";
        var path = $"{containerPath}/{contentName}";
        var firstDate = new DateTime(2022, 03, 10);
        var updatedDateDate = new DateTime(2023, 03, 10);

        // OPERATIONS
        // 1 - Delete content if exists for the clean test
        if (await repository.IsContentExistsAsync(containerPath, cancel).ConfigureAwait(false))
        {
            await repository.DeleteContentAsync(containerPath, true, cancel).ConfigureAwait(false);
            Assert.IsFalse(await repository.IsContentExistsAsync(containerPath, cancel).ConfigureAwait(false));
        }

        // 2 - Create container
        var container = repository.CreateContent(rootPath, containerType, containerName);
        await container.SaveAsync().ConfigureAwait(false);

        // 3 - Create brand new content and test its existence
        var content = repository.CreateContent<TestMemo>(containerPath, null, contentName);
        content.Description = "My first memo.";
        content.MemoType = new[] {"generic"};
        content.Date = firstDate;
        await content.SaveAsync().ConfigureAwait(false);
        Assert.IsTrue(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));
        var contentId = content.Id;
        Assert.IsTrue(contentId > 0);
            
        // 4 - Load created content
        var loadedContent = await repository.LoadContentAsync<TestMemo>(contentId, cancel).ConfigureAwait(false);
        Assert.AreEqual("My first memo.", loadedContent.Description);
        Assert.AreEqual(firstDate, loadedContent.Date);
        Assert.AreEqual(1, loadedContent.MemoType.Length);
        Assert.AreEqual("generic", loadedContent.MemoType[0]);

        // 5 - Update content
        loadedContent.Description = "Updated description.";
        loadedContent.Date = updatedDateDate;
        loadedContent.MemoType[0] = "iso";
        await loadedContent.SaveAsync().ConfigureAwait(false);

        // 6 - Load updated content
        var reloadedContent = await repository.LoadContentAsync<TestMemo>(contentId, cancel).ConfigureAwait(false);
        Assert.AreEqual("Updated description.", reloadedContent.Description);
        Assert.AreEqual(updatedDateDate, reloadedContent.Date);
        Assert.AreEqual(1, reloadedContent.MemoType.Length);
        Assert.AreEqual("iso", reloadedContent.MemoType[0]);

        // 7 - Delete the content and check the repository is clean
        await repository.DeleteContentAsync(contentId, true, cancel).ConfigureAwait(false);
        Assert.IsFalse(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));
    }
    [TestMethod]
    public async Task IT_Content_T_Update_ReferenceList()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository =
            await GetRepositoryCollection(
                    services => { services.RegisterGlobalContentType<TestMemo>("Memo"); })
                .GetRepositoryAsync("local", cancel).ConfigureAwait(false);
        var rootPath = "/Root/Content";
        var containerName = "MyMemos";
        var containerType = "MemoList";
        var contentTypeName = "Memo";
        var containerPath = $"{rootPath}/{containerName}";
        var contentName = "Memo1";
        var path = $"{containerPath}/{contentName}";

        // OPERATIONS
        // 1 - Delete content if exists for the clean test
        if (await repository.IsContentExistsAsync(containerPath, cancel).ConfigureAwait(false))
        {
            await repository.DeleteContentAsync(containerPath, true, cancel).ConfigureAwait(false);
            Assert.IsFalse(await repository.IsContentExistsAsync(containerPath, cancel).ConfigureAwait(false));
        }

        // 2 - Create container and some memos
        var container = repository.CreateContent(rootPath, containerType, containerName);
        await container.SaveAsync().ConfigureAwait(false);
        var referredMemos = new Content[3];
        for (int i = 0; i < referredMemos.Length; i++)
        {
            referredMemos[i] = repository.CreateContent(container.Path, contentTypeName, $"ReferredMemo{i + 1}");
            //referredMemos[i]["MemoType"] = Array.Empty<string>();
            //referredMemos[i]["MemoType"] = new[] { "generic" };
            ((TestMemo)referredMemos[i]).MemoType = Array.Empty<string>();
            await referredMemos[i].SaveAsync().ConfigureAwait(false);
        }

        // 3 - Create brand new content and test its existence
        var content = repository.CreateContent<TestMemo>(containerPath, null, contentName);
        content.MemoType = Array.Empty<string>();
        content.SeeAlso = referredMemos.Take(2).ToList();
        await content.SaveAsync().ConfigureAwait(false);
        var contentId = content.Id;
        Assert.IsTrue(contentId > 0);

        // 4 - Load created content without expanding the references
        var loadedContent = await repository.LoadContentAsync<TestMemo>(contentId, cancel).ConfigureAwait(false);
        Assert.AreEqual(null, loadedContent.SeeAlso);

        // 5 - Load created content with expanding the references
        var expandedRequest = new LoadContentRequest
        {
            ContentId = contentId,
            Expand = new[] {nameof(TestMemo.SeeAlso)},
            Select = new[] {"Id", "Name", "Description", "Date", "MemoType", "SeeAlso/Path", "SeeAlso/Type" }
        };
        loadedContent = await repository.LoadContentAsync<TestMemo>(expandedRequest, cancel).ConfigureAwait(false);
        Assert.IsNotNull(loadedContent.SeeAlso);
        Assert.AreEqual(2, loadedContent.SeeAlso.Count);
        Assert.AreEqual("/Root/Content/MyMemos/ReferredMemo1", loadedContent.SeeAlso[0].Path);
        Assert.AreEqual("/Root/Content/MyMemos/ReferredMemo2", loadedContent.SeeAlso[1].Path);

        // 6 - Update content
        loadedContent.SeeAlso[1] = referredMemos[2];
        await loadedContent.SaveAsync().ConfigureAwait(false);

        // 7 - Load updated content
        var reloadedContent = await repository.LoadContentAsync<TestMemo>(expandedRequest, cancel).ConfigureAwait(false);
        Assert.IsNotNull(loadedContent.SeeAlso);
        Assert.AreEqual(2, reloadedContent.SeeAlso.Count);
        Assert.AreEqual("/Root/Content/MyMemos/ReferredMemo1", reloadedContent.SeeAlso[0].Path);
        Assert.AreEqual("/Root/Content/MyMemos/ReferredMemo3", reloadedContent.SeeAlso[1].Path);

        // 8 - Load updated content with incompletely expanded references
        var notCompletelyExpandedRequest = new LoadContentRequest
        {
            ContentId = contentId,
            Expand = new[] { nameof(TestMemo.SeeAlso) },
            Select = new[] { "Id", "Name", "Description", "Date", "MemoType", "SeeAlso/Description", "SeeAlso/Type" }
        };
        var wrongContent = await repository.LoadContentAsync<TestMemo>(notCompletelyExpandedRequest, cancel).ConfigureAwait(false);
        Assert.IsNotNull(wrongContent.SeeAlso);
        Assert.AreEqual(2, wrongContent.SeeAlso.Count);
        Assert.IsNull(wrongContent.SeeAlso[0].Path);
        Assert.IsNull(wrongContent.SeeAlso[1].Path);
        Assert.AreEqual(0, wrongContent.SeeAlso[0].Id);
        Assert.AreEqual(0, wrongContent.SeeAlso[1].Id);

        // 9 - Try to update incompletely expanded references
        wrongContent.SeeAlso.Add(referredMemos[2]);
        try
        {
            await wrongContent.SaveAsync().ConfigureAwait(false);
            Assert.Fail("The expected ApplicationException was not thrown.");
        }
        catch (ApplicationException e)
        {
            Assert.AreEqual($"Cannot save the content. Id: {wrongContent.Id}, Path: '{wrongContent.Path}'. " +
                            $"See inner exception for details.", e.Message);
            Assert.AreEqual("One or more referred content cannot be recognized. " +
                            "The referred content should have the Id or Path. FieldName: 'SeeAlso'.", e.InnerException?.Message);
        }

        // 10 - Delete the content and check the repository is clean
        await repository.DeleteContentAsync(contentId, true, cancel).ConfigureAwait(false);
        Assert.IsFalse(await repository.IsContentExistsAsync(path, cancel).ConfigureAwait(false));
    }
}