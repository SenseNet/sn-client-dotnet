using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

internal class Repository : IRepository
{
    private readonly IRestCaller _restCaller;
    private readonly IServiceProvider _services;
    private readonly ILogger<Repository> _logger;

    public ServerContext Server { get; set; }
    
    public RegisteredContentTypes GlobalContentTypes { get; }

    public Repository(IRestCaller restCaller, IServiceProvider services, IOptions<RegisteredContentTypes> globalContentTypes, ILogger<Repository> logger)
    {
        _restCaller = restCaller;
        _services = services;
        GlobalContentTypes = globalContentTypes.Value;
        _logger = logger;
    }

    /* ============================================================================ CREATION */

    public Content CreateExistingContent(int id)
    {
        if (id <= 0)
            throw new ArgumentException($"Value cannot be less than or equal 0. (Parameter '{nameof(id)}')");

        return CreateExistingContent<Content>(id, null);
    }
    public Content CreateExistingContent(string path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (path.Length == 0)
            throw new ArgumentException($"Value cannot be empty. (Parameter '{nameof(path)}')");

        return CreateExistingContent<Content>(0, path);
    }
    public T CreateExistingContent<T>(int id) where T : Content
    {
        if (id <= 0)
            throw new ArgumentException($"Value cannot be less than or equal 0. (Parameter '{nameof(id)}')");

        return CreateExistingContent<T>(id, null);
    }
    public T CreateExistingContent<T>(string path) where T : Content
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (path.Length == 0)
            throw new ArgumentException($"Value cannot be empty. (Parameter '{nameof(path)}')");

        return CreateExistingContent<T>(0, path);
    }
    private T CreateExistingContent<T>(int id, string path) where T : Content
    {
        var content = (T) _services.GetRequiredService(typeof(T));
        content.Server = Server;
        content.Repository = this;
        if (id > 0)
            content.Id = id;
        else
            content.Path = path;
        dynamic c = content;
        c.Existing = true;
        return content;
    }

    /* ---------------------------------------------------------------------------- */

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

        var contentType = GetContentTypeByName(contentTypeName) ?? typeof(Content);

        var content = (Content) _services.GetRequiredService(contentType);
        return PrepareContent(content, parentPath, name, contentTypeName);
    }
    public T CreateContent<T>(string parentPath, string contentTypeName, string name) where T : Content
    {
        if (parentPath == null)
            throw new ArgumentNullException(nameof(parentPath));
        if (parentPath.Length == 0)
            throw new ArgumentException($"Value cannot be empty. (Parameter '{nameof(parentPath)}')");

        T content;
        try
        {
            content = _services.GetRequiredService<T>();
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationException("The content type is not registered: " + typeof(T).Name, ex);
        }

        return PrepareContent(content, parentPath, name, contentTypeName ?? GetContentTypeNameByType<T>());
    }

    public Content CreateContentFromJson(JObject jObject, Type contentType = null)
    {
        return CreateContentFromResponse(jObject, contentType);
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
    public T CreateContentByTemplate<T>(string parentPath, string contentTypeName, string name, string contentTemplate) where T : Content
    {
        dynamic content = CreateContent<T>(parentPath, contentTypeName, name);

        if (contentTemplate == null)
            throw new ArgumentNullException(nameof(contentTemplate));
        if (contentTemplate.Length == 0)
            throw new ArgumentException($"Value cannot be empty. (Parameter '{nameof(contentTemplate)}')");

        content.__ContentTemplate = contentTemplate;
        return content;
    }

    private T PrepareContent<T>(T content, string parentPath, string name, string contentTypeName) where T : Content
    {
        dynamic c = content;
        c.Server = Server;
        c.Repository = this;
        c.ParentPath = parentPath;
        c.Name = name;
        c.__ContentType = contentTypeName;
        c.Existing = false;
        return (T)c;
    }

    /* ============================================================================ LOAD CONTENT */

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

        var rs = await GetResponseStringAsync(oDataRequest, HttpMethod.Get, cancel)
            .ConfigureAwait(false);
        if (string.IsNullOrEmpty(rs))
            return null;

        var type = GetTypeFromJsonModel(rs);

        T content;
        try
        {
            content = type != null ? (T)_services.GetRequiredService(type) : _services.GetRequiredService<T>();
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationException("The content type is not registered: " + typeof(T).Name, ex);
        }

        content.Server = Server;
        content.Repository = this;

        content.InitializeFromResponse(JsonHelper.Deserialize(rs).d);

        return content;
    }

    /* ============================================================================ LOAD COLLECTION */

    public Task<IEnumerable<Content>> LoadCollectionAsync(LoadCollectionRequest requestData, CancellationToken cancel)
    {
        if (requestData.ContentQuery != null)
            requestData.ContentQuery = AddInFolderRestriction(requestData.ContentQuery, requestData.Path);
        return LoadCollectionAsync(requestData.ToODataRequest(Server), cancel);
    }
    public Task<IEnumerable<T>> LoadCollectionAsync<T>(LoadCollectionRequest requestData, CancellationToken cancel) where T :Content
    {
        if (requestData.ContentQuery != null)
            requestData.ContentQuery = AddInFolderRestriction(requestData.ContentQuery, requestData.Path);
        return LoadCollectionAsync<T>(requestData.ToODataRequest(Server), cancel);
    }

    public async Task<int> GetContentCountAsync(LoadCollectionRequest requestData, CancellationToken cancel)
    {
        var oDataRequest = requestData.ToODataRequest(Server);
        oDataRequest.IsCollectionRequest = true;
        oDataRequest.CountOnly = true;

        var response = await GetResponseStringAsync(oDataRequest, HttpMethod.Get, cancel).ConfigureAwait(false);
            
        if (int.TryParse(response, out var count))
            return count;

        throw new ClientException($"Invalid count response. Request: {oDataRequest.GetUri()}. Response: {response}");
    }

    private string AddInFolderRestriction(string contentQuery, string folderPath)
    {
        var clause = $"InFolder:'{folderPath}'";
        return MoveSettingsToTheEnd($"+{clause} +({contentQuery})").Trim();
    }
    private Task<IEnumerable<Content>> LoadCollectionAsync(ODataRequest requestData, CancellationToken cancel)
    {
        return LoadCollectionAsync<Content>(requestData, cancel);
    }
    private async Task<IEnumerable<T>> LoadCollectionAsync<T>(ODataRequest requestData, CancellationToken cancel) where T :Content
    {
        requestData.IsCollectionRequest = true;
        requestData.SiteUrl = ServerContext.GetUrl(Server); //UNDONE: Why set the requestData.SiteUrl?

        var response = await GetResponseStringAsync(requestData, HttpMethod.Get, cancel).ConfigureAwait(false);
        if (string.IsNullOrEmpty(response))
            return Array.Empty<T>();

        var items = JsonHelper.Deserialize(response).d.results as JArray;

        return items?.Select(CreateContentFromResponse<T>) ?? Array.Empty<T>();
    }

    /* ============================================================================ EXISTENCE */

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

    /* ============================================================================ QUERY */

    public Task<IEnumerable<Content>> QueryForAdminAsync(QueryContentRequest requestData, CancellationToken cancel)
    {
        return QueryForAdminAsync<Content>(requestData, cancel);
    }

    public Task<IEnumerable<T>> QueryForAdminAsync<T>(QueryContentRequest requestData, CancellationToken cancel) where T : Content
    {
        var oDataRequest = requestData.ToODataRequest(Server);
        oDataRequest.AutoFilters = FilterStatus.Disabled;
        oDataRequest.LifespanFilter = FilterStatus.Disabled;
        return LoadCollectionAsync<T>(oDataRequest, cancel);
    }

    public Task<IEnumerable<Content>> QueryAsync(QueryContentRequest requestData, CancellationToken cancel)
    {
        return QueryAsync<Content>(requestData, cancel);
    }

    public Task<IEnumerable<T>> QueryAsync<T>(QueryContentRequest requestData, CancellationToken cancel) where T : Content
    {
        return LoadCollectionAsync<T>(requestData.ToODataRequest(Server), cancel);
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

        var response = await GetResponseStringAsync(oDataRequest, HttpMethod.Get, cancel).ConfigureAwait(false);

        if (int.TryParse(response, out var count))
            return count;

        throw new ClientException($"Invalid count response. Request: {oDataRequest.GetUri()}. Response: {response}");
    }

    /* ============================================================================ DELETE */

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
            ActionName = "DeleteBatch",
            PostData = JsonHelper.GetJsonPostModel(new
            {
                permanent,
                paths = idsOrPaths
            })
        };

        await GetResponseStringAsync(oDataRequest, HttpMethod.Post, cancel)
            .ConfigureAwait(false);

        _logger?.LogTrace(idsOrPaths.Length == 1
            ? $"Content {idsOrPaths[0]} was deleted."
            : $"{idsOrPaths.Length} contents were deleted.");
    }

    /* ============================================================================ MIDDLE LEVEL API */

    public Task<T> GetResponseAsync<T>(ODataRequest requestData, HttpMethod method, CancellationToken cancel)
    {
        //UNDONE: not implemented: GetResponseAsync<T>(ODataRequest, HttpMethod, CancellationToken)
        throw new NotImplementedException();
    }

    public Task<dynamic> GetResponseJsonAsync(ODataRequest requestData, HttpMethod method, CancellationToken cancel)
    {
        //UNDONE: not implemented: GetResponseJsonAsync(ODataRequest, HttpMethod, CancellationToken)
        throw new NotImplementedException();
    }

    public Task<string> GetResponseStringAsync(ODataRequest requestData, HttpMethod method, CancellationToken cancel)
    {
        return GetResponseStringAsync(requestData.GetUri(), method, requestData.PostData,
            requestData.AdditionalRequestHeaders, cancel);
    }

    public Task<string> GetResponseStringAsync(Uri uri, HttpMethod method, string postData,
        Dictionary<string, IEnumerable<string>> additionalHeaders, CancellationToken cancel)
    {
        return _restCaller.GetResponseStringAsync(uri, method, postData, additionalHeaders, Server, cancel);
    }

    /* ============================================================================ LOW LEVEL API */

    public Task ProcessWebResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        HttpContent httpContent, Action<HttpResponseMessage> responseProcessor, CancellationToken cancel)
    {
        return _restCaller.ProcessWebResponseAsync(relativeUrl, method, additionalHeaders,
            httpContent, responseProcessor, Server, cancel);
    }

    public Task ProcessWebRequestResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor,
        Action<HttpResponseMessage> responseProcessor, CancellationToken cancel)
    {
        return _restCaller.ProcessWebRequestResponseAsync(relativeUrl, method, additionalHeaders,
            requestProcessor, responseProcessor, Server, cancel);
    }

    /* ============================================================================ TOOLS */

    internal string GetContentTypeNameByType<T>() => GetContentTypeNameByType(typeof(T));
    internal string GetContentTypeNameByType(Type contentType)
    {
        if (contentType == null)
            return null;

        var contentTypeName = Server.RegisteredContentTypes?.GetContentTypeNameByType(contentType);

        return contentTypeName ?? GlobalContentTypes.GetContentTypeNameByType(contentType);
    }

    /* ============================================================================ */

    private T CreateContentFromResponse<T>(dynamic jObject) where T : Content
    {
        //var content = _services.GetRequiredService<T>();
        var content = (T)CreateContentFromResponse(jObject);
        return content;
    }
    private Content CreateContentFromResponse(dynamic jObject, Type contentType = null)
    {
        Type type;
        if (contentType == null)
        {
            string contentTypeName = jObject.Type?.ToString();
            type = GetContentTypeByName(contentTypeName);
        }
        else
        {
            type = contentType;
        }

        var content = type != null
            ? (Content)_services.GetRequiredService(type)
            : _services.GetRequiredService<Content>();

        content.Server = Server;
        content.Repository = this;

        content.InitializeFromResponse(jObject);

        return content;
    }

    private Type GetTypeFromJsonModel(string rawJson)
    {
        var jsonModel = JsonHelper.Deserialize(rawJson).d;
        string contentTypeName = jsonModel.Type?.ToString();
        return GetContentTypeByName(contentTypeName);
    }
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

    /* ---------------------------------------------------------------------------- */

    private static string[] QuerySettingParts { get; } = {
        "SELECT", "SKIP", "TOP", "SORT", "REVERSESORT", "AUTOFILTERS", "LIFESPAN", "COUNTONLY", "QUICK", "ALLVERSIONS"
    };
    private static readonly string RegexKeywordsAndComments = "//|/\\*|(\\.(?<keyword>[A-Z]+)(([ ]*:[ ]*[#]?\\w+(\\.\\w+)?)|([\\) $\\r\\n]+)))";
    private static string MoveSettingsToTheEnd(string queryText)
    {
        if (string.IsNullOrEmpty(queryText))
            return queryText;

        var backParts = string.Empty;
        var index = 0;
        var regex = new Regex(RegexKeywordsAndComments, RegexOptions.Multiline);

        while (true)
        {
            if (index >= queryText.Length)
                break;

            // find the next setting keyword or comment start
            var match = regex.Match(queryText, index);
            if (!match.Success)
                break;

            // if it is not a keyword than it is a comment --> skip it
            if (!match.Value.StartsWith("."))
            {
                index = queryText.Length;
                continue;
            }

            // if we do not recognise the keyword, skip it (it may be in the middle of a text between quotation marks)
            if (!QuerySettingParts.Contains(match.Groups["keyword"].Value))
            {
                index = match.Index + match.Length;
                continue;
            }

            // remove the setting from the original position and store it
            // Patch match.value: it cannot ends with ') '.
            var matchValue = match.Value;
            if (matchValue.EndsWith(") "))
                matchValue = matchValue.Substring(0, matchValue.Length - 2);
            else if (matchValue.EndsWith(")"))
                matchValue = matchValue.Substring(0, matchValue.Length - 1);

            queryText = queryText.Remove(match.Index, matchValue.Length);
            index = match.Index;
            backParts += " " + matchValue + " ";
        }

        // add the stored settings to the end of the query
        return string.Concat(queryText, backParts);
    }
}