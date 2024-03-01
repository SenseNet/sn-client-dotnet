using Microsoft.Extensions.DependencyInjection;
using SenseNet.Client.Authentication;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class TokenStoreExtensions
    {
        /// <summary>
        /// Adds the token store services to the service collection. The default
        /// token provider is the IdentityServerTokenProvider.
        /// </summary>
        public static IServiceCollection AddSenseNetClientTokenStore(this IServiceCollection services)
        {
            services.AddSingleton<ITokenProvider, IdentityServerTokenProvider>();
            services.AddSingleton<ITokenStore, TokenStore>();

            return services;
        }
    }
}
