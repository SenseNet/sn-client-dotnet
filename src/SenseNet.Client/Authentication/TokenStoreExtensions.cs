using Microsoft.Extensions.DependencyInjection;
using SenseNet.Client.Authentication;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class TokenStoreExtensions
    {
        public static IServiceCollection AddSenseNetClientTokenStore(this IServiceCollection services)
        {
            services.AddSingleton<ITokenProvider, IdentityServerTokenProvider>();
            services.AddSingleton<ITokenStore, TokenStore>();

            return services;
        }
    }
}
