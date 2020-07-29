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
                _logger?.LogTrace($"Getting authority info from {server.Url}.");

                authInfo = await _tokenProvider.GetAuthorityInfoAsync(server).ConfigureAwait(false);

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
