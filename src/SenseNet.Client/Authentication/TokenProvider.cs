using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;

namespace SenseNet.Client.Authentication
{
    public interface ITokenProvider
    {
        Task<TokenInfo> GetTokenFromAuthorityAsync(AuthorityInfo authorityInfo, string secret);
        Task<AuthorityInfo> GetAuthorityInfoAsync(ServerContext server);
    }

    internal class IdentityServerTokenProvider : ITokenProvider
    {
        private readonly ILogger<IdentityServerTokenProvider> _logger;

        public IdentityServerTokenProvider(ILogger<IdentityServerTokenProvider> logger)
        {
            _logger = logger;
        }

        public async Task<TokenInfo> GetTokenFromAuthorityAsync(AuthorityInfo authorityInfo, string secret)
        {
            using var client = new System.Net.Http.HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(authorityInfo.Authority).ConfigureAwait(false);
            if (disco.IsError)
            {
                _logger?.LogError($"Error during discovery document request to authority {authorityInfo.Authority}.");
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
            });

            if (tokenResponse.IsError)
            {
                _logger?.LogError($"Error during requesting client credentials from authority {authorityInfo.Authority}.");
                return null;
            }

            return tokenResponse.ToTokenInfo();
        }
        public async Task<AuthorityInfo> GetAuthorityInfoAsync(ServerContext server)
        {
            var req = new ODataRequest(server)
            {
                Path = "/Root",
                ActionName = "GetClientRequestParameters"
            };

            // The client type is hardcoded because the real client id
            // is provided by the repository using the request below.
            req.Parameters.Add("clientType", "client");

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
                _logger?.LogError($"Could not access repository {server.Url} for getting the authority url. {ex.Message}");
            }

            return AuthorityInfo.Empty;
        }
    }
}
