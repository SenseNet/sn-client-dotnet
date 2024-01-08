using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SenseNet.Client.Security;

namespace SenseNet.Client;

//UNDONE: Move to new "enum" file.
//UNDONE: Add doc
public enum VersioningMode
{
    Inherited = 0,
    None = 1,
    MajorOnly = 2,
    MajorAndMinor = 3
}

/// <summary>
/// Central class for all content-related client operations. It contains predefined content 
/// properties and can be extended with custom fields as it is a dynamic type.
/// </summary>
public partial class Content : DynamicObject
{
    private readonly IRestCaller _restCaller;
    private readonly ILogger<Content> _logger;
    private readonly IDictionary<string, object> _fields = new Dictionary<string, object>();

    //============================================================================= Content properties

    /// <summary>
    /// Content id.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Id of the parent content.
    /// </summary>
    public int ParentId { get; set; }
    /// <summary>
    /// Content path.
    /// </summary>
    public string Path { get; set; }

    private string _parentPath;
    /// <summary>
    /// Path of the parent content if available.
    /// </summary>
    public string ParentPath
    {
        get
        {
            // calculate parent path if not given
            if (_parentPath == null && !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Path) && Path.EndsWith(Name))
                _parentPath = Path.Substring(0, Path.LastIndexOf(Name, StringComparison.Ordinal) - 1);

            return _parentPath;
        }
        set { _parentPath = value; }
    }
    /// <summary>
    /// Content name.
    /// </summary>
    public string Name { get; set; }

    //UNDONE: Add doc
    public VersioningMode? VersioningMode { get; set; }
    //UNDONE: Add doc
    public VersioningMode? InheritableVersioningMode { get; set; }

    public string[] FieldNames { get; private set; } = Array.Empty<string>();

    //============================================================================= Technical properties

    private bool Existing { get; set; }

    /// <summary>
    /// The target server that this content belongs to.
    /// </summary>
    public ServerContext Server { get; internal set; }
    public IRepository Repository { get; internal set; }

    private dynamic _responseContent;

    //============================================================================= Constructors

    public Content(IRestCaller restCaller, ILogger<Content> logger)
    {
        _restCaller = restCaller;
        _logger = logger;
    }
    /// <summary>
    /// Internal constructor for client content.
    /// </summary>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    protected Content(ServerContext server)
    {
        Server = server ?? ClientContext.Current.Server;
    }
    /// <summary>
    /// Internal constructor for client content.
    /// </summary>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <param name="responseContent">A JSON response that contains content fields.</param>
    protected Content(ServerContext server, dynamic responseContent) : this(server)
    {
        InitializeFromResponse(responseContent);
    }

    internal void InitializeFromResponse(dynamic responseContent)
    {
        _responseContent = responseContent;
        _fields.Clear();

        // fill local properties from the response object
        if (_responseContent is JObject jo)
        {
            if (jo.Properties().Any(p => p.Name == "Id"))
                Id = _responseContent.Id;
            if (jo.Properties().Any(p => p.Name == "ParentId" && _responseContent.ParentId != null))
                ParentId = _responseContent.ParentId;
            if (jo.Properties().Any(p => p.Name == "Path"))
                Path = _responseContent.Path;
            if (jo.Properties().Any(p => p.Name == "Name"))
                Name = _responseContent.Name;

            FieldNames = jo.Properties().Select(p => p.Name).OrderBy(pn => pn).ToArray();
        }

        SetProperties(responseContent);

        Existing = true;
    }

    // ============================================================================= Creators

    /// <summary>
    /// Creates a new in-memory local representation of an existing content without loading it from the server.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use CreateExistingContent(int id) or CreateExistingContent<T>(int id) methods of the IRepository.")]
    public static Content Create(int id, ServerContext server = null)
    {
        return new Content(server)
        {
            Id = id,
            Existing = true
        };
    }
    /// <summary>
    /// Creates a new in-memory local representation of an existing content without loading it from the server.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use CreateExistingContent(string path) or CreateExistingContent<T>(string path) methods of the IRepository.")]
    public static Content Create(string path, ServerContext server = null)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException("path");

        return new Content(server) 
        { 
            Path = path,
            Name = path.Substring(path.LastIndexOf('/') + 1),
            Existing = true,
        };
    }
    /// <summary>
    /// Creates a new content in memory without saving it.
    /// </summary>
    /// <param name="parentPath">Parent content path in the Content Repository.</param>
    /// <param name="contentType">Content type name.</param>
    /// <param name="name">Name of the new content.</param>
    /// <param name="contentTemplate">Content template path.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use CreateContent or CreateContentByTemplate methods of the IRepository.")]
    public static Content CreateNew(string parentPath, string contentType, string name, string contentTemplate = null, ServerContext server = null)
    {
        return CreateNew<Content>(parentPath, contentType, name, contentTemplate, server);
    }
    /// <summary>
    /// Creates a new specialized content in memory without saving it.
    /// </summary>
    /// <typeparam name="T">One of the specialized client content types inheriting from Content (e.g. Group).</typeparam>
    /// <param name="parentPath">Parent content path in the Content Repository.</param>
    /// <param name="contentType">Content type name.</param>
    /// <param name="name">Name of the new content.</param>
    /// <param name="contentTemplate">Content template path.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use CreateContent<T> or CreateContentByTemplate<T> methods of the IRepository.")]
    public static T CreateNew<T>(string parentPath, string contentType, string name, string contentTemplate = null, ServerContext server = null) where T : Content
    {
        if (string.IsNullOrEmpty(parentPath))
            throw new ArgumentNullException(nameof(parentPath));
        if (string.IsNullOrEmpty(contentType))
            throw new ArgumentNullException(nameof(contentType));

        var ctor = typeof (T).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, 
            null, new[] { typeof (ServerContext) }, null);

        dynamic dc = ctor?.Invoke(new object[] { server }) as T;

        if (dc == null)
            throw new ClientException("Constructor not found or type could not be initialized. " + typeof(T).FullName);

        dc.ParentPath = parentPath;
        dc.Name = name;
        dc.Existing = false;

        // set dynamic properties
        dc.__ContentType = contentType;

        if (!string.IsNullOrEmpty(contentTemplate))
            dc.__ContentTemplate = contentTemplate;

        return dc;
    }
    internal static Content CreateFromResponse(dynamic responseContent, ServerContext server = null)
    {
        return new Content(server, responseContent) 
        {
            Existing = true
        };
    }

    //============================================================================= Static API

    /// <summary>
    /// Loads a content from the server.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadContentAsync(int id, CancellationToken cancel) method of the IRepository.")]
    public static async Task<Content> LoadAsync(int id, ServerContext server = null)
    {
        return await RESTCaller.GetContentAsync(id, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Loads a content from the server.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadContentAsync(string path, CancellationToken cancel) method of the IRepository.")]
    public static async Task<Content> LoadAsync(string path, ServerContext server = null)
    {
        return await RESTCaller.GetContentAsync(path, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Loads a content from the server. Use this method to specify a detailed 
    /// content request, for example which fields you want to expand or select.
    /// </summary>
    /// <param name="requestData">Detailed information that will be sent as part of the request.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadContentAsync<T>(LoadContentRequest requestData, CancellationToken cancel) method of the IRepository.")]
    public static async Task<Content> LoadAsync(ODataRequest requestData, ServerContext server = null)
    {
        return await RESTCaller.GetContentAsync(requestData, server).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks whether a content exists on the server with the provided path.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use IsContentExistsAsync(string path, CancellationToken cancel) method of the IRepository.")]
    public static async Task<bool> ExistsAsync(string path, ServerContext server = null)
    {
        var requestData = new ODataRequest(server)
        {
            Path = path,
            Metadata = MetadataFormat.None,
            Select = new[] { "Id" }
        };

        var content = await RESTCaller.GetContentAsync(requestData, server).ConfigureAwait(false);
        return content != null;
    }
        
    /// <summary>
    /// Loads children of a container.
    /// </summary>
    /// <param name="path">Path of the container.</param>
    /// <param name="server">Target server.</param>
    /// <returns></returns>
    [Obsolete("Use LoadCollectionAsync or LoadCollectionAsync<T> methods of the IRepository.")]
    public static async Task<IEnumerable<Content>> LoadCollectionAsync(string path, ServerContext server = null)
    {
        return await RESTCaller.GetCollectionAsync(path, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Queries the server for content items using the provided request data.
    /// </summary>
    /// <param name="requestData">Detailed information that will be sent as part of the request.
    /// For example Top, Skip, Select, etc.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadCollectionAsync or LoadCollectionAsync<T> methods of the IRepository.")]
    public static async Task<IEnumerable<Content>> LoadCollectionAsync(ODataRequest requestData, ServerContext server = null)
    {
        return await RESTCaller.GetCollectionAsync(requestData, server).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a single referenced content from a reference field.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <param name="fieldName">Reference field name.</param>
    /// <param name="select">Field names of the referenced content to select.</param>
    /// <param name="server">Target server.</param>
    public static async Task<Content> LoadReferenceAsync(int id, string fieldName,
        string[] select = null, ServerContext server = null)
    {
        return (await LoadReferencesAsync(id, fieldName, select, server)
            .ConfigureAwait(false)).FirstOrDefault();
    }
    /// <summary>
    /// Loads a single referenced content from a reference field.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="fieldName">Reference field name.</param>
    /// <param name="select">Field names of the referenced content to select.</param>
    /// <param name="server">Target server.</param>
    public static async Task<Content> LoadReferenceAsync(string path, string fieldName,
        string[] select = null, ServerContext server = null)
    {
        return (await LoadReferencesAsync(path, fieldName, select, server)
            .ConfigureAwait(false)).FirstOrDefault();
    }
    /// <summary>
    /// Loads referenced content from a reference field.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <param name="fieldName">Reference field name.</param>
    /// <param name="select">Field names of the referenced content items to select.</param>
    /// <param name="server">Target server.</param>
    public static async Task<IEnumerable<Content>> LoadReferencesAsync(int id, string fieldName, string[] select = null, ServerContext server = null)
    {
        return await LoadReferencesAsync(null, id, fieldName, select, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Loads referenced content from a reference field.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="fieldName">Reference field name.</param>
    /// <param name="select">Field names of the referenced content items to select.</param>
    /// <param name="server">Target server.</param>
    public static async Task<IEnumerable<Content>> LoadReferencesAsync(string path, string fieldName, string[] select = null, ServerContext server = null)
    {
        return await LoadReferencesAsync(path, 0, fieldName, select, server).ConfigureAwait(false);
    }
    private static async Task<IEnumerable<Content>> LoadReferencesAsync(string path, int id, string fieldName, string[] select = null, ServerContext server = null)
    {
        if (select == null || select.Length == 0)
            select = new[] { "*" };
        var projection = new[] { "Id", "Path", "Type" };
        projection = projection.Union(select.Select(p => fieldName + "/" + p)).ToArray();

        var oreq = new ODataRequest(server)
        {
            Expand = new[] { fieldName },
            Select = projection,
            ContentId = id,
            Path = path
        };

        dynamic content = await Content.LoadAsync(oreq, server).ConfigureAwait(false);
        var refValue = content[fieldName];

        // we assume that this is either an array of content json objects or a single object
        if (refValue is JArray refArray)
            return refArray.Select(c => CreateFromResponse(c, server));

        if (refValue is JValue refJValue && !refJValue.HasValues)
            return Array.Empty<Content>();

        return new List<Content>{ CreateFromResponse(refValue, server) };
    }

    /// <summary>
    /// Loads referenced content from a reference field.
    /// </summary>
    /// <param name="requestData">Detailed information that will be sent as part of the request.
    /// For example Top, Skip, Select, Expand</param>
    /// <param name="server">Target server.</param>
    public static async Task<IEnumerable<Content>> LoadReferencesAsync(ODataRequest requestData, ServerContext server = null)
    {
        if (string.IsNullOrEmpty(requestData.PropertyName))
            throw new ClientException("Please provide a reference field name as the PropertyName in request data.");
            
        var responseText = await RESTCaller.GetResponseStringAsync(requestData.GetUri(), server).ConfigureAwait(false);
        if (string.IsNullOrEmpty(responseText))
            return Array.Empty<Content>();

        var refValue = JsonHelper.Deserialize(responseText).d.results;
        refValue ??= JsonHelper.Deserialize(responseText).d;

        // we assume that this is either an array of content json objects or a single object
        return refValue switch
        {
            JArray refArray => refArray.Select(c => CreateFromResponse(c, server)),
            JValue { HasValues: false } => Array.Empty<Content>(),
            _ => new List<Content> { CreateFromResponse(refValue, server) }
        };
    }

    /// <summary>
    /// Executes a count-only query in a subfolder on the server.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="query">Content query text. If it is empty, the count of children will be returned.</param>
    /// <param name="server">Target server.</param>
    /// <returns>Count of result content.</returns>
    [Obsolete("Use GetContentCountAsync, QueryCountAsync or QueryCountForAdminAsync methods of the IRepository.")]
    public static async Task<int> GetCountAsync(string path, string query, ServerContext server = null)
    {
        var request = new ODataRequest(server)
        {
            Path = path,
            IsCollectionRequest = true,
            ContentQuery = query,
            CountOnly = true
        };

        return await RESTCaller.GetCountAsync(request, server).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query on the server and returns results filtered and expanded 
    /// based on the provided parameters. Both lifespan and system content filters
    /// are disabled.
    /// </summary>
    /// <param name="queryText">Content query text.</param>
    /// <param name="select">Fields to select.</param>
    /// <param name="expand">Fields to expand.</param>
    /// <param name="settings">Query settings.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use QueryForAdminAsync or QueryForAdminAsync<T> methods of the IRepository.")]
    public static async Task<IEnumerable<Content>> QueryForAdminAsync(string queryText, string[] select = null, string[] expand = null, QuerySettings settings = null, ServerContext server = null)
    {
        if (settings == null)
            settings = new QuerySettings();
        settings.EnableAutofilters = FilterStatus.Disabled;
        settings.EnableLifespanFilter = FilterStatus.Disabled;

        return await QueryAsync(queryText, select, expand, settings, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Executes a query on the server and returns results filtered and expanded 
    /// based on the provided parameters. 
    /// </summary>
    /// <param name="queryText">Content query text.</param>
    /// <param name="select">Fields to select.</param>
    /// <param name="expand">Fields to expand.</param>
    /// <param name="settings">Query settings.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use QueryAsync or QueryAsync<T> method of the IRepository.")]
    public static async Task<IEnumerable<Content>> QueryAsync(string queryText, string[] select = null, string[] expand = null, QuerySettings settings = null, ServerContext server = null)
    {
        if (settings == null)
            settings = QuerySettings.Default;

        var oDataRequest = new ODataRequest(server)
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

        return await Content.LoadCollectionAsync(oDataRequest, server).ConfigureAwait(false);
    }

    /// <summary>
    /// Uploads a file to the server into the provided container.
    /// </summary>
    /// <param name="parentPath">Parent path.</param>
    /// <param name="fileName">Name of the file to upload.</param>
    /// <param name="stream">File contents.</param>
    /// <param name="contentType">Content type of the file.</param>
    /// <param name="propertyName">Name of the field to upload to. Default is Binary.</param>
    /// <param name="server">Target server.</param>
    /// <param name="progressCallback">An optional callback method that is called after each chunk is uploaded to the server.</param>
    /// <returns>The uploaded file content returned at the end of the upload request.</returns>
    [Obsolete("Use the UploadAsync method of IRepository.")]
    public static async Task<Content> UploadAsync(string parentPath, string fileName, Stream stream, string contentType = null, string propertyName = null, ServerContext server = null, Action<int> progressCallback = null)
    {
        var uploadData = new UploadData() 
        { 
            FileName = fileName,
            FileLength = stream.Length
        };

        if (!string.IsNullOrEmpty(contentType))
            uploadData.ContentType = contentType;

        if (!string.IsNullOrEmpty(propertyName))
            uploadData.PropertyName = propertyName;

        return await RESTCaller.UploadAsync(stream, uploadData, parentPath, server, progressCallback).ConfigureAwait(false);
    }
    /// <summary>
    /// Uploads a file to the server into the provided container.
    /// </summary>
    /// <param name="parentId">Parent id.</param>
    /// <param name="fileName">Name of the file to upload.</param>
    /// <param name="stream">File contents.</param>
    /// <param name="contentType">Content type of the file. Default is determined by the container.</param>
    /// <param name="propertyName">Name of the field to upload to. Default is Binary.</param>
    /// <param name="server">Target server.</param>
    /// <param name="progressCallback">An optional callback method that is called after each chunk is uploaded to the server.</param>
    /// <returns>The uploaded file content returned at the end of the upload request.</returns>
    [Obsolete("Use the UploadAsync method of IRepository.")]
    public static async Task<Content> UploadAsync(int parentId, string fileName, Stream stream, string contentType = null, string propertyName = null, ServerContext server = null, Action<int> progressCallback = null)
    {
        var uploadData = new UploadData()
        {
            FileName = fileName,
            FileLength = stream.Length
        };

        if (!string.IsNullOrEmpty(contentType))
            uploadData.ContentType = contentType;

        if (!string.IsNullOrEmpty(propertyName))
            uploadData.PropertyName = propertyName;

        return await RESTCaller.UploadAsync(stream, uploadData, parentId, server, progressCallback).ConfigureAwait(false);
    }


    /// <summary>
    /// Uploads a short text file to the server into the provided container.
    /// The contents cannot be bigger than the configured chunk size.
    /// </summary>
    /// <param name="parentPath">Parent path.</param>
    /// <param name="fileName">Name of the file to upload.</param>
    /// <param name="fileText">File content.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="contentType">Content type of the file.</param>
    /// <param name="propertyName">Name of the field to upload to. Default is Binary.</param>
    /// <param name="server">Target server.</param>
    /// <returns>The uploaded file content returned at the end of the upload request.</returns>
    [Obsolete("Use the UploadTextAsync method of IRepository.")]
    public static async Task<Content> UploadTextAsync(string parentPath, string fileName, string fileText,
        CancellationToken cancellationToken,
        string contentType = null, string propertyName = null, ServerContext server = null)
    {
        var uploadData = new UploadData()
        {
            FileName = fileName,
            ContentType = contentType,
        };

        if (!string.IsNullOrEmpty(contentType))
            uploadData.ContentType = contentType;

        if (!string.IsNullOrEmpty(propertyName))
            uploadData.PropertyName = propertyName;

        return await RESTCaller.UploadTextAsync(fileText, uploadData, parentPath, cancellationToken, server)
            .ConfigureAwait(false);
    }
    /// <summary>
    /// Uploads a short text file to the server into the provided container.
    /// The contents cannot be bigger than the configured chunk size.
    /// </summary>
    /// <param name="parentId">Parent id.</param>
    /// <param name="fileName">Name of the file to upload.</param>
    /// <param name="fileText">File content.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="contentType">Content type of the file.</param>
    /// <param name="propertyName">Name of the field to upload to. Default is Binary.</param>
    /// <param name="server">Target server.</param>
    /// <returns>The uploaded file content returned at the end of the upload request.</returns>
    [Obsolete("Use the UploadTextAsync method of IRepository.")]
    public static async Task<Content> UploadTextAsync(int parentId, string fileName, string fileText,
        CancellationToken cancellationToken,
        string contentType = null, string propertyName = null, ServerContext server = null)
    {
        var uploadData = new UploadData()
        {
            FileName = fileName,
            ContentType = contentType,
        };

        if (!string.IsNullOrEmpty(contentType))
            uploadData.ContentType = contentType;

        if (!string.IsNullOrEmpty(propertyName))
            uploadData.PropertyName = propertyName;

        return await RESTCaller.UploadTextAsync(fileText, uploadData, parentId, cancellationToken, server)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Uploads a file or a custom binary property of a content in the provided container.
    /// </summary>
    /// <param name="parentPath">Parent path.</param>
    /// <param name="contentName">Name of the content to create or update.</param>
    /// <param name="fileSize">Full length of the binary data.</param>
    /// <param name="blobCallback">An action that is called between the initial and the finalizer requests. 
    /// Use this to actually save the binary through the blob storage component.
    /// Parameters: contentId, versionId, token</param>
    /// <param name="contentType">Content type of the new content. Default is determined by the allowed child types in the container.</param>
    /// <param name="fileName">Binary file name. Default is the content name.</param>
    /// <param name="propertyName">Binary field name. Default is "Binary".</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Do not use this method anymore.")]
    public static async Task UploadBlobAsync(string parentPath, string contentName, long fileSize,
        Func<int, int, string, Task> blobCallback, string contentType = null, string fileName = null, 
        string propertyName = null, ServerContext server = null)
    {
        if (string.IsNullOrEmpty(parentPath))
            throw new ArgumentNullException(nameof(parentPath));
        if (string.IsNullOrEmpty(contentName))
            throw new ArgumentNullException(nameof(contentName));
        if (blobCallback == null)
            throw new ArgumentNullException(nameof(blobCallback));

        // send initial request
        var responseText = await RESTCaller.GetResponseStringAsync(parentPath, "StartBlobUploadToParent", HttpMethod.Post,
                JsonHelper.Serialize(new
                {
                    name = contentName,
                    contentType,
                    fullSize = fileSize,
                    fieldName = propertyName
                }),
                server)
            .ConfigureAwait(false);

        // call the common method that contains the part that is the same for all implementations
        await SaveAndFinalizeBlobInternalAsync(responseText, fileSize, blobCallback, fileName, propertyName, server)
            .ConfigureAwait(false);
    }
    /// <summary>
    /// Uploads a file or a custom binary property of a content in the provided container.
    /// </summary>
    /// <param name="parentId">Parent id.</param>
    /// <param name="contentName">Name of the content to create or update.</param>
    /// <param name="fileSize">Full length of the binary data.</param>
    /// <param name="blobCallback">An action that is called between the initial and the finalizer requests. 
    /// Use this to actually save the binary through the blob storage component.
    /// Parameters: contentId, versionId, token</param>
    /// <param name="contentType">Content type of the new content. Default is determined by the allowed child types in the container.</param>
    /// <param name="fileName">Binary file name. Default is the content name.</param>
    /// <param name="propertyName">Binary field name. Default is "Binary".</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Do not use this method anymore.")]
    public static async Task UploadBlobAsync(int parentId, string contentName, long fileSize,
        Func<int, int, string, Task> blobCallback, string contentType = null, string fileName = null,
        string propertyName = null, ServerContext server = null)
    {
        if (string.IsNullOrEmpty(contentName))
            throw new ArgumentNullException(nameof(contentName));
        if (blobCallback == null)
            throw new ArgumentNullException(nameof(blobCallback));

        // send initial request
        var responseText = await RESTCaller.GetResponseStringAsync(parentId, "StartBlobUploadToParent", HttpMethod.Post,
                JsonHelper.Serialize(new
                {
                    name = contentName,
                    contentType,
                    fullSize = fileSize,
                    fieldName = propertyName
                }),
                server)
            .ConfigureAwait(false);

        // call the common method that contains the part that is the same for all implementations
        await SaveAndFinalizeBlobInternalAsync(responseText, fileSize, blobCallback, fileName, propertyName, server)
            .ConfigureAwait(false);
    }
    [Obsolete("Do not use this method anymore.")]
    private static async Task SaveAndFinalizeBlobInternalAsync(string initResponse, long fileSize,
        Func<int, int, string, Task> blobCallback, string fileName = null,
        string propertyName = null, ServerContext server = null)
    {
        // parse the response of the initial request
        var response = JsonHelper.Deserialize(initResponse);
        int contentId = response.id;
        string token = response.token;
        int versionId = response.versionId;

        // save binary through the blob storage
        await blobCallback(contentId, versionId, token).ConfigureAwait(false);

        // send final request
        await RESTCaller.GetResponseStringAsync(contentId, "FinalizeBlobUpload", HttpMethod.Post,
                JsonHelper.Serialize(new
                {
                    token,
                    fullSize = fileSize,
                    fieldName = propertyName,
                    fileName
                }),
                server)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a blob storage token that identifies a binary in the storage.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <param name="version">Content version (e.g. V2.3D). If not provided, the highest version 
    /// accessible to the current user will be served.</param>
    /// <param name="propertyName">Binary field name. Default is Binary.</param>
    /// <param name="server">Target server.</param>
    /// <returns>A token that can be used with the Blob storage API.</returns>
    [Obsolete("Do not use this method anymore.")]
    public static async Task<string> GetBlobToken(int id, string version = null, string propertyName = null, ServerContext server = null)
    {
        var responseText = await RESTCaller.GetResponseStringAsync(id, "GetBinaryToken", HttpMethod.Post,
                JsonHelper.Serialize(new { version, fieldName = propertyName }), server)
            .ConfigureAwait(false);

        var response = JsonHelper.Deserialize(responseText);

        return response.token;
    }
    /// <summary>
    /// Gets a blob storage token that identifies a binary in the storage.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="version">Content version (e.g. V2.3D). If not provided, the highest version 
    /// accessible to the current user will be served.</param>
    /// <param name="propertyName">Binary field name. Default is Binary.</param>
    /// <param name="server">Target server.</param>
    /// <returns>A token that can be used with the Blob storage API.</returns>
    [Obsolete("Do not use this method anymore.")]
    public static async Task<string> GetBlobToken(string path, string version = null, string propertyName = null, ServerContext server = null)
    {
        var responseText = await RESTCaller.GetResponseStringAsync(path, "GetBinaryToken", HttpMethod.Post,
                JsonHelper.Serialize(new { version, fieldName = propertyName }), server)
            .ConfigureAwait(false);

        var response = JsonHelper.Deserialize(responseText);

        return response.token;
    }

    /// <summary>
    /// Deletes the content by Path.
    /// </summary>
    /// <param name="path">Path of the <see cref="Content"/> to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(string path, bool permanent, CancellationToken cancel) method of the IRepository.")]
    public static async Task DeleteAsync(string path, bool permanent, CancellationToken cancellationToken,
        ServerContext server = null)
    {
        await DeleteAsync(new[] {path}, permanent, cancellationToken, server).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes one or more content by Path.
    /// </summary>
    /// <param name="paths">Paths of the <see cref="Content"/> objects to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(string[] paths, bool permanent, CancellationToken cancel) method of the IRepository.")]
    public static async Task DeleteAsync(string[] paths, bool permanent, CancellationToken cancellationToken,
        ServerContext server = null)
    {
        await DeleteAsync(paths.Cast<object>().ToArray(), permanent, cancellationToken, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Deletes the content by Id.
    /// </summary>
    /// <param name="id">Id of the <see cref="Content"/> to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(int id, bool permanent, CancellationToken cancel) method of the IRepository.")]
    public static async Task DeleteAsync(int id, bool permanent, CancellationToken cancellationToken,
        ServerContext server = null)
    {
        await DeleteAsync(new[] { id }, permanent, cancellationToken, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Deletes one or more content by Id.
    /// </summary>
    /// <param name="ids">Paths of the <see cref="Content"/> objects to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(int[] ids, bool permanent, CancellationToken cancel) method of the IRepository.")]
    public static async Task DeleteAsync(int[] ids, bool permanent, CancellationToken cancellationToken,
        ServerContext server = null)
    {
        await DeleteAsync(ids.Cast<object>().ToArray(), permanent, cancellationToken, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Deletes one or more content by Id or Path.
    /// </summary>
    /// <param name="idsOrPaths">One or more id or path of the <see cref="Content"/> objects to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(object[] idsOrPaths, bool permanent, CancellationToken cancel) method of the IRepository.")]
    public static async Task DeleteAsync(object[] idsOrPaths, bool permanent, CancellationToken cancellationToken,
        ServerContext server = null)
    {
        await RESTCaller.GetResponseStringAsync("/Root", "DeleteBatch", HttpMethod.Post, 
                JsonHelper.GetJsonPostModel(new
                {
                    permanent,
                    paths = idsOrPaths
                }), server)
            .ConfigureAwait(false);
    }

    //============================================================================= Instance API

    /// <summary>
    /// Saves the content to the server.
    /// </summary>
    [Obsolete("Use overload SaveAsync(CancellationToken).")]
    public Task SaveAsync()
    {
        return SaveAsync(CancellationToken.None);
    }
    public async Task SaveAsync(CancellationToken cancel)
    {
        dynamic postData = new ExpandoObject();
        postData.Name = this.Name;

        // add local field values to post data
        if (_fields != null)
        {
            var dict = postData as IDictionary<string, object>;

            foreach (var field in _fields)
            {
                dict[field.Key] = field.Value;
            }
        }

        //if (this.GetType() != typeof(Content))
        //{
            try
            {
                ManagePostData(postData);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Cannot save the content. Id: {Id}, Path: '{Path}'. See inner exception for details.", ex);
            }
        //}

        dynamic responseContent;
        if (_restCaller == null)
        {
            // Backward compatible version
            responseContent = Existing
                ? (this.Id > 0
                    ? await RESTCaller.PatchContentAsync(this.Id, postData, Server).ConfigureAwait(false)
                    : await RESTCaller.PatchContentAsync(this.Path, postData, Server).ConfigureAwait(false))
                : await RESTCaller.PostContentAsync(this.ParentPath, postData, Server).ConfigureAwait(false);
        }
        else
        {
            // Modernized version
            responseContent = Existing
                ? (this.Id > 0
                    ? await PatchContentAsync(this.Id, postData, cancel).ConfigureAwait(false)
                    : await PatchContentAsync(this.Path, postData, cancel).ConfigureAwait(false))
                : await PostContentAsync(this.ParentPath, postData, cancel).ConfigureAwait(false);
        }

        // reset local values
        InitializeFromResponse(responseContent);
    }
    private async Task<dynamic> PostContentAsync(string parentPath, object postData, CancellationToken cancel)
    {
        return await PostContentInternalAsync(parentPath, postData, HttpMethod.Post, cancel).ConfigureAwait(false);
    }
    private async Task<dynamic> PatchContentAsync(int contentId, object postData, CancellationToken cancel)
    {
        return await PostContentInternalAsync(contentId, postData, HttpMethods.PATCH, cancel).ConfigureAwait(false);
    }
    private async Task<dynamic> PatchContentAsync(string path, object postData, CancellationToken cancel)
    {
        return await PostContentInternalAsync(path, postData, HttpMethods.PATCH, cancel).ConfigureAwait(false);
    }
    private async Task<dynamic> PutContentAsync(string path, object postData, CancellationToken cancel)
    {
        return await PostContentInternalAsync(path, postData, HttpMethod.Put, cancel).ConfigureAwait(false);
    }
    private async Task<dynamic> PostContentInternalAsync(string path, object postData, HttpMethod method, CancellationToken cancel)
    {
        var reqData = new ODataRequest(Server)
        {
            Path = path,
            PostData = postData
        };
        var rs = await Repository.GetResponseStringAsync(reqData, method, cancel).ConfigureAwait(false);
        return JsonHelper.Deserialize(rs).d;
    }
    private async Task<dynamic> PostContentInternalAsync(int contentId, object postData, HttpMethod method, CancellationToken cancel)
    {
        var reqData = new ODataRequest(Server)
        {
            ContentId = contentId,
            PostData = postData
        };
        var rs = await Repository.GetResponseStringAsync(reqData, method, cancel).ConfigureAwait(false);
        return JsonHelper.Deserialize(rs).d;
    }

    /// <summary>
    /// Deletes the content.
    /// </summary>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    [Obsolete("Use overload DeleteAsync(bool, CancellationToken).")]
    public Task DeleteAsync(bool permanent = true)
    {
        return DeleteAsync(permanent, CancellationToken.None);
    }
    /// <summary>
    /// Deletes the content.
    /// </summary>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteAsync(bool permanent, CancellationToken cancel)
    {
        return ExecuteSimpleAction("Delete", new { permanent }, cancel);
    }

    /// <summary>
    /// Moves the content to the target location.
    /// </summary>
    /// <param name="targetPath">Target path.</param>
    [Obsolete("Use overload MoveToAsync(string, CancellationToken).")]
    public Task MoveToAsync(string targetPath)
    {
        return MoveToAsync(targetPath, CancellationToken.None);
    }
    /// <summary>
    /// Moves the content to the target location.
    /// </summary>
    /// <param name="targetPath">Target path.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task MoveToAsync(string targetPath, CancellationToken cancel)
    {
        if (Id == 0 && Path == null)
            throw new InvalidOperationException("Cannot execute 'MoveTo' action of a Content if neither Id and Path provided.");
        if (targetPath == null)
            throw new ArgumentNullException(nameof(targetPath));

        return ExecuteSimpleAction(
            request: new ODataRequest(Server)
            {
                Path = "/Root",
                ActionName = "MoveBatch"
            },
            postData: new
            {
                paths = new object[] {Id == 0 ? Path : Id},
                targetPath
            }, cancel);
    }

    /// <summary>
    /// Creates a copy of the content to the target location.
    /// </summary>
    /// <param name="targetPath">Target path.</param>
    [Obsolete("Use overload CopyToAsync(string, CancellationToken).")]
    public Task CopyToAsync(string targetPath)
    {
        return CopyToAsync(targetPath, CancellationToken.None);
    }
    /// <summary>
    /// Creates a copy of the content to the target location.
    /// </summary>
    /// <param name="targetPath">Target path.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task CopyToAsync(string targetPath, CancellationToken cancel)
    {
        if (Id == 0 && Path == null)
            throw new InvalidOperationException("Cannot execute 'CopyTo' action of a Content if neither Id and Path provided.");
        if (targetPath == null)
            throw new ArgumentNullException(nameof(targetPath));

        return ExecuteSimpleAction(
            request: new ODataRequest(Server)
            {
                Path = "/Root",
                ActionName = "CopyBatch"
            },
            postData: new
            {
                paths = new object[] { Id == 0 ? Path : Id },
                targetPath
            }, cancel);
    }

    /// <summary>
    /// Locks the content for the current user.
    /// </summary>
    [Obsolete("Use overload CheckOutAsync(CancellationToken).")]
    public Task CheckOutAsync()
    {
        return CheckOutAsync(CancellationToken.None);
    }
    /// <summary>
    /// Locks the content for the current user.
    /// </summary>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task CheckOutAsync(CancellationToken cancel)
    {
        return ExecuteSimpleAction("CheckOut", null, cancel);
    }

    /// <summary>
    /// Check in the content.
    /// </summary>
    [Obsolete("Use overload CheckInAsync(CancellationToken).")]
    public Task CheckInAsync()
    {
        return CheckInAsync(CancellationToken.None);
    }
    /// <summary>
    /// Check in the content.
    /// </summary>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task CheckInAsync(CancellationToken cancel)
    {
        return ExecuteSimpleAction("CheckIn", null, cancel);
    }

    /// <summary>
    /// Undo all modifications on the content since the last checkout operation.
    /// </summary>
    [Obsolete("Use overload UndoCheckOutAsync(CancellationToken).")]
    public Task UndoCheckOutAsync()
    {
        return UndoCheckOutAsync(CancellationToken.None);
    }
    /// <summary>
    /// Undo all modifications on the content since the last checkout operation.
    /// </summary>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task UndoCheckOutAsync(CancellationToken cancel)
    {
        return ExecuteSimpleAction("UndoCheckOut", null, cancel);
    }

    /// <summary>
    /// Publishes the requested content. The version number is changed to the next major version
    /// according to the content's versioning and approving mode.
    /// </summary>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task PublishAsync(CancellationToken cancel)
    {
        return ExecuteSimpleAction("Publish", null, cancel);
    }

    /// <summary>
    /// Approves the requested content. The content's version number will be the next major version according to
    /// the content's versioning and approving mode.
    /// </summary>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task ApproveAsync(CancellationToken cancel)
    {
        return ExecuteSimpleAction("Approve", null, cancel);
    }

    /// <summary>
    /// Rejects the modifications of the requested content and persists
    /// the <paramref name="rejectReason"/> if there is.
    /// </summary>
    /// <param name="rejectReason">A short description of the reason for rejection.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task RejectAsync(string rejectReason, CancellationToken cancel)
    {
        object postData = null;
        if(rejectReason != null)
            postData = new { rejectReason };
        return ExecuteSimpleAction("Reject", postData, cancel);
    }

    /// <summary>
    /// Drops the last draft version of the requested content if there is. This operation is allowed only
    /// for users who have <c>ForceCheckIn</c> permission on this content. 
    /// </summary>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task ForceUndoCheckOutAsync(CancellationToken cancel)
    {
        return ExecuteSimpleAction("ForceUndoCheckOut", null, cancel);
    }

    /// <summary>
    /// Restores an old existing version as the last version according to the content's versioning mode.
    /// The old version is identified by the <paramref name="version"/> parameter that can be in
    /// one of the following forms:
    /// - [major].[minor] e.g. "1.2"
    /// - V[major].[minor] e.g. "V1.2"
    /// - [major].[minor].[status] e.g. "1.2.D"
    /// - V[major].[minor].[status] e.g. "V1.2.D"
    /// <para>Note that [status] is not required but an incorrect value causes an exception.</para>
    /// </summary>
    /// <param name="version">The old version number.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task RestoreVersionAsync(string version, CancellationToken cancel)
    {
        if (version == null)
            throw new ArgumentNullException(nameof(version));
        return ExecuteSimpleAction("RestoreVersion", new { version }, cancel);
    }

    private Task ExecuteSimpleAction(string actionName, object postData, CancellationToken cancel)
    {
        if (Id == 0 && Path == null)
            throw new InvalidOperationException($"Cannot execute '{actionName}' action of a Content if neither Id and Path provided.");

        var requestData = new ODataRequest(Server)
        {
            ContentId = this.Id,
            Path = this.Path,
            ActionName = actionName
        };
        return ExecuteSimpleAction(requestData, postData, cancel);
    }
    private async Task ExecuteSimpleAction(ODataRequest request, object postData, CancellationToken cancel)
    {
        if (_restCaller == null)
        {
            string requestBody = null;
            if (postData != null)
                requestBody = JsonHelper.GetJsonPostModel(postData);
            await RESTCaller.GetResponseStringAsync(request.GetUri(), Server, HttpMethod.Post, requestBody)
                .ConfigureAwait(false);
        }
        else
        {
            request.PostData = postData;
            await Repository.GetResponseStringAsync(request, HttpMethod.Post, cancel).ConfigureAwait(false);
        }
    }

    //----------------------------------------------------------------------------- Security

    /// <summary>
    /// Checks whether a user has the provided permissions on the content.
    /// </summary>
    /// <param name="permissions">Permission names to check.</param>
    /// <param name="user">The user who's permissions need to be checked. If it is not provided, the server checks the current user.</param>
    /// <param name="server">Target server.</param>
    public async Task<bool> HasPermissionAsync(string[] permissions, string user = null, ServerContext server = null)
    {
        return await SecurityManager.HasPermissionAsync(this.Id, permissions, user, server).ConfigureAwait(false);
    }

    /// <summary>
    /// Breaks permissions on the content.
    /// </summary>
    /// <param name="server">Target server.</param>
    public async Task BreakInheritanceAsync(ServerContext server = null)
    {
        await SecurityManager.BreakInheritanceAsync(this.Id, server).ConfigureAwait(false);
    }
    /// <summary>
    /// Removes permission break on the content.
    /// </summary>
    /// <param name="server">Target server.</param>
    public async Task UnbreakInheritanceAsync(ServerContext server = null)
    {
        await SecurityManager.UnbreakInheritanceAsync(this.Id, server).ConfigureAwait(false);
    }

    //============================================================================= DynamicObject implementation

    /// <summary>
    /// Gets a dynamic property value, for example value of a content field (DynamicObject implementation).
    /// </summary>
    /// <param name="binder">Property binder definition.</param>
    /// <param name="result">Field value if found.</param>
    /// <returns>Whether the field value was found or not.</returns>
    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        // If the property name is found in the local dictionary, set the result and return
        if (_fields != null && _fields.TryGetValue(binder.Name, out result))
            return true;

        // fallback to the inner dynamic object (received from a request previously)
        if (_responseContent != null)
        {
            result = _responseContent[binder.Name];
            return true;
        }

        result = null;
        return false;
    }
    /// <summary>
    /// Sets a dynamic property value, for example value of a content field (DynamicObject implementation).
    /// </summary>
    /// <param name="binder">Property binder definition.</param>
    /// <param name="value">Field value to set.</param>
    /// <returns>This operation is always succesful.</returns>
    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        _fields[binder.Name] = value;

        return true;
    }
    /// <summary>
    /// Calls a dynamic method on a content. It will be resolved into an asynchronous OData action request (DynamicObject implementation).
    /// </summary>
    /// <param name="binder">Method binder definition.</param>
    /// <param name="args">Method arguments provided by the caller.</param>
    /// <param name="result">An awaitable Task&lt;dynamic&gt; object containing the response of the action request.</param>
    /// <returns>Aleays true.</returns>
    public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
    {
        var requestData = new ODataRequest(Server)
        {
            ContentId = this.Id,
            Path = this.Path,
            ActionName = binder.Name
        };

        HttpMethod method = null;
        object postData = null;

        // Get http method and post data from the optional argument list. 
        // It is possible to provide both of them or none of them.
        if (args != null && args.Length > 0)
        {
            // tale only the first 2 params into account
            for (var i = 0; i < Math.Min(2, args.Length); i++)
            {
                var httpMethod = args[i] as HttpMethod;
                if (httpMethod != null)
                    method = httpMethod;
                else
                    postData = args[i];
            }
        }

        result = RESTCaller.GetResponseJsonAsync(requestData, Server, method, postData);

        return true;
    }

    /// <summary>
    /// Gets or sets a content field value. If the value has been set locally, it returns that. 
    /// Otherwise it checks the fields returned from the server.
    /// </summary>
    /// <param name="fieldName">Name of the field.</param>
    /// <returns>The field value if found, otherwise null.</returns>
    public object this[string fieldName]
    {
        get
        {
            object value;
            // If the property name is found in the local dictionary, set the result and return
            if (_fields != null && _fields.TryGetValue(fieldName, out value))
                return value;

            // fallback to the inner dynamic object (received from a request previously)
            if (_responseContent != null)
                return _responseContent[fieldName];

            return null;
        }
        set
        {
            _fields[fieldName] = value;
        }
    }
}