using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Authentication;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Client.Tests
{
    internal class TestTokenProvider : ITokenProvider
    {
        public Task<AuthorityInfo> GetAuthorityInfoAsync(ServerContext server)
        {
            return Task.FromResult(new AuthorityInfo
            {
                Authority = Guid.NewGuid().ToString(),
                ClientId = "client"
            });
        }

        public Task<TokenInfo> GetTokenFromAuthorityAsync(AuthorityInfo authorityInfo, string secret)
        {
            return Task.FromResult(new TokenInfo
            {
                AccessToken = Guid.NewGuid().ToString()
            });
        }
    }

    [TestClass]
    public class TokenStoreTests
    {
        [TestMethod]
        public async Task GetTokenTwice()
        {
            var ts = new TokenStore(new TestTokenProvider(),  new Logger<TokenStore>(new NullLoggerFactory()));
            var server = new ServerContext
            {
                Url = "https://localhost:44362"
            };

            var token = await ts.GetTokenAsync(server, "secret");
            var token2 = await ts.GetTokenAsync(server, "secret");

            Assert.AreEqual(token, token2);
        }
    }
}
