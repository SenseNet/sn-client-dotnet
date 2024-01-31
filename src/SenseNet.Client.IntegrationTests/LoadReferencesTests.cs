namespace SenseNet.Client.IntegrationTests;

[TestClass]
public class LoadReferencesTests : IntegrationTestBase
{
    [TestMethod]
    public async Task IT_Repository_LoadReferences_SingleRef()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        var request = new LoadReferenceRequest {Path = "/Root", FieldName = "CreatedBy"};
        var multiRef = await repository.LoadReferencesAsync(request, cancel).ConfigureAwait(false);
        var singleRef = await repository.LoadReferenceAsync(request, cancel).ConfigureAwait(false);
        var multiRefUser = await repository.LoadReferencesAsync<User>(request, cancel).ConfigureAwait(false);
        var singleRefUser = await repository.LoadReferenceAsync<User>(request, cancel).ConfigureAwait(false);

        // ASSERT
        Assert.AreEqual(1, multiRef.Single().Id);
        Assert.AreEqual(1, singleRef.Id);
        Assert.AreEqual(typeof(ContentCollection<User>), multiRefUser.GetType());
        Assert.AreEqual(typeof(User), multiRefUser.Single().GetType());
        Assert.AreEqual(typeof(User), singleRefUser.GetType());
        Assert.AreEqual(1, multiRefUser.Single().Id);
        Assert.AreEqual(1, singleRefUser.Id);
    }
    [TestMethod]
    public async Task IT_Repository_LoadReferences_MultiRef()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT
        var request = new LoadReferenceRequest { Path = Constants.Group.AdministratorsPath, FieldName = "Members" };
        var multiRef = await repository.LoadReferencesAsync(request, cancel).ConfigureAwait(false);
        var singleRef = await repository.LoadReferenceAsync(request, cancel).ConfigureAwait(false);

        // ASSERT
        var content = multiRef.FirstOrDefault();
        Assert.IsNotNull(content);
        Assert.AreEqual(Constants.User.AdminId, content.Id);
        Assert.IsNotNull(content["DisplayName"]);

        Assert.IsNotNull(singleRef);
        Assert.AreEqual(Constants.User.AdminId, singleRef.Id);
        Assert.IsNotNull(singleRef["DisplayName"]);
    }
    [TestMethod]
    public async Task IT_Repository_LoadReferences_WithSelect()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT-1 Load by path
        var request = new LoadReferenceRequest
        {
            Path = Constants.Group.AdministratorsPath,
            FieldName = "Members",
            Select = new[] { "Id", "Path" }
        };
        var admins = await repository.LoadReferencesAsync(request, cancel).ConfigureAwait(false);

        // ASSERT-1
        var admin = admins.FirstOrDefault();
        Assert.IsNotNull(admin);
        Assert.AreEqual(Constants.User.AdminId, admin.Id);
        Assert.IsNull(admin["DisplayName"]); // because we did not select this field

        // ACT-2 Load by id
        request = new LoadReferenceRequest
        {
            ContentId = 7, // Administrators group
            FieldName = "Members",
            Select = new[] { "Id", "Path" }
        };

        admins = await repository.LoadReferencesAsync(request, cancel).ConfigureAwait(false);
        // ASSERT-2
        admin = admins.FirstOrDefault();
        Assert.IsNotNull(admin);
        Assert.AreEqual(Constants.User.AdminId, admin.Id);
        Assert.IsNull(admin["DisplayName"]); // because we did not select this field
    }
    [TestMethod]
    public async Task IT_Repository_LoadReferences_MultiRef_WithFilter()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT loads references and expands their reference field.
        var response = await repository.LoadReferencesAsync(new LoadReferenceRequest
        {
            Path = Constants.Group.AdministratorsPath,
            FieldName = "Members",
            Expand = new[] {"CreatedBy"},
            Select = new[]
            {
                "Id", "Name", "Path", "Index", "Type",
                "CreatedBy/Id", "CreatedBy/Name", "CreatedBy/Path", "CreatedBy/Type", "CreatedBy/CreationDate",
                "CreatedBy/Index"
            },
            OrderBy = new[] {"Id"},
            Top = 1,
            Skip = 1
        }, cancel).ConfigureAwait(false);

        // ASSERT
        var members = response.ToArray();
        // there are actually 2 members but we skipped one in the filter above
        Assert.AreEqual(1, members.Length);

        dynamic content = members[0];
        string createdByName = content.CreatedBy.Name;

        Assert.AreEqual("Developers", content.Name);
        Assert.AreEqual("Group", (string)content.Type);
        Assert.IsTrue(content.Index > -1);
        Assert.AreEqual("Admin", createdByName);
    }

    [TestMethod]
    public async Task IT_Content_LoadReferences_SingleRef()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT: Load content then load reference
        var content = await repository.LoadContentAsync<PortalRoot>(2, cancel).ConfigureAwait(false);
        var multiRef = await content.LoadReferencesAsync("CreatedBy", cancel).ConfigureAwait(false);
        var singleRef = await content.LoadReferenceAsync("CreatedBy", cancel).ConfigureAwait(false);
        var multiRefUser = await content.LoadReferencesAsync<User>("CreatedBy", cancel).ConfigureAwait(false);
        var singleRefUser = await content.LoadReferenceAsync<User>("CreatedBy", cancel).ConfigureAwait(false);

        // ASSERT
        Assert.AreEqual(1, multiRef.Single().Id);
        Assert.AreEqual(1, singleRef.Id);
        Assert.AreEqual(typeof(ContentCollection<User>), multiRefUser.GetType());
        Assert.AreEqual(typeof(User), multiRefUser.Single().GetType());
        Assert.AreEqual(typeof(User), singleRefUser.GetType());
        Assert.AreEqual(1, multiRefUser.Single().Id);
        Assert.AreEqual(1, singleRefUser.Id);
    }
    [TestMethod]
    public async Task IT_Content_LoadReferences_MultiRef()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT: Load content then load reference
        var content = await repository.LoadContentAsync<Group>(Constants.Group.AdministratorsPath, cancel).ConfigureAwait(false);
        var multiRef = await content.LoadReferencesAsync("Members", cancel).ConfigureAwait(false);
        var singleRef = await content.LoadReferenceAsync("Members", cancel).ConfigureAwait(false);

        // ASSERT
        var refContent = multiRef.FirstOrDefault();
        Assert.IsNotNull(refContent);
        Assert.AreEqual(Constants.User.AdminId, refContent.Id);
        Assert.IsNotNull(refContent["DisplayName"]);

        Assert.IsNotNull(singleRef);
        Assert.AreEqual(Constants.User.AdminId, singleRef.Id);
        Assert.IsNotNull(singleRef["DisplayName"]);
    }
    [TestMethod]
    public async Task IT_Content_LoadReferences_MultiRef_WithFilter()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT Load content then load reference and expands their reference field.
        var content = await repository.LoadContentAsync<Group>(Constants.Group.AdministratorsPath, cancel).ConfigureAwait(false);
        var response = await content.LoadReferencesAsync(new LoadReferenceRequest
        {
            FieldName = "Members",
            Expand = new[] { "CreatedBy" },
            Select = new[]
            {
                "Id", "Name", "Path", "Index", "Type",
                "CreatedBy/Id", "CreatedBy/Name", "CreatedBy/Path", "CreatedBy/Type", "CreatedBy/CreationDate",
                "CreatedBy/Index"
            },
            OrderBy = new[] { "Id" },
            Top = 1,
            Skip = 1
        }, cancel).ConfigureAwait(false);

        // ASSERT
        var members = response.ToArray();
        // there are actually 2 members but we skipped one in the filter above
        Assert.AreEqual(1, members.Length);

        dynamic refContent = members[0];
        string createdByName = refContent.CreatedBy.Name;

        Assert.AreEqual("Developers", refContent.Name);
        Assert.AreEqual("Group", (string)refContent.Type);
        Assert.IsTrue(refContent.Index > -1);
        Assert.AreEqual("Admin", createdByName);
    }

    [TestMethod]
    public async Task IT_Content_LoadReferences_Error_NotSaved()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT Load content then load reference and expands their reference field.
        var content = await repository.LoadContentAsync<Group>(Constants.Group.AdministratorsPath, cancel).ConfigureAwait(false);
        content.Path = null;
        content.Id = 0;
        Exception? exception = null;

        // ACT
        try
        {
            var _ = await content.LoadReferencesAsync(
                new LoadReferenceRequest {FieldName = "Members"}, cancel).ConfigureAwait(false);
            Assert.Fail("The expected InvalidOperationException was not thrown.");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // ASSERT
        Assert.AreEqual("Cannot load references of unsaved content.", exception.Message);
    }
    [TestMethod]
    public async Task IT_Content_LoadReferences_Error_IdIsNotZero()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT Load content then load reference and expands their reference field.
        var content = await repository.LoadContentAsync<Group>(Constants.Group.AdministratorsPath, cancel).ConfigureAwait(false);
        Exception? exception = null;

        // ACT
        try
        {
            var _ = await content.LoadReferencesAsync(
                new LoadReferenceRequest { FieldName = "Members", ContentId = -42 }, cancel).ConfigureAwait(false);
            Assert.Fail("The expected InvalidOperationException was not thrown.");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // ASSERT
        Assert.AreEqual("Do not provide ContentId when load reference of a content instance.", exception.Message);
    }
    [TestMethod]
    public async Task IT_Content_LoadReferences_Error_PathIsNotNull()
    {
        var cancel = new CancellationTokenSource().Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", cancel).ConfigureAwait(false);

        // ACT Load content then load reference and expands their reference field.
        var content = await repository.LoadContentAsync<Group>(Constants.Group.AdministratorsPath, cancel).ConfigureAwait(false);
        Exception? exception = null;

        // ACT
        try
        {
            var _ = await content.LoadReferencesAsync(
                new LoadReferenceRequest { FieldName = "Members", Path = "fake" }, cancel).ConfigureAwait(false);
            Assert.Fail("The expected InvalidOperationException was not thrown.");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // ASSERT
        Assert.AreEqual("Do not provide Path when load reference of a content instance.", exception.Message);
    }

}