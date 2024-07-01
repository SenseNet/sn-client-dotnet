namespace SenseNet.Client.IntegrationTests.Legacy
{
    [TestClass]
    public class RenameTests : IntegrationTestBase
    {
        private static readonly string ROOTPATH = "/Root/_RenameTests";
        private readonly CancellationToken _cancel = CancellationToken.None;

        [TestMethod]
        public async Task Rename_Folder_01()
        {
            var cancel = new CancellationTokenSource().Token;
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", CancellationToken.None).ConfigureAwait(false);

            await Tools.EnsurePathAsync(ROOTPATH, null, repository, _cancel).ConfigureAwait(false);

            var parent = repository.CreateContent(ROOTPATH, "Folder", "Parent-" + Guid.NewGuid());
            //var parent = Content.CreateNew(ROOTPATH, "Folder", "Parent-" + Guid.NewGuid());
            await parent.SaveAsync(cancel).ConfigureAwait(false);

            parent.Name = parent.Name + "-Renamed";
            await parent.SaveAsync(cancel).ConfigureAwait(false);

            var child = repository.CreateContent(parent.Path, "Folder", "Child");
            //var child = Content.CreateNew(parent.Path, "Folder", "Child");
            await child.SaveAsync(cancel).ConfigureAwait(false);

            parent.Name = parent.Name + "-Renamed2";
            await parent.SaveAsync(cancel).ConfigureAwait(false);

            child = await repository.LoadContentAsync(child.Id, cancel).ConfigureAwait(false);
            //child = await Content.LoadAsync(child.Id).ConfigureAwait(false);

            Assert.AreEqual(parent.Path + "/" + child.Name, child.Path);
        }

        [TestInitialize]
        public void InitializeTest(TestContext context)
        {
            var repository = GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).GetAwaiter().GetResult();

            Initializer.InitializeServer(context);

            var root = repository.LoadContentAsync(ROOTPATH, _cancel).Result;
            root?.DeleteAsync(true, _cancel).ConfigureAwait(false).GetAwaiter().GetResult();
        }

    }
}
