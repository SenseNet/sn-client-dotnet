﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SenseNet.Extensions.DependencyInjection;

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
        /// Can be true in development scenarios that means server certificate validation is skipped.
        /// This value needs to be false (default) in production.
        /// </summary>
        public bool IsTrusted { get; set; }
        /// <summary>
        /// Custom certificate validation method. Default is null that means all certificates 
        /// are trusted if the <see cref="IsTrusted"/> flag is set to True.
        /// </summary>
        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }

        public AuthenticationInfo Authentication { get; } = new AuthenticationInfo();

        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets the current user content based on the available authentication token.
        /// If there is no token or it is invalid, the method returns the Visitor user.
        /// </summary>
        /// <param name="select">Fields to select.</param>
        /// <param name="expand">Fields to expand.</param>
        public Task<Content> GetCurrentUserAsync(string[] select = null, string[] expand = null)
        {
            ODataRequest request = null;

            if (!string.IsNullOrEmpty(Authentication?.AccessToken))
            {
                // The token contains the user id in the SUB claim.
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtSecurityToken = handler.ReadJwtToken(Authentication.AccessToken);

                    if (int.TryParse(jwtSecurityToken.Subject, out var contentId))
                        request = new ODataRequest(this)
                        {
                            ContentId = contentId,
                            Select = select,
                            Expand = expand
                        };
                }
                catch (Exception ex)
                {
                    Logger?.LogTrace(ex, "Error during JWT access token conversion.");
                }
            }

            // no token or invalid: load Visitor
            request ??= new ODataRequest(this)
            {
                Path = Constants.User.VisitorPath,
                Select = select,
                Expand = expand
            };

            return Content.LoadAsync(request, this);
        }

        /// <summary>
        /// Creates a new server context object filled with the properties of the current instance.
        /// </summary>
        internal ServerContext Clone()
        {
            var server = new ServerContext
            {
                Username = this.Username,
                Password = this.Password,
                Url = this.Url,
                IsTrusted = this.IsTrusted,
                Logger = this.Logger,
                ServerCertificateCustomValidationCallback = this.ServerCertificateCustomValidationCallback,
                Authentication =
                {
                    AccessToken = this.Authentication.AccessToken,
                    RefreshToken = this.Authentication.RefreshToken
                }
            };

            return server;
        }

        //============================================================================= Static API
        
        /// <summary>
        /// Gets a URL from a server instance for sending requests. In case of a null
        /// instance the first one from the currently configured list will be used.
        /// </summary>
        /// <param name="server">Server context object.</param>
        public static string GetUrl(ServerContext server)
        {
            server ??= ClientContext.Current.Server;
            return server.Url;
        }

        public static bool DefaultServerCertificateCustomValidationCallback(HttpRequestMessage arg1, X509Certificate2 arg2, X509Chain arg3, SslPolicyErrors arg4)
        {
            return true;
        }
    }
}
