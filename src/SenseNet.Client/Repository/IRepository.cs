using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.IO;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Defines the main client API for managing content items in a sensenet repository.
/// </summary>
public interface IRepository
{
    /// <summary>
    /// A context object that represents a connection to a sensenet service.
    /// </summary>
    /// <remarks>A repository instance always belongs to a single sensenet service.</remarks>
    public ServerContext Server { get; set; }

    /// <summary>
    /// Gets the registered repository-independent content types.
    /// </summary>
    public RegisteredContentTypes GlobalContentTypes { get; }

    /* ============================================================================ CREATION */

    /// <summary>
    /// Creates a new in-memory local representation of an existing content without loading it from the server.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is less than or equal 0.</exception>
    public Content CreateExistingContent(int id);
    /// <summary>
    /// Creates a new in-memory local representation of an existing content without loading it from the server.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty.</exception>
    public Content CreateExistingContent(string path);
    /// <summary>
    /// Creates a new specialized in-memory local representation of an existing content without loading it from the server.
    /// </summary>
    /// <remarks>The <see cref="T"/> type should be registered during application start either by the
    /// ConfigureSenseNetRepository or the RegisterGlobalContentType methods otherwise
    /// an <see cref="ApplicationException"/> will be thrown.</remarks>
    /// <typeparam name="T">Type of the content.</typeparam>
    /// <param name="id">Content id.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is less than or equal 0.</exception>
    public T CreateExistingContent<T>(int id) where T : Content;
    /// <summary>
    /// Creates a new specialized in-memory local representation of an existing content without loading it from the server.
    /// </summary>
    /// <remarks>The <see cref="T"/> type should be registered during application start either by the
    /// ConfigureSenseNetRepository or the RegisterGlobalContentType methods otherwise
    /// an <see cref="ApplicationException"/> will be thrown.</remarks>
    /// <typeparam name="T">Type of the content.</typeparam>
    /// <param name="path">Content path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty.</exception>
    /// <exception cref="ApplicationException">Thrown when the requested type is registered.</exception>
    public T CreateExistingContent<T>(string path) where T : Content;


    /// <summary>
    /// Creates a new content instance in memory.
    /// If the requested type is not registered, the return value will be a <see cref="Content"/>.
    /// </summary>
    /// <param name="parentPath">Path of the already existing parent (required).</param>
    /// <param name="contentTypeName">Content type name (required).</param>
    /// <param name="name">Name of the content (optional).</param>
    /// <returns>A new content instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>
    /// or <paramref name="contentTypeName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="parentPath"/>
    /// or <paramref name="contentTypeName"/> is empty.</exception>
    public Content CreateContent(string parentPath, string contentTypeName, string name);
    /// <summary>
    /// Creates a new content instance in memory by the given type parameter.
    /// If the content type is registered globally or in a repository with multiple names,
    /// use the optional parameter <paramref name="contentTypeName"/> to properly identify the type and name.
    /// </summary>
    /// <remarks>The <see cref="T"/> type should be registered during application start either by the
    /// ConfigureSenseNetRepository or the RegisterGlobalContentType methods otherwise
    /// an <see cref="ApplicationException"/> will be thrown.</remarks>
    /// <typeparam name="T">Type of the content.</typeparam>
    /// <param name="parentPath">Path of the already existing parent (required).</param>
    /// <param name="contentTypeName">Content type name (optional).</param>
    /// <param name="name">Name of the content (optional).</param>
    /// <returns>A new content instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="parentPath"/> is empty.</exception>
    /// <exception cref="ApplicationException">Thrown when the requested type is registered.</exception>
    public T CreateContent<T>(string parentPath, string contentTypeName, string name) where T : Content;

    Content CreateContentFromJson(JObject jObject, Type contentType = null);

    /// <summary>
    /// Creates a new content instance in memory. When saved, the content is created from the
    /// given content template on the server.
    /// If the content type is registered globally or in a repository with multiple names,
    /// use the optional parameter <paramref name="contentTypeName"/> to properly identify the type and name.
    /// If the requested type is not registered, the return value will be a <see cref="Content"/>.
    /// </summary>
    /// <param name="parentPath">Path of the already existing parent.</param>
    /// <param name="contentTypeName">Content type name.</param>
    /// <param name="name">Name of the content. If it is null, the server will generate a name for the content.</param>
    /// <param name="contentTemplate">Content template name.</param>
    /// <returns>A new content instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>, <paramref name="contentTypeName"/>
    /// or <paramref name="contentTemplate"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>, <paramref name="contentTypeName"/>
    /// or <paramref name="contentTemplate"/> is empty.</exception>
    public Content CreateContentByTemplate(string parentPath, string contentTypeName, string name, string contentTemplate);
    /// <summary>
    /// Creates a new content instance in memory. When saved, the content is created from the
    /// given content template on the server.
    /// </summary>
    /// <remarks>The <see cref="T"/> type should be registered during application start either by the
    /// ConfigureSenseNetRepository or the RegisterGlobalContentType methods otherwise
    /// an <see cref="ApplicationException"/> will be thrown.</remarks>
    /// <typeparam name="T">Type of the content.</typeparam>
    /// <param name="parentPath">Path of the already existing parent.</param>
    /// <param name="contentTypeName">Content type name (optional).</param>
    /// <param name="name">Name of the content. If it is null, the server will generate a name for the content.</param>
    /// <param name="contentTemplate">Content template name.</param>
    /// <returns>A new content instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>
    /// or <paramref name="contentTemplate"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>
    /// or <paramref name="contentTemplate"/> is empty.</exception>
    /// <exception cref="ApplicationException">Thrown when the requested type is registered.</exception>
    public T CreateContentByTemplate<T>(string parentPath, string contentTypeName, string name, string contentTemplate) where T : Content;

    /* ============================================================================ LOAD CONTENT */

    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <param name="id">Content id</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<Content> LoadContentAsync(int id, CancellationToken cancel);
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <param name="path">Content path</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<Content> LoadContentAsync(string path, CancellationToken cancel);
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <param name="requestData">Detailed request information.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<Content> LoadContentAsync(LoadContentRequest requestData, CancellationToken cancel);

    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <typeparam name="T">Well-known type of the content.</typeparam>
    /// <remarks>The <see cref="T"/> type should be registered during application start either by the
    /// ConfigureSenseNetRepository or the RegisterGlobalContentType methods otherwise
    /// an <see cref="ApplicationException"/> will be thrown.</remarks>
    /// <param name="id">Content id</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<T> LoadContentAsync<T>(int id, CancellationToken cancel) where T : Content;
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <remarks>The <see cref="T"/> type should be registered during application start either by the
    /// ConfigureSenseNetRepository or the RegisterGlobalContentType methods otherwise
    /// an <see cref="ApplicationException"/> will be thrown.</remarks>
    /// <typeparam name="T">Well-known type of the content.</typeparam>
    /// <param name="path">Content path</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<T> LoadContentAsync<T>(string path, CancellationToken cancel) where T : Content;
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <remarks>The <see cref="T"/> type should be registered during application start either by the
    /// ConfigureSenseNetRepository or the RegisterGlobalContentType methods otherwise
    /// an <see cref="ApplicationException"/> will be thrown.</remarks>
    /// <typeparam name="T">Well-known type of the content.</typeparam>
    /// <param name="requestData">Detailed request information.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<T> LoadContentAsync<T>(LoadContentRequest requestData, CancellationToken cancel) where T : Content;

    /* ============================================================================ LOAD COLLECTION */

    /// <summary>
    /// Loads child elements of the provided content.
    /// </summary>
    /// <remarks>This method loads only child elements, not the whole subtree.</remarks>
    /// <param name="requestData">Collection request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>Children of the provided content.</returns>
    public Task<IContentCollection<Content>> LoadCollectionAsync(LoadCollectionRequest requestData, CancellationToken cancel);
    /// <summary>
    /// Loads child elements of the provided content.
    /// </summary>
    /// <typeparam name="T">Well-known type of the content.</typeparam>
    /// <param name="requestData">Collection request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>Children of the provided content.</returns>
    /// <exception cref="InvalidCastException"></exception>
    public Task<IContentCollection<T>> LoadCollectionAsync<T>(LoadCollectionRequest requestData, CancellationToken cancel) where T : Content;

    /// <summary>
    /// Gets the count of a children collection. 
    /// </summary>
    /// <remarks>This method counts only child elements, not the whole subtree.</remarks>
    /// <param name="requestData">Collection request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>The count of a children collection.</returns>
    public Task<int> GetContentCountAsync(LoadCollectionRequest requestData, CancellationToken cancel);

    /* ============================================================================ EXISTENCE */

    /// <summary>
    /// Checks if a content with the provided path exists in the repository.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>True if the content exists and the current user can access it.</returns>
    public Task<bool> IsContentExistsAsync(string path, CancellationToken cancel);

    /* ============================================================================ QUERY */

    /// <summary>
    /// Loads content items by a query with lifespan and system filters switched OFF.
    /// </summary>
    /// <remarks>This method is able to load contents from the whole repository, not only a single folder.</remarks>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>List of contents returned by the provided content query.</returns>
    public Task<IContentCollection<Content>> QueryForAdminAsync(QueryContentRequest requestData, CancellationToken cancel);
    /// <summary>
    /// Loads content items by a query with lifespan and system filters switched OFF.
    /// </summary>
    /// <remarks>This method is able to load contents from the whole repository, not only a single folder.</remarks>
    /// <typeparam name="T">Well-known type of the content.</typeparam>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>List of contents returned by the provided content query.</returns>
    /// <exception cref="InvalidCastException"></exception>
    public Task<IContentCollection<T>> QueryForAdminAsync<T>(QueryContentRequest requestData, CancellationToken cancel) where T : Content;

    /// <summary>
    /// Loads content items by a query.
    /// </summary>
    /// <remarks>This method is able to load contents from the whole repository, not only a single folder.</remarks>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>List of contents returned by the provided content query.</returns>
    public Task<IContentCollection<Content>> QueryAsync(QueryContentRequest requestData, CancellationToken cancel);
    /// <summary>
    /// Loads content items by a query.
    /// </summary>
    /// <remarks>This method is able to load contents from the whole repository, not only a single folder.</remarks>
    /// <typeparam name="T">Well-known type of the content.</typeparam>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>List of contents returned by the provided content query.</returns>
    /// <exception cref="InvalidCastException"></exception>
    public Task<IContentCollection<T>> QueryAsync<T>(QueryContentRequest requestData, CancellationToken cancel) where T : Content;

    /// <summary>
    /// Gets the count of content items by a query with lifespan and system filters switched OFF.
    /// </summary>
    /// <remarks>This method is able to count contents in the whole repository, not only a single folder.</remarks>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>Count of contents returned by the provided content query.</returns>
    public Task<int> QueryCountForAdminAsync(QueryContentRequest requestData, CancellationToken cancel);
    /// <summary>
    /// Gets the count of content items by a query.
    /// </summary>
    /// <remarks>This method is able to count contents in the whole repository, not only a single folder.</remarks>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>Count of contents returned by the provided content query.</returns>
    public Task<int> QueryCountAsync(QueryContentRequest requestData, CancellationToken cancel);

    /* ============================================================================ UPLOAD */

    /// <summary>
    /// Uploads the given stream to the destination according to the <paramref name="request"/>.
    /// If the stream is too big, it will be uploaded in several rounds (chunks).
    /// </summary>
    /// <param name="request">Common request parameters that define the destination and control the return data.</param>
    /// <param name="stream">The stream that will be uploaded.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A Task that represents the asynchronous operation and wraps
    /// the uploaded file content returned at the end of the upload request.</returns>
    Task<UploadResult> UploadAsync(UploadRequest request, Stream stream, CancellationToken cancel);
    /// <summary>
    /// Uploads the given stream to the destination according to the <paramref name="request"/>.
    /// If the stream is too big, it will be uploaded in several rounds (chunks).
    /// </summary>
    /// <param name="request">Common request parameters that define the destination and control the return data.</param>
    /// <param name="stream">The stream that will be uploaded.</param>
    /// <param name="progressCallback">Optional callback for tracing upload progress if the file is too big.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A Task that represents the asynchronous operation and wraps
    /// the uploaded file content returned at the end of the upload request.</returns>
    Task<UploadResult> UploadAsync(UploadRequest request, Stream stream, Action<int> progressCallback, CancellationToken cancel);

    /// <summary>
    /// Uploads the given textual data to the destination according to the <paramref name="request"/>.
    /// The contents cannot be bigger than the configured chunk size.
    /// This method is designed for upload relatively small test files e.g. settings.
    /// </summary>
    /// <param name="request">Common request parameters that define the destination and control the return data.</param>
    /// <param name="fileText">The textual data that will be uploaded.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A Task that represents the asynchronous operation and wraps
    /// the uploaded file content returned at the end of the upload request.</returns>
    /// <returns></returns>
    Task<UploadResult> UploadAsync(UploadRequest request, string fileText, CancellationToken cancel);

    /* ============================================================================ DOWNLOAD */

    /// <summary>
    /// Gets a blob storage token that identifies a binary in the storage.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <param name="version">Content version (e.g. V2.3D). If not provided, the highest version 
    /// accessible to the current user will be served.</param>
    /// <param name="propertyName">Binary field name. Default is Binary.</param>
    /// <returns>A Task that represents the asynchronous operation and wraps
    /// a token that can be used with the Blob storage API.</returns>
    Task<string> GetBlobToken(int id, CancellationToken cancel, string version = null, string propertyName = null);
    /// <summary>
    /// Gets a blob storage token that identifies a binary in the storage.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <param name="version">Content version (e.g. V2.3D). If not provided, the highest version 
    /// accessible to the current user will be served.</param>
    /// <param name="propertyName">Binary field name. Default is Binary.</param>
    /// <returns>A Task that represents the asynchronous operation and wraps
    /// a token that can be used with the Blob storage API.</returns>
    Task<string> GetBlobToken(string path, CancellationToken cancel, string version = null, string propertyName = null);

    /* ============================================================================ DELETE */

    /// <summary>
    /// Deletes a content by path.
    /// </summary>
    /// <param name="path">Path of the <see cref="Content"/> to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(string path, bool permanent, CancellationToken cancel);
    /// <summary>
    /// Deletes multiple contents by path.
    /// </summary>
    /// <param name="paths">List of paths of <see cref="Content"/> items to delete.</param>
    /// <param name="permanent">Delete the contents permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(string[] paths, bool permanent, CancellationToken cancel);
    /// <summary>
    /// Deletes a content by id.
    /// </summary>
    /// <param name="id">Id of the <see cref="Content"/> to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(int id, bool permanent, CancellationToken cancel);
    /// <summary>
    /// Deletes multiple contents by id.
    /// </summary>
    /// <param name="ids">List of ids of <see cref="Content"/> items to delete.</param>
    /// <param name="permanent">Delete the contents permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(int[] ids, bool permanent, CancellationToken cancel);
    /// <summary>
    /// Deletes multiple contents by id or path.
    /// </summary>
    /// <param name="idsOrPaths">List of ids or paths of <see cref="Content"/> items to delete.</param>
    /// <param name="permanent">Delete the contents permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(object[] idsOrPaths, bool permanent, CancellationToken cancel);

    /* ============================================================================ MIDDLE LEVEL API */

    Task<T> GetResponseAsync<T>(ODataRequest requestData, HttpMethod method, CancellationToken cancel);
    Task<dynamic> GetResponseJsonAsync(ODataRequest requestData, HttpMethod method, CancellationToken cancel);
    Task<string> GetResponseStringAsync(ODataRequest requestData, HttpMethod method, CancellationToken cancel);

    Task<string> GetResponseStringAsync(Uri uri, HttpMethod method, string postData,
        Dictionary<string, IEnumerable<string>> additionalHeaders, CancellationToken cancel);

    /* ============================================================================ LOW LEVEL API */

    Task ProcessWebResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        HttpContent httpContent,
        Func<HttpResponseMessage, CancellationToken, Task> responseProcessor,
        CancellationToken cancel);

    Task ProcessWebRequestResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor,
        Func<HttpResponseMessage, CancellationToken, Task> responseProcessor,
        CancellationToken cancel);
}