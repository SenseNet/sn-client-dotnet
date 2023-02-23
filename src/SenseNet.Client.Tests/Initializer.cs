using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class Initializer
    {
        [AssemblyInitialize]
        public static void InitializeAllTests(TestContext context)
        {
            InitializeServer();
        }

        public static void InitializeServer()
        {
            ClientContext.Current.RemoveAllServers();
            ClientContext.Current.AddServers(new[]
            {
                new ServerContext
                {
                    Url = "https://localhost:44362",
                    Authentication =
                    {
                        ApiKey = ""
                    }
                }
            });

            // for testing purposes
            //ClientContext.Current.ChunkSizeInBytes = 1024;
        }
    }
}