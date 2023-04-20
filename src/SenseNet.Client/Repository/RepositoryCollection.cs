using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    internal class RepositoryCollection : IRepositoryCollection
    {
        private readonly ILogger<RepositoryCollection> _logger;
        private readonly IServiceProvider _services;
        private readonly IServerContextFactory _serverFactory;
        private readonly IDictionary<string, IRepository> _repositories = new ConcurrentDictionary<string, IRepository>();
        private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

        public RepositoryCollection(IServiceProvider services, IServerContextFactory serverFactory, ILogger<RepositoryCollection> logger)
        {
            _services = services;
            _serverFactory = serverFactory;
            _logger = logger;
        }

        public Task<IRepository> GetRepositoryAsync(CancellationToken cancel)
        {
            return GetRepositoryAsync(ServerContextOptions.DefaultServerName, cancel);
        }

        public Task<IRepository> GetRepositoryAsync(string name, CancellationToken cancel)
        {
            return GetRepositoryAsync(new RepositoryRequest { Name = name }, cancel);
        }

        public async Task<IRepository> GetRepositoryAsync(RepositoryRequest repositoryRequest, CancellationToken cancel)
        {
            var name = repositoryRequest.Name ?? ServerContextOptions.DefaultServerName;

            string GetCacheKey()
            {
                //UNDONE: finalize cache key. Cache key must contain all property values to be unique.
                return $"{name}-{repositoryRequest.AccessToken}";
            }

            var cacheKey = GetCacheKey();

            if (_repositories.TryGetValue(cacheKey, out var repo))
                return repo;

            await _asyncLock.WaitAsync(cancel);

            try
            {
                if (_repositories.TryGetValue(cacheKey, out repo))
                    return repo;

                _logger.LogTrace($"Building server context for repository {name}");

                // get the server context, create a repository instance and cache it
                var server = await _serverFactory.GetServerAsync(name, repositoryRequest.AccessToken).ConfigureAwait(false);
                if (server == null)
                    _logger.LogWarning($"Server context could not be constructed for repository {name}");

                repo = _services.GetRequiredService<IRepository>();
                repo.Server = server;

                _repositories[cacheKey] = repo;

                _logger.LogTrace($"Connected to repository {name} ({server?.Url}).");
            }
            finally
            {
                _asyncLock.Release();
            }

            return repo;
        }
    }
}
