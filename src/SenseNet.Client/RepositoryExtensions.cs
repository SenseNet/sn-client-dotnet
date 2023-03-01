using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Client;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Registers services necessary for connecting to a sensenet repository service.
        /// </summary>
        [Obsolete("Use AddSenseNetClient and ConfigureSenseNetRepository instead.")]
        public static IServiceCollection AddSenseNetRepository(this IServiceCollection services,
            Action<RepositoryOptions> configure)
        {
            services.AddSenseNetRepository(ServerContextOptions.DefaultServerName, configure);

            return services;
        }
        /// <summary>
        /// Registers services necessary for connecting to a named sensenet repository service.
        /// </summary>
        [Obsolete("Use AddSenseNetClient and ConfigureSenseNetRepository instead.")]
        public static IServiceCollection AddSenseNetRepository(this IServiceCollection services,
            string name, Action<RepositoryOptions> configure)
        {
            // add all services and options needed to connect to a sensenet repository
            services.AddSenseNetClient()
                .ConfigureSenseNetRepository(name, configure);

            return services;
        }

        /// <summary>
        /// Adds all the features required to connect to a sensenet repository service.
        /// </summary>
        /// <remarks>After calling this method please configure a repository using
        /// the ConfigureSenseNetRepository registration method.</remarks>
        public static IServiceCollection AddSenseNetClient(this IServiceCollection services)
        {
            return services.AddSenseNetClientTokenStore()
                .AddSingleton<IServerContextFactory, ServerContextFactory>()
                .AddSingleton<IRepositoryCollection, RepositoryCollection>()
                .AddSingleton<IRestCaller, DefaultRestCaller>()
                .AddTransient<IRepository, Repository>()
                .AddTransient<Content, Content>()
                .Configure<ServerContextOptions>(_ => { });
        }

        /// <summary>
        /// Configures the unnamed sensenet repository.
        /// </summary>
        /// <remarks>
        /// Note that there can be only one unnamed repository in an application. If you want to
        /// connect to multiple repositories, please register them by name.
        /// </remarks>
        public static IServiceCollection ConfigureSenseNetRepository(this IServiceCollection services, 
            Action<RepositoryOptions> configure)
        {
            return services.ConfigureSenseNetRepository(ServerContextOptions.DefaultServerName, configure);
        }
        /// <summary>
        /// Configures a named sensenet repository.
        /// </summary>
        public static IServiceCollection ConfigureSenseNetRepository(this IServiceCollection services,
            string name, Action<RepositoryOptions> configure)
        {
            services.Configure<ServerContextOptions>(opt =>
            {
                var ro = new RepositoryOptions();
                configure?.Invoke(ro);

                opt.AddServer(name, ro);
            });

            return services;
        }
    }
}
