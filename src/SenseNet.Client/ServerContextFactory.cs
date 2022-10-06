using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.Client.Authentication;

namespace SenseNet.Client
{
    /// <summary>
    /// Internal config class for holding repository config instances. Used by the factory service
    /// that creates and serves server objects.
    /// </summary>
    internal class ServerContextOptions
    {
        public static string DefaultServerName => string.Empty;

        internal IDictionary<string, RepositoryOptions> ServerOptions { get; } = new Dictionary<string, RepositoryOptions>();

        public void AddServer(string name, RepositoryOptions options)
        {
            ServerOptions[name] = options;
        }
    }

    /// <summary>
    /// Defines methods for a server context factory that provides pre-built and cached
    /// server context objects containing authentication tokens.
    /// </summary>
    public interface IServerContextFactory
    {
        /// <summary>
        /// Gets the default server context.
        /// </summary>
        Task<ServerContext> GetServerAsync();

        /// <summary>
        /// Gets a named server context.
        /// </summary>
        /// <param name="name">Server context name.</param>
        Task<ServerContext> GetServerAsync(string name);
    }
    
    internal class ServerContextFactory : IServerContextFactory
    {
        private readonly ITokenStore _tokenStore;
        private readonly ILogger<ServerContextFactory> _logger;
        private readonly ServerContextOptions _serverContextOptions;
        private readonly RepositoryOptions _repositoryOptions;
        private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);
        private readonly IDictionary<string, ServerContext> _servers = new Dictionary<string, ServerContext>();

        public ServerContextFactory(ITokenStore tokenStore, IOptions<ServerContextOptions> serverContextOptions,
            IOptions<RepositoryOptions> repositoryOptions, ILogger<ServerContextFactory> logger)
        {
            _tokenStore = tokenStore;
            _logger = logger;
            _serverContextOptions = serverContextOptions.Value;
            _repositoryOptions = repositoryOptions.Value;
        }
        
        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public async Task<ServerContext> GetServerAsync(string name)
        {
            name ??= ServerContextOptions.DefaultServerName;

            if (_servers.TryGetValue(name, out var server))
                return server;

            await _asyncLock.WaitAsync();

            try
            {
                if (_servers.TryGetValue(name, out server))
                    return server;

                server = await GetAuthenticatedServerAsync(name).ConfigureAwait(false);

                if (server != null)
                    _servers[name] = server;
            }
            finally
            {
                _asyncLock.Release();
            }

            return server;
        }

        public Task<ServerContext> GetServerAsync()
        {
            // default server context
            return GetServerAsync(ServerContextOptions.DefaultServerName);
        }

        private async Task<ServerContext> GetAuthenticatedServerAsync(string name)
        {
            // Check if a named server was configured. Fallback to default options.
            if (!_serverContextOptions.ServerOptions.TryGetValue(name, out var options))
                options = _repositoryOptions;

            var server = new ServerContext
            {
                Url = options.Url.AppendSchema()
            };

            server.Authentication.AccessToken = await _tokenStore.GetTokenAsync(server,
                options.Authentication.ClientId, options.Authentication.ClientSecret);

            if (string.IsNullOrEmpty(server.Authentication.AccessToken))
                _logger.LogWarning($"Could not obtain authentication token value for {server.Url}");

            return server;
        }
    }
}
