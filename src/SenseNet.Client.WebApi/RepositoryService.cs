using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SenseNet.Client.WebApi
{
    public class RepositoryService : IRepositoryService
    {
        private readonly IRepositoryCollection _repositoryCollection;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RepositoryService> _logger;

        public RepositoryService(IRepositoryCollection repositoryCollection, IHttpContextAccessor httpContextAccessor,
            ILogger<RepositoryService> logger)
        {
            _repositoryCollection = repositoryCollection;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<IRepository> GetUserRepositoryAsync(CancellationToken cancel)
        {
            var token = _httpContextAccessor.HttpContext?.GetBearerToken();
            if (token != null)
                return await GetRepositoryAsync(Repositories.UserRepository, token, cancel).ConfigureAwait(false);

            _logger.LogTrace("Returning Visitor repository instead of User, because there is no token available.");

            return await GetVisitorRepositoryAsync(cancel).ConfigureAwait(false);
        }

        public Task<IRepository> GetVisitorRepositoryAsync(CancellationToken cancel) =>
            GetRepositoryAsync(Repositories.VisitorRepository, null, cancel);
        public Task<IRepository> GetAdminRepositoryAsync(CancellationToken cancel) =>
            GetRepositoryAsync(Repositories.AdminRepository, null, cancel);

        private Task<IRepository> GetRepositoryAsync(string name, string? token, CancellationToken cancel) =>
            _repositoryCollection.GetRepositoryAsync(new RepositoryArgs
            {
                Name = name,
                AccessToken = token
            }, cancel);
    }
}
