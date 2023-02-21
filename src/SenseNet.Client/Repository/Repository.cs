using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <inheritdoc />
    public class Repository : IRepository
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<Repository> _logger;

        public ServerContext Server { get; set; }

        public Repository(IServiceProvider services, ILogger<Repository> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task<Content> LoadContentAsync(int id, CancellationToken cancel)
        {
            return LoadContentAsync(new ODataRequest(Server)
            {
                ContentId = id
            }, cancel);
        }
        public Task<Content> LoadContentAsync(string path, CancellationToken cancel)
        {
            return LoadContentAsync(new ODataRequest(Server)
            {
                Path = path
            }, cancel);
        }
        public Task<Content> LoadContentAsync(ODataRequest requestData, CancellationToken cancel)
        {
            return LoadContentAsync<Content>(requestData, cancel);
        }

        public Task<T> LoadContentAsync<T>(int id, CancellationToken cancel) where T : Content
        {
            return LoadContentAsync<T>(new ODataRequest(Server)
            {
                ContentId = id
            }, cancel);
        }
        public Task<T> LoadContentAsync<T>(string path, CancellationToken cancel) where T : Content
        {
            return LoadContentAsync<T>(new ODataRequest(Server)
            {
                Path = path
            }, cancel);
        }
        public async Task<T> LoadContentAsync<T>(ODataRequest requestData, CancellationToken cancel) where T : Content
        {
            // just to make sure
            requestData.IsCollectionRequest = false;

            //TODO: error handling
            var rs = await RESTCaller.GetResponseStringAsync(requestData.GetUri(), Server).ConfigureAwait(false);
            if (string.IsNullOrEmpty(rs))
                return null;

            var content = _services.GetRequiredService<T>();
            content.Server = Server;
            content.Repository = this;

            content.InitializeFromResponse(JsonHelper.Deserialize(rs).d);

            return content;
        }
    }
}
