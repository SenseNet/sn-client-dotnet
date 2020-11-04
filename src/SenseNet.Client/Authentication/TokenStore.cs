using System;
using System.Threading.Tasks;
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
        private readonly ITokenProvider _tokenProvider;

        public TokenStore(ITokenProvider tokenProvider, ILogger<TokenStore> logger)
        {
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        /// <summary>
        /// Gets an access token from an authority. The authority is discovered from the content
        /// repository service defined by the server parameter.
        /// This method uses the client id returned by the discovery call from the repository.
        /// If you have a client id, use the other overload.
        /// </summary>
        /// <param name="server">A server object that defines the content repository service.</param>
        /// <param name="secret">A secret that is necessary for the token to be requested.</param>
        /// <returns>An access token or null.</returns>
        public Task<string> GetTokenAsync(ServerContext server, string secret)
        {
            return GetTokenAsync(server, null, secret);
        }

        /// <summary>
        /// Gets an access token from an authority. The authority is discovered from the content
        /// repository service defined by the server parameter.
        /// </summary>
        /// <param name="server">A server object that defines the content repository service.</param>
        /// <param name="clientId">Client id necessary for the token request.</param>
        /// <param name="secret">A secret that is necessary for the token to be requested.</param>
        /// <returns>An access token or null.</returns>
        public async Task<string> GetTokenAsync(ServerContext server, string clientId, string secret)
        {
            // look for the token in the cache
            var tokenCacheKey = "TK:" + server.Url;
            if (_cache.TryGetValue(tokenCacheKey, out string accessToken))
                return accessToken;

            // look for auth info in the cache
            var authInfoCacheKey = "AI:" + server.Url;
            if (!_cache.TryGetValue(authInfoCacheKey, out AuthorityInfo authInfo))
            {
                _logger?.LogTrace($"Getting authority info from {server.Url}.");

                authInfo = await _tokenProvider.GetAuthorityInfoAsync(server).ConfigureAwait(false);

                // set client id if provided
                if (!string.IsNullOrEmpty(clientId))
                    authInfo.ClientId = clientId;

                if (!string.IsNullOrEmpty(authInfo.Authority))
                    _cache.Set(authInfoCacheKey, authInfo, TimeSpan.FromMinutes(30));
            }

            if (string.IsNullOrEmpty(authInfo.Authority))
                return string.Empty;

            _logger?.LogTrace($"Getting token from {authInfo.Authority}.");

            var tokenInfo = await _tokenProvider.GetTokenFromAuthorityAsync(authInfo, secret)
                .ConfigureAwait(false);

            accessToken = tokenInfo?.AccessToken;

            //TODO: determine access token cache expiration based on the token expiration
            if (!string.IsNullOrEmpty(accessToken))
                _cache.Set(tokenCacheKey, accessToken, TimeSpan.FromMinutes(5));

            return accessToken;
        }
    }
}
