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
    internal class RepositoryService : IRepositoryService
    {
        private readonly ILogger<RepositoryService> _logger;
        private readonly IServiceProvider _services;
        private readonly IServerContextFactory _serverFactory;
        private readonly IDictionary<string, IRepository> _repositories = new ConcurrentDictionary<string, IRepository>();
        private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

        public RepositoryService(IServiceProvider services, IServerContextFactory serverFactory, ILogger<RepositoryService> logger)
        {
            _services = services;
            _serverFactory = serverFactory;
            _logger = logger;
        }

        public Task<IRepository> GetRepositoryAsync(CancellationToken cancel)
        {
            return GetRepositoryAsync(ServerContextOptions.DefaultServerName, cancel);
        }

        public async Task<IRepository> GetRepositoryAsync(string name, CancellationToken cancel)
        {
            //TODO: authenticate using the config
            // auth: what about on-the-fly authentication? How to connect to the same repo with multiple different users?

            name ??= ServerContextOptions.DefaultServerName;

            if (_repositories.TryGetValue(name, out var repo))
                return repo;

            await _asyncLock.WaitAsync(cancel);

            try
            {
                if (_repositories.TryGetValue(name, out repo))
                    return repo;

                _logger.LogTrace($"Building server context for repository {name}");

                // get the server context, create a repository instance and cache it
                var server = await _serverFactory.GetServerAsync(name).ConfigureAwait(false);
                if (server == null)
                    _logger.LogWarning($"Server context could not be constructed for repository {name}");

                repo = _services.GetRequiredService<IRepository>();
                repo.Server = server;

                _repositories[name] = repo;

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
