using Newtonsoft.Json.Linq;

namespace SenseNet.Client.IntegrationTests.Legacy
{
    [TestClass]
    public class FieldTests
    {
        [ClassInitialize]
        public static void ClassInitializer(TestContext context)
        {
            Initializer.InitializeServer(context);
        }

        [TestMethod]
        public async Task LongTextField_Emoji()
        {
            var contentId = 0;
            var expected = "prefix 🙂😐🧱🤬👍🏻👉🏻🤘🏻🎱🚴🎸💀☢️7️🕜 suffix";

            try
            {
                var content = Content.CreateNew("/Root", "SystemFolder", Guid.NewGuid().ToString());
                content["DisplayName"] = expected;
                content["Description"] = expected;
                await content.SaveAsync().ConfigureAwait(false);
                contentId = content.Id;

                var loaded = await Content.LoadAsync(contentId).ConfigureAwait(false);
                var displayName = ((JValue)loaded["DisplayName"]).Value<string>();
                var description = ((JValue)loaded["Description"]).Value<string>();
                Assert.AreEqual(expected, displayName);
                Assert.AreEqual(expected, description);
            }
            finally
            {
                await Content.DeleteAsync(contentId, true, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
    }
}
