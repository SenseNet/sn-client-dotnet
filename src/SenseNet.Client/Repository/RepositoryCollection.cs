using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    internal class RepositoryCollection : IRepositoryCollection
    {
        private readonly ILogger<RepositoryCollection> _logger;
        private readonly IServiceProvider _services;
        private readonly IServerContextFactory _serverFactory;
        private readonly MemoryCache _repositories = new(new MemoryCacheOptions { SizeLimit = 1024 });
        private readonly SemaphoreSlim _asyncLock = new(1, 1);

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
            return GetRepositoryAsync(new RepositoryArgs { Name = name }, cancel);
        }

        public async Task<IRepository> GetRepositoryAsync(RepositoryArgs repositoryArgs, CancellationToken cancel)
        {
            var name = repositoryArgs.Name ?? ServerContextOptions.DefaultServerName;

            int GetCacheKey()
            {
                // Cache key must contain all property values to be unique.
                return $"{name}-{repositoryArgs.AccessToken}".GetHashCode();
            }

            var cacheKey = GetCacheKey();

            if (_repositories.TryGetValue<IRepository>(cacheKey, out var repo))
                return repo;

            await _asyncLock.WaitAsync(cancel);

            try
            {
                if (_repositories.TryGetValue(cacheKey, out repo))
                    return repo;

                _logger.LogTrace($"Building server context for repository {name}");

                // get the server context, create a repository instance and cache it
                var server = await _serverFactory.GetServerAsync(name, repositoryArgs.AccessToken).ConfigureAwait(false);
                if (server == null)
                    _logger.LogWarning($"Server context could not be constructed for repository {name}");

                repo = _services.GetRequiredService<IRepository>();
                repo.Server = server;

                _repositories.Set(cacheKey, repo, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.AddHours(1)),
                    Size = 1
                });

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
