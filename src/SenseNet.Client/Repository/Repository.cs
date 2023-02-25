using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <inheritdoc />
    internal class Repository : IRepository
    {
        private readonly IRestCaller _restCaller;
        private readonly IServiceProvider _services;
        private readonly ILogger<Repository> _logger;

        public ServerContext Server { get; set; }

        public Repository(IRestCaller restCaller, IServiceProvider services, ILogger<Repository> logger)
        {
            _restCaller = restCaller;
            _services = services;
            _logger = logger;
        }

        public Content CreateContent(string parentPath, string contentTypeName, string name)
        {
            dynamic content = PrepareContent(_services.GetRequiredService<Content>());
            content.ParentPath = parentPath;
            content.Name = name;
            content.__ContentType = contentTypeName;
            return content;
        }

        private T PrepareContent<T>(T content) where T : Content
        {
            content.Server = Server;
            content.Repository = this;
            return content;
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
            var rs = await _restCaller.GetResponseStringAsync(requestData.GetUri(), Server).ConfigureAwait(false);
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
