using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests.LegacyIntegrationTests
{
    [TestClass]
    public class CertificateValidationTests
    {
        [ClassInitialize]
        public static void ClassInitializer(TestContext _)
        {
            Initializer.InitializeServer();
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
            var content = await Content.LoadAsync("/Root", regularServer).ConfigureAwait(false);
            // ASSERT-1
            Assert.IsFalse(_serverCertificateCustomValidationCallbackCalled);

            // ACTION-2
            _serverCertificateCustomValidationCallbackCalled = false;
            content = await Content.LoadAsync("/Root", trustedServer);
            // ASSERT-2
            Assert.IsTrue(_serverCertificateCustomValidationCallbackCalled);
        }
    }
}
