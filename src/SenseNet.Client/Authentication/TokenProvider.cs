using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;

namespace SenseNet.Client.Authentication
{
    /// <summary>
    /// Defines methods for getting the authority and token for a repository.
    /// </summary>
    public interface ITokenProvider
    {
        /// <summary>
        /// Gets tokens from an authority using the specified secret.
        /// </summary>
        /// <param name="authorityInfo">The authority to send the token request to.</param>
        /// <param name="secret">A secret corresponding to the client id in the authority info.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>Tokens for accessing the repository. Currently only the access token is available.</returns>
        Task<TokenInfo> GetTokenFromAuthorityAsync(AuthorityInfo authorityInfo, 
            string secret, CancellationToken cancel = default);
        /// <summary>
        /// Gets the publicly available authority information for a repository.
        /// </summary>
        /// <param name="server">A server object containing the repository url.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>The authority related to the repository.</returns>
        Task<AuthorityInfo> GetAuthorityInfoAsync(ServerContext server, CancellationToken cancel = default);
    }

    internal class IdentityServerTokenProvider : ITokenProvider
    {
        private readonly ILogger<IdentityServerTokenProvider> _logger;

        public IdentityServerTokenProvider(ILogger<IdentityServerTokenProvider> logger)
        {
            _logger = logger;
        }

        public async Task<TokenInfo> GetTokenFromAuthorityAsync(AuthorityInfo authorityInfo, string secret,
            CancellationToken cancel = default)
        {
            using var client = new System.Net.Http.HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(authorityInfo.Authority, cancel)
                .ConfigureAwait(false);

            if (disco.IsError)
            {
                _logger?.LogError(disco.Exception, $"Error during discovery document request to authority {authorityInfo.Authority}. {disco.Error}");
                return null;
            }
            
            //TODO: Request REFRESH token too and use that to renew the access token after expiration.
            // Currently the token response does not contain the refresh token, it needs to be requested.

            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = authorityInfo.ClientId,
                ClientSecret = secret,
                Scope = "sensenet"
            }, cancel);

            if (tokenResponse.IsError)
            {
                _logger?.LogError(tokenResponse.Exception, "Error during requesting client credentials " +
                                                           $"from authority {authorityInfo.Authority}." +
                                                           $"ClientId: {authorityInfo.ClientId}. {tokenResponse.Error}");
                return null;
            }

            return tokenResponse.ToTokenInfo();
        }
        public async Task<AuthorityInfo> GetAuthorityInfoAsync(ServerContext server, CancellationToken cancel = default)
        {
            var req = new ODataRequest(server)
            {
                Path = "/Root",
                ActionName = "GetClientRequestParameters"
            };

            // The client type is hardcoded because the real client id
            // is provided by the repository using the request below.
            req.Parameters.Add("clientType", "client");

            //TODO: remove this assertion when the GetResponse method below
            // is able to receive a cancellation token.
            cancel.ThrowIfCancellationRequested();

            try
            {
                dynamic response = await RESTCaller.GetResponseJsonAsync(req, server)
                    .ConfigureAwait(false);

                return new AuthorityInfo
                {
                    Authority = response.authority,
                    ClientId = response.client_id
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Could not access repository {server.Url} for getting the authority url.");
            }

            return new AuthorityInfo();
        }
    }
}
