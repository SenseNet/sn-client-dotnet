﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using SenseNet.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <inheritdoc />
    internal class Repository : IRepository
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

//UNDONE: rewrite CreateContent<T>
public T CreateContent<T>(string parentPath, string name) where T : Content
{
    try
    {
        return PrepareContent(_services.GetRequiredService<T>());
    }
    catch (InvalidOperationException ex)
    {
        throw new ApplicationException("The content type is not registered: " + typeof(T).Name, ex);
    }
}
        
        //UNDONE: rewrite CreateContent
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

            if (name == string.Empty)
                name = null;

//public Content CreateContent(string contentTypeName)
//{
//    var contentType = GetContentTypeByName(contentTypeName);
//    if (contentType == null)
//        throw new ApplicationException("The content type is not registered: " + contentTypeName);
//    return PrepareContent((Content)_services.GetRequiredService(contentType));
//}
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
            return LoadContentAsync(new LoadContentRequest {ContentId = id}, cancel);
        }
        public Task<Content> LoadContentAsync(string path, CancellationToken cancel)
        {
            return LoadContentAsync(new LoadContentRequest {Path = path}, cancel);
        }
        public Task<Content> LoadContentAsync(LoadContentRequest requestData, CancellationToken cancel)
        {
            return LoadContentAsync<Content>(requestData, cancel);
        }

        public async Task<bool> IsContentExistsAsync(string path, CancellationToken cancel)
        {
            var requestData = new LoadContentRequest
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
            return LoadContentAsync<T>(new LoadContentRequest {ContentId = id}, cancel);
        }
        public Task<T> LoadContentAsync<T>(string path, CancellationToken cancel) where T : Content
        {
            return LoadContentAsync<T>(new LoadContentRequest {Path = path}, cancel);
        }
        public async Task<T> LoadContentAsync<T>(LoadContentRequest requestData, CancellationToken cancel) where T : Content
        {
            var oDataRequest = requestData.ToODataRequest(Server);
            oDataRequest.IsCollectionRequest = false;

            //TODO: error handling
            var rs = await _restCaller.GetResponseStringAsync(oDataRequest.GetUri(), Server, cancel).ConfigureAwait(false);
            if (string.IsNullOrEmpty(rs))
                return null;

            var content = CreateContentFromResponse<T>(JsonHelper.Deserialize(rs).d);
            return content;
        }
//UNDONE: merge methods
/*
public async Task<T> LoadContentAsync<T>(LoadContentRequest requestData, CancellationToken cancel) where T : Content
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
*/

        public Task<IEnumerable<Content>> LoadCollectionAsync(LoadCollectionRequest requestData, CancellationToken cancel)
        {
            if (requestData.ContentQuery != null)
                requestData.ContentQuery = AddInFolderRestriction(requestData.ContentQuery, requestData.Path);
            return LoadCollectionAsync(requestData.ToODataRequest(Server), cancel);
        }
        private string AddInFolderRestriction(string contentQuery, string folderPath)
        {
            //UNDONE: AddInFolderRestriction throws a server error if the content query has top level instructions e.g. .SORT
            return $"+InFolder:'{folderPath}' +({contentQuery})";
        }
        private async Task<IEnumerable<Content>> LoadCollectionAsync(ODataRequest requestData, CancellationToken cancel)
        {
            requestData.IsCollectionRequest = true;
            requestData.SiteUrl = ServerContext.GetUrl(Server);

            var response = await _restCaller.GetResponseStringAsync(requestData.GetUri(), Server, cancel).ConfigureAwait(false);
            var items = JsonHelper.Deserialize(response).d.results as JArray;

            return items?.Select(CreateContentFromResponse<Content>) ?? Array.Empty<Content>();
        }

        public async Task<int> GetContentCountAsync(LoadCollectionRequest requestData, CancellationToken cancel)
        {
            var oDataRequest = requestData.ToODataRequest(Server);
            oDataRequest.IsCollectionRequest = true;
            oDataRequest.CountOnly = true;

            var response = await _restCaller.GetResponseStringAsync(oDataRequest.GetUri(), Server, cancel).ConfigureAwait(false);
            
            if (int.TryParse(response, out var count))
                return count;

            throw new ClientException($"Invalid count response. Request: {oDataRequest.GetUri()}. Response: {response}");
        }

        public Task<IEnumerable<Content>> QueryForAdminAsync(QueryContentRequest requestData, CancellationToken cancel)
        {
            var oDataRequest = requestData.ToODataRequest(Server);
            oDataRequest.AutoFilters = FilterStatus.Disabled;
            oDataRequest.LifespanFilter = FilterStatus.Disabled;
            return LoadCollectionAsync(oDataRequest, cancel);
        }
        public Task<IEnumerable<Content>> QueryAsync(QueryContentRequest requestData, CancellationToken cancel)
        {
            return LoadCollectionAsync(requestData.ToODataRequest(Server), cancel);
        }

        public Task<int> QueryCountForAdminAsync(QueryContentRequest requestData, CancellationToken cancel)
        {
            requestData.AutoFilters = FilterStatus.Disabled;
            requestData.LifespanFilter = FilterStatus.Disabled;

            return QueryCountAsync(requestData, cancel);
        }
        public async Task<int> QueryCountAsync(QueryContentRequest requestData, CancellationToken cancel)
        {
            var oDataRequest = requestData.ToODataRequest(Server);
            oDataRequest.CountOnly = true;

            var response = await _restCaller.GetResponseStringAsync(oDataRequest.GetUri(), Server, cancel).ConfigureAwait(false);

            if (int.TryParse(response, out var count))
                return count;

            throw new ClientException($"Invalid count response. Request: {oDataRequest.GetUri()}. Response: {response}");
        }

        public Task DeleteContentAsync(string path, bool permanent, CancellationToken cancel)
        {
            return DeleteContentAsync(new object[] { path }, permanent, cancel);
        }
        public Task DeleteContentAsync(string[] paths, bool permanent, CancellationToken cancel)
        {
            return DeleteContentAsync(paths.Cast<object>().ToArray(), permanent, cancel);
        }
        public Task DeleteContentAsync(int id, bool permanent, CancellationToken cancel)
        {
            return DeleteContentAsync(new object[] { id }, permanent, cancel);
        }
        public Task DeleteContentAsync(int[] ids, bool permanent, CancellationToken cancel)
        {
            return DeleteContentAsync(ids.Cast<object>().ToArray(), permanent, cancel);
        }
        public async Task DeleteContentAsync(object[] idsOrPaths, bool permanent, CancellationToken cancel)
        {
            if (idsOrPaths == null)
                throw new ArgumentNullException(nameof(idsOrPaths));

            var oDataRequest = new ODataRequest(Server)
            {
                Path = "/Root",
                ActionName = "DeleteBatch"
            };

            await _restCaller.GetResponseStringAsync(oDataRequest.GetUri(), Server, cancel, HttpMethod.Post,
                    JsonHelper.GetJsonPostModel(new
                    {
                        permanent,
                        paths = idsOrPaths
                    }))
                .ConfigureAwait(false);

            _logger?.LogTrace(idsOrPaths.Length == 1
                ? $"Content {idsOrPaths[0]} was deleted."
                : $"{idsOrPaths.Length} contents were deleted.");
        }

        private T CreateContentFromResponse<T>(dynamic jObject) where T : Content
        {
            var content = _services.GetRequiredService<T>();

            content.Server = Server;
            content.Repository = this;

            content.InitializeFromResponse(jObject);

            return content;
        }


//UNDONE: integrate GetTypeFromJsonModel
private Type GetTypeFromJsonModel(string rawJson)
{
    var jsonModel = JsonHelper.Deserialize(rawJson).d;
    string contentTypeName = jsonModel.Type?.ToString();
    return GetContentTypeByName(contentTypeName);
}
//UNDONE: integrate GetContentTypeByName
private Type GetContentTypeByName(string contentTypeName)
{
    if (contentTypeName == null)
        return null;
    if (Server.RegisteredContentTypes != null)
        if (Server.RegisteredContentTypes.ContentTypes.TryGetValue(contentTypeName, out var contentType))
            return contentType;
    if (GlobalContentTypes.ContentTypes.TryGetValue(contentTypeName, out var globalContentType))
        return globalContentType;
    return null;
}
    }
}
