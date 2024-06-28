using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SenseNet.Client.IntegrationTests.Legacy
{
    [TestClass]
    public class CertificateValidationTests : IntegrationTestBase
    {
        private CancellationToken _cancel => CancellationToken.None;

        [ClassInitialize]
        public static void ClassInitializer(TestContext context)
        {
            Initializer.InitializeServer(context);
        }


        private static bool _serverCertificateCustomValidationCallbackCalled;
        private readonly Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>
            _serverCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) =>
            {
                _serverCertificateCustomValidationCallbackCalled = true;
                return true;
            };

        [TestMethod]
        public async Task Cert_Validation()
        {
            var repository = await GetRepositoryCollection()
                .GetRepositoryAsync("local", _cancel).ConfigureAwait(false);

            var defaultServer = ClientContext.Current.Server;
            var regularServer = new ServerContext()
            {
                IsTrusted = false,
                ServerCertificateCustomValidationCallback = _serverCertificateCustomValidationCallback,
                Url = defaultServer.Url,
                Username = defaultServer.Username,
                Password = defaultServer.Password,
            };

            var trustedServer = new ServerContext()
            {
                IsTrusted = true,
                ServerCertificateCustomValidationCallback = _serverCertificateCustomValidationCallback,
                Url = defaultServer.Url,
                Username = defaultServer.Username,
                Password = defaultServer.Password,
            };

            // ACTION-1
            _serverCertificateCustomValidationCallbackCalled = false;
            repository.Server = regularServer;
            _ = await repository.LoadContentAsync("/Root", _cancel);
            // ASSERT-1
            Assert.IsFalse(_serverCertificateCustomValidationCallbackCalled);

            // ACTION-2
            _serverCertificateCustomValidationCallbackCalled = false;
            repository.Server = trustedServer;
            _ = await repository.LoadContentAsync("/Root", _cancel);
            // ASSERT-2
            Assert.IsTrue(_serverCertificateCustomValidationCallbackCalled);
        }
    }
}
