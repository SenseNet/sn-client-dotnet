using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static IServiceCollection AddSenseNetRepository(this IServiceCollection services,
            Action<RepositoryOptions> configure)
        {
            services.AddSenseNetRepository(ServerContextOptions.DefaultServerName, configure);

            return services;
        }
        /// <summary>
        /// Registers services necessary for connecting to a named sensenet repository service.
        /// </summary>
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
        public static IServiceCollection AddSenseNetClient(this IServiceCollection services)
        {
            return services.AddSenseNetClientTokenStore()
                .AddSingleton<IServerContextFactory, ServerContextFactory>()
                .AddSingleton<IRepositoryService, RepositoryService>()
                .AddSingleton<IRestCaller, DefaultRestCaller>()
                .AddTransient<IRepository, Repository>()
                .AddTransient<Content, Content>()
                .Configure<ServerContextOptions>(opt => { });
        }

        /// <summary>
        /// Configures default sensenet repository options.
        /// </summary>
        public static IServiceCollection ConfigureSenseNetRepository(this IServiceCollection services, 
            Action<RepositoryOptions> configure)
        {
            return services.ConfigureSenseNetRepository(ServerContextOptions.DefaultServerName, configure);
        }
        /// <summary>
        /// Configures options for a named sensenet repository service.
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
