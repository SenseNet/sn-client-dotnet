using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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
            if (parentPath == null)
                throw new ArgumentNullException(nameof(parentPath));
            if (parentPath.Length == 0)
                throw new ArgumentException($"Value cannot be empty. (Parameter '{nameof(parentPath)}')");
            if (contentTypeName == null)
                throw new ArgumentNullException(nameof(contentTypeName));
            if (contentTypeName.Length == 0)
                throw new ArgumentException($"Value cannot be empty. (Parameter '{nameof(contentTypeName)}')");
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException($"Value cannot be empty. (Parameter '{nameof(name)}')");

            dynamic content = PrepareContent(_services.GetRequiredService<Content>());
            content.ParentPath = parentPath;
            content.Name = name;
            content.__ContentType = contentTypeName;
            content.Existing = false;
            return content;
        }

        public Content CreateContentByTemplate(string parentPath, string contentTypeName, string name, string contentTemplate)
        {
            dynamic content = CreateContent(parentPath, contentTypeName, name);

            if (contentTemplate == null)
                throw new ArgumentNullException(nameof(contentTemplate));
            if (contentTemplate.Length == 0)
                throw new ArgumentException($"Value cannot be empty. (Parameter '{nameof(contentTemplate)}')");

            content.__ContentTemplate = contentTemplate;
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

        public async Task<bool> IsContentExistAsync(string path, CancellationToken cancel)
        {
            var requestData = new ODataRequest(Server)
            {
                Path = path,
                Metadata = MetadataFormat.None,
                Select = new[] { "Id" }
            };

            var content = await LoadContentAsync(requestData, cancel).ConfigureAwait(false);
            return content != null;
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

            var content = CreateContentFromResponse<T>(JsonHelper.Deserialize(rs).d);

            return content;
        }

        public async Task<IEnumerable<Content>> LoadCollectionAsync(ODataRequest requestData, CancellationToken cancel)
        {
            // ---- return await RESTCaller.GetCollectionAsync(requestData, Server).ConfigureAwait(false);
            // just to make sure
            requestData.IsCollectionRequest = true;
            requestData.SiteUrl = ServerContext.GetUrl(Server);

            var response = await _restCaller.GetResponseStringAsync(requestData.GetUri(), Server).ConfigureAwait(false);
            var items = JsonHelper.Deserialize(response).d.results as JArray;

            return items?.Select(CreateContentFromResponse<Content>) ?? new Content[0];
        }

        public async Task<int> GetContentCountAsync(ODataRequest requestData, CancellationToken cancel)
        {
            // just to make sure
            requestData.IsCollectionRequest = true;
            requestData.CountOnly = true;

            var response = await _restCaller.GetResponseStringAsync(requestData.GetUri(), Server).ConfigureAwait(false);
            
            if (int.TryParse(response, out var count))
                return count;

            throw new ClientException($"Invalid count response. Request: {requestData.GetUri()}. Response: {response}");
        }

        public Task<IEnumerable<Content>> QueryForAdminAsync(string queryText, CancellationToken cancel,
            string[] select = null, string[] expand = null, QuerySettings settings = null)
        {
            settings ??= new QuerySettings();
            settings.EnableAutofilters = FilterStatus.Disabled;
            settings.EnableLifespanFilter = FilterStatus.Disabled;
            return QueryAsync(queryText, cancel, select, expand, settings);
        }

        public Task<IEnumerable<Content>> QueryAsync(string queryText, CancellationToken cancel,
            string[] select = null, string[] expand = null, QuerySettings settings = null)
        {
            settings ??= QuerySettings.Default;

            var oDataRequest = new ODataRequest(Server)
            {
                Path = "/Root",
                Select = select,
                Expand = expand,
                Top = settings.Top,
                Skip = settings.Skip,
                AutoFilters = settings.EnableAutofilters,
                LifespanFilter = settings.EnableLifespanFilter,
                ContentQuery = queryText
            };

            return LoadCollectionAsync(oDataRequest, cancel);
        }

        private T CreateContentFromResponse<T>(dynamic jObject) where T : Content
        {
            var content = _services.GetRequiredService<T>();

            content.Server = Server;
            content.Repository = this;

            content.InitializeFromResponse(jObject);

            return content;
        }
    }
}
