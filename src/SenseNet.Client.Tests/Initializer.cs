using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class Initializer
    {
        [AssemblyInitialize]
        public static void InitializeAllTests(TestContext context)
        {
            ClientContext.Current.AddServers(new[]
            {
                new ServerContext
                {
                    Url = "http://localhost",
                    Username = "admin",
                    Password = "admin"
                }
            });

            // for testing purposes
            //ClientContext.Current.ChunkSizeInBytes = 1024;
        }
    }
}