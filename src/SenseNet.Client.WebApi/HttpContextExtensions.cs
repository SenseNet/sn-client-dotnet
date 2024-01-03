using Microsoft.AspNetCore.Http;

namespace SenseNet.Client.WebApi
{
    internal static class HttpContextExtensions
    {
        private const string BearerPrefix = "Bearer ";

        /// <summary>
        /// Returns the bearer token from the Authorization header without the prefix.
        /// </summary>
        /// <param name="context">The current HttpContext instance.</param>
        /// <returns>The token or null.</returns>
        public static string? GetBearerToken(this HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Authorization", out var token))
                return null;

            var tokenString = token.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(tokenString))
                return null;

            // the token is expected to start with the bearer prefix
            return tokenString.StartsWith(BearerPrefix) ? tokenString[BearerPrefix.Length..] : null;
        }
    }
}
