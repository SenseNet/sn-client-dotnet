using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Authentication;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class TokenStoreTests
    {
        [TestMethod]
        public async Task GetTokenTwice()
        {
            var ts = new TokenStore(new Logger<TokenStore>(new NullLoggerFactory()));
            var server = new ServerContext()
            {
                Url = "https://localhost:44362"
            };
            var token = await ts.GetTokenAsync(server, "secret");
            var token2 = await ts.GetTokenAsync(server, "secret");

            Assert.AreEqual(token, token2);
        }
    }
}
