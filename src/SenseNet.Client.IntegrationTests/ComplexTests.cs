using System.Diagnostics;

namespace SenseNet.Client.IntegrationTests;

[TestClass]
public class ComplexTests : IntegrationTestBase
{
    private readonly CancellationToken _cancel = new CancellationTokenSource().Token;

    [TestMethod]
    public async Task IT_Complex_HandleUsersGroups_Membership()
    {
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", CancellationToken.None).ConfigureAwait(false);

        await repository.DeleteContentAsync(new[]
        {
            "/Root/IMS/Public/Group-3",
            "/Root/IMS/Public/Group-2",
            "/Root/IMS/Public/Group-1",
            "/Root/IMS/Public/User-1"
        }, true, _cancel).ConfigureAwait(false);

        var publicDomainPath = "/Root/IMS/Public";
        var expand = new[] {"Members"};
        var select = new[] {"*", "Members/Id", "Members/Path", "Members/Name"};

        var admin = await repository.LoadContentAsync<User>(1, _cancel).ConfigureAwait(false);

        // ACT-1: Creation
        var user1 = repository.CreateContent<User>(publicDomainPath, null, "User-1");
        user1.FullName = "Rhoda Scott";
        user1.LoginName = "rscott";
        user1.Email = "rscott@example.com";
        user1.BirthDate = new DateTime(1938, 06, 03);
        user1.Enabled = true;
        user1.Gender = Gender.Female;
        user1.MaritalStatus = MaritalStatus.Married;
        user1.Manager = admin;
        user1["Password"] = "password1";
        await user1.SaveAsync(_cancel);

        var group1 = repository.CreateContent<Group>(publicDomainPath, null, "Group-1");
        await group1.SaveAsync(_cancel);
        var group2 = repository.CreateContent<Group>(publicDomainPath, null, "Group-2");
        group2.Members = new Content[] {user1, group1};
        await group2.SaveAsync(_cancel);
        var group3 = repository.CreateContent<Group>(publicDomainPath, null, "Group-3");
        group3.Members = new Content[] { group1 };
        await group3.SaveAsync(_cancel);

        // ASSERT-1
        Assert.IsTrue(user1.Id > 0);
        Assert.IsTrue(group1.Id > 0);
        Assert.IsTrue(group2.Id > 0);
        // check membership
        var loadedGroup2 = await repository.LoadContentAsync<Group>(
            new LoadContentRequest {ContentId = group2.Id, Expand = expand, Select = select}, _cancel).ConfigureAwait(false);
        Assert.IsNotNull(loadedGroup2?.Members);
        var loadedMemberNames2 = loadedGroup2.Members.Select(c => c.Name).OrderBy(x => x);
        Assert.AreEqual("Group-1 User-1", string.Join(" ", loadedMemberNames2));
        var loadedGroup3 = await repository.LoadContentAsync<Group>(
            new LoadContentRequest {ContentId = group3.Id, Expand = expand, Select = select}, _cancel).ConfigureAwait(false);
        Assert.IsNotNull(loadedGroup2?.Members);
        var loadedMemberNames3 = loadedGroup3.Members.Select(c => c.Name).OrderBy(x => x);
        Assert.AreEqual("Group-1", string.Join(" ", loadedMemberNames3));

        // ACT-2: Move user1 from group2 to group3
        var sourceMembers = loadedGroup2.Members.ToArray();
        var membersToMove = sourceMembers.Where(x => x.Name == "User-1").ToArray();
        loadedGroup2.Members = sourceMembers.Except(membersToMove);
        await loadedGroup2.SaveAsync(_cancel).ConfigureAwait(false);
        loadedGroup3.Members = loadedGroup3.Members.Union(membersToMove);
        await loadedGroup3.SaveAsync(_cancel).ConfigureAwait(false);

        // ASSERT-2 check membership
        var reloadedGroup2 = await repository.LoadContentAsync<Group>(
            new LoadContentRequest {ContentId = group2.Id, Expand = expand, Select = select}, _cancel).ConfigureAwait(false);
        Assert.IsNotNull(loadedGroup2?.Members);
        var reloadedMemberNames2 = reloadedGroup2.Members.Select(c => c.Name).OrderBy(x => x);
        Assert.AreEqual("Group-1", string.Join(" ", reloadedMemberNames2));
        var reloadedGroup3 = await repository.LoadContentAsync<Group>(
            new LoadContentRequest {ContentId = group3.Id, Expand = expand, Select = select}, _cancel).ConfigureAwait(false);
        Assert.IsNotNull(loadedGroup2?.Members);
        var reloadedMemberNames3 = reloadedGroup3.Members.Select(c => c.Name).OrderBy(x => x);
        Assert.AreEqual("Group-1 User-1", string.Join(" ", reloadedMemberNames3));
    }
}