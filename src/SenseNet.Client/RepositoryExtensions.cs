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
            Action<RepositoryOptions> configure, Action<RegisteredContentTypes> registerContentTypes = null)
        {
            return services.ConfigureSenseNetRepository(ServerContextOptions.DefaultServerName, configure, registerContentTypes);
        }
        /// <summary>
        /// Configures options for a named sensenet repository service.
        /// </summary>
        public static IServiceCollection ConfigureSenseNetRepository(this IServiceCollection services,
            string name, Action<RepositoryOptions> configure, Action<RegisteredContentTypes> registerContentTypes = null)
        {
            var registeredContentTypes = new RegisteredContentTypes();
            if (registerContentTypes != null)
            {
                registerContentTypes(registeredContentTypes);
                foreach (var contentType in registeredContentTypes.ContentTypes.Values)
                    services.AddTransient(contentType, contentType);
            }
            services.Configure<ServerContextOptions>(opt =>
            {
                var ro = new RepositoryOptions();
                configure?.Invoke(ro);
                ro.RegisteredContentTypes = registeredContentTypes;

                opt.AddServer(name, ro);
            });

            return services;
        }

        public static IServiceCollection RegisterGlobalContentType(this IServiceCollection services, Type contentType, string contentTypeName = null)
        {
            services.AddTransient(contentType, contentType);
            services.Configure<RegisteredContentTypes>(contentTypes =>
            {
                contentTypes.ContentTypes.Add(contentTypeName ?? contentType.Name, contentType);
            });
            return services;
        }
        public static IServiceCollection RegisterGlobalContentType<T>(this IServiceCollection services, string contentTypeName = null) where T : Content
        {
            services.AddTransient<T, T>();
            services.Configure<RegisteredContentTypes>(contentTypes =>
            {
                contentTypes.ContentTypes.Add(contentTypeName ?? typeof(T).Name, typeof(T));
            });
            return services;
        }
    }

    [DebuggerDisplay("RegisteredContentTypes: {ContentTypes.Count}")]
    public class RegisteredContentTypes
    {
        internal IDictionary<string, Type> ContentTypes { get; } = new Dictionary<string, Type>();

        public RegisteredContentTypes Add(Type contentType, string contentTypeName = null)
        {
            ContentTypes.Add(contentTypeName ?? contentType.Name, contentType);
            return this;
        }
        public RegisteredContentTypes Add<T>(string contentTypeName = null) where T : Content
        {
            ContentTypes.Add(contentTypeName ?? typeof(T).Name, typeof(T));
            return this;
        }
    }
}
