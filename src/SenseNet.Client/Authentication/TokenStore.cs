using System;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SenseNet.Client.Authentication
{
    /// <summary>
    /// Handles authentication tokens for multiple content repositories.
    /// </summary>
    public class TokenStore
    {
        private readonly ILogger<TokenStore> _logger;
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public TokenStore(ILogger<TokenStore> logger)
        {
            _logger = logger;
        }

        public async Task<string> GetTokenAsync(ServerContext server, string secret)
        {
            // look for the token in the cache
            var tokenCacheKey = "TK:" + server.Url;
            if (_cache.TryGetValue(tokenCacheKey, out string accessToken))
                return accessToken;

            // look for auth info in the cache
            var authInfoCacheKey = "AI:" + server.Url;
            if (!_cache.TryGetValue(authInfoCacheKey, out AuthorityInfo authInfo))
            {
                authInfo = await GetAuthorityInfo(server).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(authInfo.Authority))
                    _cache.Set(authInfoCacheKey, authInfo, TimeSpan.FromMinutes(30));
            }

            if (string.IsNullOrEmpty(authInfo.Authority))
                return string.Empty;

            accessToken = await GetTokenFromAuthorityAsync(authInfo, secret)
                .ConfigureAwait(false);

            //TODO: determine access token cache expiration based on the token expiration
            if (!string.IsNullOrEmpty(accessToken))
                _cache.Set(tokenCacheKey, accessToken, TimeSpan.FromMinutes(5));

            return accessToken;
        }

        private async Task<string> GetTokenFromAuthorityAsync(AuthorityInfo authorityInfo, string secret)
        {
            using var client = new System.Net.Http.HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(authorityInfo.Authority).ConfigureAwait(false);
            if (disco.IsError)
            {
                _logger?.LogError($"Error during discovery document request to authority {authorityInfo.Authority}.");
                return string.Empty;
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
                return string.Empty;
            }
            
            return tokenResponse.AccessToken;
        }

        private async Task<AuthorityInfo> GetAuthorityInfo(ServerContext server)
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
