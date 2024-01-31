namespace SenseNet.Client.IntegrationTests.Legacy
{
    [TestClass]
    public class RenameTests : IntegrationTestBase
    {
        private static readonly string ROOTPATH = "/Root/_RenameTests";

        [TestMethod]
        public async Task Rename_Folder_01()
        {
            var cancel = new CancellationTokenSource().Token;
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", CancellationToken.None).ConfigureAwait(false);

            await Tools.EnsurePathAsync(ROOTPATH).ConfigureAwait(false);

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

        [ClassInitialize]
        public static void Cleanup(TestContext context)
        {
            Initializer.InitializeServer(context);

            var root = Content.LoadAsync(ROOTPATH).Result;
            root?.DeleteAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
