using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <inheritdoc />
    public class Repository : IRepository
    {
        private readonly IRestCaller _restCaller;
        private readonly IServiceProvider _services;
        private readonly ILogger<Repository> _logger;

        public RegisteredContentTypes GlobalContentTypes { get; }
        public ServerContext Server { get; set; }

        public Repository(IRestCaller restCaller, IServiceProvider services, IOptions<RegisteredContentTypes> globalContentTypes, ILogger<Repository> logger)
        {
            _restCaller = restCaller;
            _services = services;
            GlobalContentTypes = globalContentTypes.Value;
            _logger = logger;
        }

        public T CreateContent<T>() where T : Content
        {
            return _services.GetRequiredService<T>();
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

            var type = GetTypeFromJsonModel(rs);
            var content = type != null
                ? (T)_services.GetRequiredService(type)
                : _services.GetRequiredService<T>();

            content.Server = Server;
            content.Repository = this;

            content.InitializeFromResponse(JsonHelper.Deserialize(rs).d);

            return content;
        }

        private Type GetTypeFromJsonModel(string rawJson)
        {
            var jsonModel = JsonHelper.Deserialize(rawJson).d;
            string typeName = jsonModel.Type?.ToString();
            if (typeName == null)
                return null;
            if (Server.RegisteredContentTypes.ContentTypes.TryGetValue(typeName, out var contentType))
                return contentType;
            if (GlobalContentTypes.ContentTypes.TryGetValue(typeName, out var globalContentType))
                return globalContentType;
            return null;
        }
    }
}
