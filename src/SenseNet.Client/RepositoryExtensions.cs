using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Client;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection;

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
            .AddTransient<IRestCaller, DefaultRestCaller>()
            .AddTransient<IRepository, Repository>()
            .AddTransient<Content, Content>()
            .AddSenseNetRetrier()
            .AddLogging()
            .Configure<ServerContextOptions>(_ => { });
    }

    /// <summary>
    /// Configures the unnamed sensenet repository.
    /// </summary>
    /// <remarks>
    /// Note that there can be only one unnamed repository in an application. If you want to
    /// connect to multiple repositories, please register them by name.
    /// </remarks>
    /// <param name="services"></param>
    /// <param name="configure">Callback for configuring <see cref="RepositoryOptions"/> instance.</param>
    /// <param name="registerContentTypes">Optional callback for register custom content types.</param>
    /// <returns></returns>
    public static IServiceCollection ConfigureSenseNetRepository(this IServiceCollection services, 
        Action<RepositoryOptions> configure, Action<RegisteredContentTypes> registerContentTypes = null)
    {
        return services.ConfigureSenseNetRepository(ServerContextOptions.DefaultServerName, configure, registerContentTypes);
    }
    /// <summary>
    /// Configures a named sensenet repository.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="name">Name of the repository.</param>
    /// <param name="configure">Callback for configuring <see cref="RepositoryOptions"/> instance.</param>
    /// <param name="registerContentTypes">Optional callback for register custom content types.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Registers a global content type.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="contentType">Type of the custom content to register.</param>
    /// <param name="contentTypeName">Name if the content type if it is different from the <paramref name="contentType"/> name.</param>
    /// <returns></returns>
    public static IServiceCollection RegisterGlobalContentType(this IServiceCollection services, Type contentType, string contentTypeName = null)
    {
        services.AddTransient(contentType, contentType);
        services.Configure<RegisteredContentTypes>(contentTypes =>
        {
            contentTypes.ContentTypes.Add(contentTypeName ?? contentType.Name, contentType);
        });
        return services;
    }
    /// <summary>
    /// Registers a global content type.
    /// </summary>
    /// <typeparam name="T">Type of the custom content to register.</typeparam>
    /// <param name="services"></param>
    /// <param name="contentTypeName">Name if the content type if it is different from the given content type name.</param>
    /// <returns></returns>
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
