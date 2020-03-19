using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SenseNet.Client
{
    /// <summary>
    /// Represents a connection to a server.
    /// </summary>
    public class ServerContext
    {
        public class AuthenticationInfo
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
        }

        /// <summary>
        /// Username used for authenticating all requests made to this server.
        /// In case of an empty username DefaultCredentials will be used.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Password for the username.
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Server URL.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Can be true in the development scenarios that means the server certificate validation is skipped.
        /// This value needs to be false (default) in production.
        /// </summary>
        public bool IsTrusted { get; set; }
        /// <summary>
        /// Custom certificate validation method. Default is null that means: all certificate is trusted.
        /// </summary>
        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }

        public AuthenticationInfo Authentication { get; } = new AuthenticationInfo();

        //============================================================================= Static API

        /// <summary>
        /// Gets a URL from a server instance for sending requests. In case of a null
        /// instance the first one from the currently configured list will be used.
        /// </summary>
        /// <param name="server">Server context object.</param>
        public static string GetUrl(ServerContext server)
        {
            if (server == null)
                server = ClientContext.Current.Server;
            return server.Url;
        }

        public static bool DefaultServerCertificateCustomValidationCallback(HttpRequestMessage arg1, X509Certificate2 arg2, X509Chain arg3, SslPolicyErrors arg4)
        {
            throw new System.NotImplementedException();
        }
    }
}
