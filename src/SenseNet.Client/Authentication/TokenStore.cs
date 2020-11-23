using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SenseNet.Client.Authentication
{
    /// <summary>
    /// Defines methods for handling authentication tokens for multiple repositories.
    /// </summary>
    public interface ITokenStore
    {
        /// <summary>
        /// Gets an access token from an authority. The authority is discovered from the content
        /// repository service defined by the server parameter.
        /// </summary>
        /// <param name="server">A server object that defines the content repository service.</param>
        /// <param name="clientId">Client id necessary for the token request.</param>
        /// <param name="secret">A secret that is necessary for the token to be requested.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>An access token or null.</returns>
        Task<string> GetTokenAsync(ServerContext server, string clientId, string secret,
            CancellationToken cancel = default);
    }

    internal class TokenStore : ITokenStore
    {
        private readonly ILogger<TokenStore> _logger;
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly ITokenProvider _tokenProvider;

        public TokenStore(ITokenProvider tokenProvider, ILogger<TokenStore> logger)
        {
            _tokenProvider = tokenProvider;
            _logger = logger;
        }
        
        public async Task<string> GetTokenAsync(ServerContext server, string clientId, string secret,
            CancellationToken cancel = default)
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

                authInfo = await _tokenProvider.GetAuthorityInfoAsync(server, cancel).ConfigureAwait(false);
                
                // set client id if provided
                if (!string.IsNullOrEmpty(clientId))
                    authInfo.ClientId = clientId;

                if (!string.IsNullOrEmpty(authInfo.Authority))
                    _cache.Set(authInfoCacheKey, authInfo, TimeSpan.FromMinutes(30));
            }

            if (string.IsNullOrEmpty(authInfo.Authority))
                return string.Empty;

            _logger?.LogTrace($"Getting token from {authInfo.Authority}.");

            var tokenInfo = await _tokenProvider.GetTokenFromAuthorityAsync(authInfo, secret, cancel)
                .ConfigureAwait(false);

            accessToken = tokenInfo?.AccessToken;

            //TODO: determine access token cache expiration based on the token expiration
            if (!string.IsNullOrEmpty(accessToken))
                _cache.Set(tokenCacheKey, accessToken, TimeSpan.FromMinutes(10));

            return accessToken;
        }
    }
}
