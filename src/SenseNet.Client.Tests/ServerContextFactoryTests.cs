using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Authentication;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class ServerContextFactoryTests
    {
        [TestMethod]
        public async Task ServerContext_ConfigureDefault()
        {
            const string exampleUrl = "https://example.com";
            var scf = GetFactory(services => { services.ConfigureSenseNetRepository(opt => { opt.Url = exampleUrl; }); });
            var server = await scf.GetServerAsync();

            Assert.AreEqual(exampleUrl, server.Url);
        }
        [TestMethod]
        public async Task ServerContext_ConfigureNamed()
        {
            const string exampleUrl = "https://example.com";
            var scf = GetFactory(services => { services.ConfigureSenseNetRepository("x", opt =>
            {
                opt.Url = exampleUrl; 
            }); });

            // default should be empty
            var server0 = await scf.GetServerAsync();
            Assert.AreNotEqual(exampleUrl, server0?.Url);

            var server1 = await scf.GetServerAsync("x");
            Assert.AreEqual(exampleUrl, server1.Url);

            // get the same server again
            var server2 = await scf.GetServerAsync("x");
            Assert.AreEqual(exampleUrl, server2.Url);
        }
        [TestMethod]
        public async Task ServerContext_ConfigureMultipleNamed()
        {
            const string exampleUrl1 = "https://example1.com";
            const string exampleUrl2 = "https://example2.com";

            var scf = GetFactory(services => {
                services.ConfigureSenseNetRepository("x", opt => { opt.Url = exampleUrl1; });
                services.ConfigureSenseNetRepository("y", opt => { opt.Url = exampleUrl2; });
            });

            var server1 = await scf.GetServerAsync("x");
            Assert.AreEqual(exampleUrl1, server1.Url);

            // get the other server
            var server2 = await scf.GetServerAsync("y");
            Assert.AreEqual(exampleUrl2, server2.Url);
        }

        [TestMethod]
        public async Task ServerContext_CustomToken()
        {
            const string exampleUrl1 = "https://example1.com";

            var scf = GetFactory(services =>
            {
                services.ConfigureSenseNetRepository(opt =>
                {
                    opt.Url = exampleUrl1;
                });
            });

            var server1 = await scf.GetServerAsync(token:"token1");
            Assert.AreEqual("token1", server1.Authentication.AccessToken);
        }
        [TestMethod]
        public async Task ServerContext_CustomToken_MultipleNamed()
        {
            const string exampleUrl1 = "https://example1.com";
            const string exampleUrl2 = "https://example2.com";

            var scf = GetFactory(services =>
            {
                services.ConfigureSenseNetRepository("x", opt => { opt.Url = exampleUrl1; });
                services.ConfigureSenseNetRepository("y", opt => { opt.Url = exampleUrl2; });
            });

            var server1 = await scf.GetServerAsync("x", "token1");
            var server2 = await scf.GetServerAsync("y", "token2");

            Assert.AreEqual("token1", server1.Authentication.AccessToken);
            Assert.AreEqual("token2", server2.Authentication.AccessToken);
            Assert.AreNotEqual(server1.Url, server2.Url);
        }

        private static IServerContextFactory GetFactory(Action<IServiceCollection> addServices)
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services
                .AddSingleton<ITokenProvider, TestTokenProvider>()
                .AddSingleton<ITokenStore, TokenStore>()
                .AddSingleton<IServerContextFactory, ServerContextFactory>();

            addServices?.Invoke(services);

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IServerContextFactory>();
        }
    }
}
