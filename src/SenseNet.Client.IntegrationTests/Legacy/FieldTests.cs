using Newtonsoft.Json.Linq;

namespace SenseNet.Client.IntegrationTests.Legacy
{
    [TestClass]
    public class FieldTests : IntegrationTestBase
    {
        //[ClassInitialize]
        //public static void ClassInitializer(TestContext context)
        //{
        //    Initializer.InitializeServer(context);
        //}
        private CancellationToken _cancel = new CancellationToken();

        [TestMethod]
        public async Task LongTextField_Emoji()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var contentId = 0;
            var expected = "prefix 🙂😐🧱🤬👍🏻👉🏻🤘🏻🎱🚴🎸💀☢️7️🕜 suffix";

            try
            {
                var content = repository.CreateContent("/Root", "SystemFolder", Guid.NewGuid().ToString());
                content["DisplayName"] = expected;
                content["Description"] = expected;
                await content.SaveAsync(_cancel).ConfigureAwait(false);
                contentId = content.Id;

                var loaded = await repository.LoadContentAsync(contentId, _cancel).ConfigureAwait(false);
                var displayName = ((JValue)loaded["DisplayName"]).Value<string>();
                var description = ((JValue)loaded["Description"]).Value<string>();
                Assert.AreEqual(expected, displayName);
                Assert.AreEqual(expected, description);
            }
            finally
            {
                await repository.DeleteContentAsync(contentId, true, _cancel)
                    .ConfigureAwait(false);
            }
        }
    }
}
