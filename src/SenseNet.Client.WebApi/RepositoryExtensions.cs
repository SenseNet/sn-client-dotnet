using Microsoft.Extensions.DependencyInjection;
using SenseNet.Client;
using SenseNet.Client.WebApi;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Adds sensenet client services and registers three repositories: User, Visitor and Admin.
        /// All repositories are configured with the same options, the only difference is the authentication. <br/><br/>
        /// - the <b>Visitor</b> repository will not have any<br/>
        /// - the <b>Admin</b> repository will use the configured Admin user<br/>
        /// - the <b>User</b> repository will try to authenticate using the current user's token available in HttpContext<br/>
        /// <br/>
        /// To make advantage of these repositories, use the <see cref="IUserRepositoryCollection"/> service
        /// in your classes.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure">Callback for configuring <see cref="RepositoryOptions"/> instance.</param>
        /// <param name="registerContentTypes">Optional callback for registering custom content types.</param>
        public static IServiceCollection AddSenseNetClientWithUserRepositories(this IServiceCollection services,
            Action<RepositoryOptions> configure, Action<RegisteredContentTypes>? registerContentTypes = null)
        {
            services.AddHttpContextAccessor();
            services.AddSenseNetClient();

            services.ConfigureSenseNetRepository(Repositories.VisitorRepository,
                    ConfigureWithEmptyAuthentication(configure),
                    registerContentTypes)
                .ConfigureSenseNetRepository(Repositories.UserRepository,
                    ConfigureWithEmptyAuthentication(configure),
                    registerContentTypes)
                .ConfigureSenseNetRepository(Repositories.AdminRepository,
                    configure,
                    registerContentTypes);

            // this can be singleton, because it requires only singleton services like IHttpContextAccessor
            services.AddSingleton<IUserRepositoryCollection, UserRepositoryCollection>();

            return services;
        }
        
        private static Action<RepositoryOptions> ConfigureWithEmptyAuthentication(Action<RepositoryOptions>? configure)
        {
            return options =>
            {
                configure?.Invoke(options);

                // clear all auth data to prevent accidental use of client credentials
                options.Authentication.ClientId = string.Empty;
                options.Authentication.ClientSecret = string.Empty;
                options.Authentication.ApiKey = string.Empty;
            };
        }
    }
}
