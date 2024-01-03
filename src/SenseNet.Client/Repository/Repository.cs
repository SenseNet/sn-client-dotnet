using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

internal class Repository : IRepository
{
    private static readonly string JsonContentMimeType = "application/json";

    private readonly IRestCaller _restCaller;
    private readonly IServiceProvider _services;
    private readonly ILogger<Repository> _logger;

    private ServerContext _server;

    public ServerContext Server
    {
        get => _server;
        set
        {
            _server = value;
            _restCaller.Server = value;
        }
    }

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

    public Task<IContentCollection<Content>> LoadCollectionAsync(LoadCollectionRequest requestData, CancellationToken cancel)
    {
        if (requestData.ContentQuery != null)
            requestData.ContentQuery = AddInFolderRestriction(requestData.ContentQuery, requestData.Path);
        return LoadCollectionAsync(requestData.ToODataRequest(Server), cancel);
    }
    public Task<IContentCollection<T>> LoadCollectionAsync<T>(LoadCollectionRequest requestData, CancellationToken cancel) where T :Content
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
    private Task<IContentCollection<Content>> LoadCollectionAsync(ODataRequest requestData, CancellationToken cancel)
    {
        return LoadCollectionAsync<Content>(requestData, cancel);
    }
    private async Task<IContentCollection<T>> LoadCollectionAsync<T>(ODataRequest requestData, CancellationToken cancel) where T :Content
    {
        requestData.IsCollectionRequest = true;
        var response = await GetResponseStringAsync(requestData, HttpMethod.Get, cancel).ConfigureAwait(false);
        if (string.IsNullOrEmpty(response))
            return ContentCollection<T>.Empty;

        var jsonResponse = JsonHelper.Deserialize(response);
        var totalCount = Convert.ToInt32(jsonResponse.d.__count ?? 0);
        var items = jsonResponse.d.results as JArray;
        var count = items?.Count ?? 0;
        var resultEnumerable = items?.Select(CreateContentFromResponse<T>).ToArray() ?? Array.Empty<T>();
        return new ContentCollection<T>(resultEnumerable, count,
            totalCount);
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

    public Task<IContentCollection<Content>> QueryForAdminAsync(QueryContentRequest requestData, CancellationToken cancel)
    {
        return QueryForAdminAsync<Content>(requestData, cancel);
    }

    public Task<IContentCollection<T>> QueryForAdminAsync<T>(QueryContentRequest requestData, CancellationToken cancel) where T : Content
    {
        var oDataRequest = requestData.ToODataRequest(Server);
        oDataRequest.AutoFilters = FilterStatus.Disabled;
        oDataRequest.LifespanFilter = FilterStatus.Disabled;
        return LoadCollectionAsync<T>(oDataRequest, cancel);
    }

    public Task<IContentCollection<Content>> QueryAsync(QueryContentRequest requestData, CancellationToken cancel)
    {
        return QueryAsync<Content>(requestData, cancel);
    }

    public Task<IContentCollection<T>> QueryAsync<T>(QueryContentRequest requestData, CancellationToken cancel) where T : Content
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

    /* ============================================================================ UPLOAD */

    public Task<UploadResult> UploadAsync(UploadRequest request, Stream stream, CancellationToken cancel)
    {
        return UploadAsync(request, stream, null, cancel);
    }
    public async Task<UploadResult> UploadAsync(UploadRequest request, Stream stream, Action<int> progressCallback,
        CancellationToken cancel)
    {
        var uploadData = new UploadData()
        {
            FileName = request.FileName ?? request.ContentName,
            FileLength = stream.Length
        };

        if (!string.IsNullOrEmpty(request.ContentType))
            uploadData.ContentType = request.ContentType;
        if (!string.IsNullOrEmpty(request.PropertyName))
            uploadData.PropertyName = request.PropertyName;

        var oDataRequest = request.ToODataRequest(Server);
        return await UploadStreamAsync(oDataRequest, uploadData, stream, progressCallback, cancel)
            .ConfigureAwait(false);
    }
    private async Task<UploadResult> UploadStreamAsync(ODataRequest request, UploadData uploadData, Stream stream,
        Action<int> progressCallback, CancellationToken cancel)
    {
        // force set values
        uploadData.UseChunk = stream.Length > ClientContext.Current.ChunkSizeInBytes;
        if (uploadData.FileLength == 0)
            uploadData.FileLength = stream.Length;

        request.Parameters.Add("create", "1");

        UploadResult result = null;

        // Get ChunkToken
        try
        {
            _logger.LogTrace("###>REQ: {0}", request);
            _logger.LogTrace($"Uploading initial data of {uploadData.FileName}.");

            var httpContent = new StringContent(uploadData.ToString());
            httpContent.Headers.ContentType = new MediaTypeHeaderValue(JsonContentMimeType);
            await ProcessWebResponseAsync(request.ToString(), HttpMethod.Post, request.AdditionalRequestHeaders,
                httpContent,
                async (response, _) =>
                {
                    uploadData.ChunkToken = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }, cancel).ConfigureAwait(false);

        }
        catch (WebException ex)
        {
            var ce = new ClientException("Error during binary upload.", ex)
            {
                Data =
                    {
                        ["SiteUrl"] = request.SiteUrl,
                        ["Parent"] = request.ContentId != 0 ? request.ContentId.ToString() : request.Path,
                        ["FileName"] = uploadData.FileName,
                        ["ContentType"] = uploadData.ContentType
                    }
            };

            throw ce;
        }

        // Reuse previous request data, but remove unnecessary parameters
        request.Parameters.Remove("create");

        // Send subsequent requests
        var boundary = "---------------------------" + DateTime.UtcNow.Ticks.ToString("x");
        var uploadFormData = uploadData.ToKeyValuePairs();
        var contentDispositionHeaderValue = new ContentDispositionHeaderValue("attachment")
        {
            FileName = uploadData.FileName
        };
        var buffer = new byte[ClientContext.Current.ChunkSizeInBytes];
        int bytesRead;
        var start = 0;
        var chunkCount = 0;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancel)) != 0)
        {
            chunkCount++;

            _logger.LogTrace($"Uploading chunk {chunkCount}: {bytesRead} bytes of {uploadData.FileName}.");

            // Prepare the current chunk request
            using var httpContent = new MultipartFormDataContent(boundary);
            foreach (var item in uploadFormData)
                httpContent.Add(new StringContent(item.Value), item.Key);
            httpContent.Headers.ContentDisposition = contentDispositionHeaderValue;

            if (uploadData.UseChunk)
                httpContent.Headers.ContentRange =
                    new ContentRangeHeaderValue(start, start + bytesRead - 1, stream.Length);

            // Add the chunk as a stream into the request content
            var postedStream = new MemoryStream(buffer, 0, bytesRead);
            httpContent.Add(new StreamContent(postedStream), "files[]", uploadData.FileName);

            // Process
            await ProcessWebResponseAsync(request.ToString(), HttpMethod.Post, request.AdditionalRequestHeaders,
                httpContent,
                async (response, _) =>
                {
                    var rs = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    result = JsonHelper.Deserialize<UploadResult>(rs);
                }, cancel).ConfigureAwait(false);

            start += bytesRead;

            // Notify the caller about every chunk that was uploaded successfully
            progressCallback?.Invoke(start);
        }

        return result;
    }

    public async Task<UploadResult> UploadAsync(UploadRequest request, string fileText, CancellationToken cancel)
    {
        var uploadData = new UploadData()
        {
            FileName = request.FileName ?? request.ContentName,
        };

        if (!string.IsNullOrEmpty(request.ContentType))
            uploadData.ContentType = request.ContentType;

        if (!string.IsNullOrEmpty(request.PropertyName))
            uploadData.PropertyName = request.PropertyName;

        //---- return await RESTCaller.UploadTextAsync(fileText, uploadData, parentId, cancellationToken, server).ConfigureAwait(false);
        var oDataRequest = request.ToODataRequest(Server);
        return await UploadTextAsync(oDataRequest, uploadData, fileText, cancel).ConfigureAwait(false);
    }
    private async Task<UploadResult> UploadTextAsync(ODataRequest request, UploadData uploadData, string text, CancellationToken cancel)
    {
        // force set values
        if (Encoding.UTF8.GetBytes(text).Length > ClientContext.Current.ChunkSizeInBytes)
            throw new InvalidOperationException("Cannot upload a text bigger than the chunk size " +
                                                $"({ClientContext.Current.ChunkSizeInBytes} bytes). " +
                                                "This method uploads whole text files. Use the regular UploadAsync method " +
                                                "for uploading big files in chunks.");
        if (uploadData.FileLength == 0)
            uploadData.FileLength = text.Length;
        uploadData.FileText = text;

        UploadResult result = null;

        var model = JsonHelper.GetJsonPostModel(uploadData.ToDictionary());
        var httpContent = new StringContent(model);
        httpContent.Headers.ContentType = new MediaTypeHeaderValue(JsonContentMimeType);

        Server.Logger?.LogTrace($"Uploading text content to the {uploadData.PropertyName} field of {uploadData.FileName}");

        await ProcessWebResponseAsync(request.ToString(), HttpMethod.Post, request.AdditionalRequestHeaders, httpContent,
            async (response, _) =>
            {
                if (response != null)
                {
                    var rs = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    result = JsonHelper.Deserialize<UploadResult>(rs);
                }
            }, cancel).ConfigureAwait(false);

        return result;
    }

    /* ============================================================================ DOWNLOAD */

    public Task<string> GetBlobToken(int id, CancellationToken cancel, string version = null,
        string propertyName = null) => throw new NotImplementedException();

    public Task<string> GetBlobToken(string path, CancellationToken cancel, string version = null,
        string propertyName = null) => throw new NotImplementedException();

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
            PostData = new
            {
                permanent,
                paths = idsOrPaths
            }
        };

        await GetResponseStringAsync(oDataRequest, HttpMethod.Post, cancel)
            .ConfigureAwait(false);

        _logger?.LogTrace(idsOrPaths.Length == 1
            ? $"Content {idsOrPaths[0]} was deleted."
            : $"{idsOrPaths.Length} contents were deleted.");
    }

    /* ============================================================================ AUTHENTICATION */

    public Task<Content> GetCurrentUserAsync(CancellationToken cancel)
    {
        return GetCurrentUserAsync(null, null, cancel);
    }
    public async Task<Content> GetCurrentUserAsync(string[] select, string[] expand, CancellationToken cancel)
    {
        var accessToken = Server?.Authentication?.AccessToken;

        if (!string.IsNullOrEmpty(accessToken))
        {
            // The token contains the user id in the SUB claim.
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(accessToken);

                if (int.TryParse(jwtSecurityToken.Subject, out var contentId))
                {
                    // Userid found: simply load the user. This will make this method work
                    // even with older portals where the action below does not exist yet.
                    var user = await LoadContentAsync(new LoadContentRequest
                    {
                        ContentId = contentId,
                        Select = select,
                        Expand = expand
                    }, cancel).ConfigureAwait(false);
                    
                    if (user != null)
                        return user;

                    // the user is not found, we will load the visitor later
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error during JWT access token conversion.");
            }
        }
        
        // if there is a chance that the user is authenticated (token or key is present)
        if (!string.IsNullOrEmpty(accessToken) || !string.IsNullOrEmpty(Server?.Authentication?.ApiKey))
        {
            // we could not extract the user from the token: use the new action as a fallback
            var request = new ODataRequest(Server)
            {
                Path = "/Root",
                ActionName = "GetCurrentUser",
                Select = select,
                Expand = expand
            };

            var response = await GetResponseJsonAsync(request, HttpMethod.Get, cancel).ConfigureAwait(false);
            return CreateContentFromJson(response);
        }

        // no token: load Visitor
        return await LoadContentAsync(new LoadContentRequest
        {
            Path = Constants.User.VisitorPath,
            Select = select,
            Expand = expand
        }, cancel).ConfigureAwait(false);
    }

    /* ============================================================================ MIDDLE LEVEL API */

    public async Task<T> GetResponseAsync<T>(ODataRequest requestData, HttpMethod method, CancellationToken cancel)
    {
        object jsonResult = await GetResponseJsonAsync(requestData, method, cancel);
        if (jsonResult == null)
            return default;
        if (jsonResult is JObject jObject)
            return jObject.ToObject<T>();

        var result = Convert.ChangeType(jsonResult, typeof(T));
        return (T)result;
    }

    public async Task<dynamic> GetResponseJsonAsync(ODataRequest requestData, HttpMethod method, CancellationToken cancel)
    {
        var stringResult = await GetResponseStringAsync(requestData, method, cancel).ConfigureAwait(false);
        return stringResult == null ? null : JsonHelper.Deserialize(stringResult);
    }

    public Task<string> GetResponseStringAsync(ODataRequest requestData, HttpMethod method, CancellationToken cancel)
    {
        return GetResponseStringAsync(requestData.GetUri(), method,
            JsonHelper.GetJsonPostModel(requestData.PostData),
            requestData.AdditionalRequestHeaders, cancel);
    }

    public Task<string> GetResponseStringAsync(Uri uri, HttpMethod method, string postData,
        Dictionary<string, IEnumerable<string>> additionalHeaders, CancellationToken cancel)
    {
        return _restCaller.GetResponseStringAsync(uri, method, postData, additionalHeaders, cancel);
    }

    public async Task DownloadAsync(DownloadRequest request, Func<Stream, StreamProperties, Task> responseProcessor, CancellationToken cancel)
    {
        var url = request.MediaSrc;
        if (url == null)
        {
            var contentId = request.ContentId;
            if (contentId == 0)
            {
                if (string.IsNullOrEmpty(request.Path))
                    throw new InvalidOperationException("Invalid request properties: ContentId, Path, or MediaUrl must be specified.");
                var content = await LoadContentAsync(
                        new LoadContentRequest {Path = request.Path, Select = new[] {"Id"}}, cancel)
                    .ConfigureAwait(false);
                if (content == null)
                    throw new InvalidOperationException("Content not found.");
                contentId = content.Id;
            }

            url = $"/binaryhandler.ashx?nodeid={contentId}&propertyname={request.PropertyName ?? "Binary"}";
            if (!string.IsNullOrEmpty(request.Version))
                url += "&version=" + request.Version;
        }

        await ProcessWebResponseAsync(url, HttpMethod.Get, request.AdditionalRequestHeaders, null,
                async (response, c) =>
                {
                    if (response == null)
                        return;
                    var headers = response.Content.Headers;
                    var properties = new StreamProperties
                    {
                        MediaType = headers.ContentType?.MediaType,
                        FileName = headers.ContentDisposition?.FileName,
                        ContentLength = headers.ContentLength,
                    };
#if NET6_0_OR_GREATER
                    await responseProcessor(await response.Content.ReadAsStreamAsync(c).ConfigureAwait(false), properties);
#else
                    await responseProcessor(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), properties);
#endif
                }, cancel)
            .ConfigureAwait(false);
    }

    public async Task<T> InvokeFunctionAsync<T>(OperationRequest request, CancellationToken cancel)
    {
        var result = default(T);
        await ProcessOperationResponseAsync(request, HttpMethod.Get, response =>
        {
            if (!string.IsNullOrEmpty(response))
                result = JsonConvert.DeserializeObject<T>(response);
        }, cancel);
        return result;
    }

    public async Task InvokeActionAsync(OperationRequest request, CancellationToken cancel)
    {
        await InvokeActionAsync<object>(request, cancel);
    }

    public async Task<T> InvokeActionAsync<T>(OperationRequest request, CancellationToken cancel)
    {
        var result = default(T);
        await ProcessOperationResponseAsync(request, HttpMethod.Post, response =>
        {
            if (!string.IsNullOrEmpty(response))
                result = JsonConvert.DeserializeObject<T>(response);
        }, cancel);
        return result;
    }

    /* ============================================================================ LOW LEVEL API */

    public async Task ProcessOperationResponseAsync(OperationRequest request, HttpMethod method,
        Action<string> responseProcessor, CancellationToken cancel)
    {
        var uri = request.ToODataRequest(Server).GetUri();
        string responseAsString = null;
        await _restCaller.ProcessWebRequestResponseAsync(uri.ToString(), method,
            additionalHeaders: request.AdditionalRequestHeaders,
            requestProcessor: (handler, client, httpRequest) =>
            {
                if (request.PostData == null)
                    return;
                var json = JsonConvert.SerializeObject(request.PostData);
                httpRequest.Content = new StringContent(json);
            },
            responseProcessor: async (response, cancellation) =>
            {
                responseAsString = await response.Content.ReadAsStringAsync();
            },
            cancel);

        responseProcessor(responseAsString);
    }

    public Task ProcessWebResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        HttpContent httpContent, Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
    {
        return _restCaller.ProcessWebResponseAsync(relativeUrl, method, additionalHeaders,
            httpContent, responseProcessor, cancel);
    }

    public Task ProcessWebRequestResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor,
        Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
    {
        return _restCaller.ProcessWebRequestResponseAsync(relativeUrl, method, additionalHeaders,
            requestProcessor, responseProcessor, cancel);
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