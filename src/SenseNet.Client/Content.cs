﻿using Newtonsoft.Json.Linq;
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
using AngleSharp.Dom;

namespace SenseNet.Client;

public enum VersioningMode
{
    Inherited = 0,
    None = 1,
    MajorOnly = 2,
    MajorAndMinor = 3
}
public enum ApprovingEnabled
{
    Inherited = 0,
    No = 1,
    Yes = 2
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
    public string? Path { get; set; }

    private string? _parentPath;
    /// <summary>
    /// Path of the parent content if available.
    /// </summary>
    public string? ParentPath
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
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public int? VersionId { get; set; }
    public int? Index { get; set; }
    public string? Icon { get; set; }

    public DateTime? CreationDate { get; set; }
    public DateTime? ModificationDate { get; set; }
    public DateTime? VersionCreationDate { get; set; }
    public DateTime? VersionModificationDate { get; set; }

    public User? Owner { get; set; }
    public User? CreatedBy { get; set; }
    public User? ModifiedBy { get; set; }
    public User? VersionModifiedBy { get; set; }
    public User? CheckedOutTo { get; set; }

    public VersioningMode? VersioningMode { get; set; }
    public VersioningMode? InheritableVersioningMode { get; set; }
    public ApprovingEnabled? ApprovingMode { get; set; }
    public ApprovingEnabled? InheritableApprovingMode { get; set; }

    public string? CheckInComments { get; set; }
    public string? RejectReason { get; set; }
    public IEnumerable<Content>? Versions { get; set; }
    public bool? EnableLifespan { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTill { get; set; }

    public bool? Hidden { get; set; }
    public bool? IsFolder { get; set; }
    public bool? IsSystemContent { get; set; }
    public bool? Locked { get; set; }
    public string? Tags { get; set; }
    public bool? TrashDisabled { get; set; }
    public Workspace? Workspace { get; set; }

    public IEnumerable<ContentType>? AllowedChildTypes { get; set; }
    [JsonIgnore] // Read only field
    public IEnumerable<ContentType>? EffectiveAllowedChildTypes { get; set; }

    // Not implemented fields
    //   Noise:
    //     CreatedById:Integer
    //     Depth:Integer
    //     ExtensionData:LongText
    //     InFolder:ShortText
    //     InTree:ShortText
    //     IsRateable:Boolean
    //     IsTaggable:Boolean
    //     ModifiedById:Integer
    //     OwnerId:Integer
    //     Publishable:Boolean
    //     Rate:Rating
    //     RateAvg:Number
    //     RateCount:Integer
    //     RateStr:ShortText
    //     SavingState:Choice
    //     VersionCreatedBy:Reference
    //   Nice to have on one complex property:
    //      SharedBy:Sharing
    //      SharedWith:Sharing
    //      Sharing:Sharing
    //      SharingLevel:Sharing
    //      SharingMode:Sharing


    public string[] FieldNames { get; private set; } = Array.Empty<string>();

    /*-------------------------------------------------------------------------- SnLinq */

    public bool InFolder(string path) => throw new NotSupportedException("Use only in a LINQ expression");
    public bool InFolder(Content content) => throw new NotSupportedException("Use only in a LINQ expression");
    public bool InTree(string path) => throw new NotSupportedException("Use only in a LINQ expression");
    public bool InTree(Content content) => throw new NotSupportedException("Use only in a LINQ expression");

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
    protected Content(ServerContext? server)
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
    [Obsolete("Use CreateExistingContent(int id) or CreateExistingContent<T>(int id) methods of the IRepository.", true)]
    public static Content Create(int id, ServerContext? server = null)
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
    [Obsolete("Use CreateExistingContent(string path) or CreateExistingContent<T>(string path) methods of the IRepository.", true)]
    public static Content Create(string path, ServerContext? server = null)
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
    [Obsolete("Use CreateContent or CreateContentByTemplate methods of the IRepository.", true)]
    public static Content CreateNew(string parentPath, string contentType, string name, string? contentTemplate = null, ServerContext? server = null)
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
    [Obsolete("Use CreateContent<T> or CreateContentByTemplate<T> methods of the IRepository.", true)]
    public static T CreateNew<T>(string parentPath, string contentType, string name, string? contentTemplate = null, ServerContext? server = null) where T : Content
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
    internal static Content CreateFromResponse(dynamic responseContent, ServerContext? server = null)
    {
        return new Content(server, responseContent) 
        {
            Existing = true
        };
    }

    /// <summary>
    /// This method is used in processing LINQ expressions only. Do not use it in your code.
    /// </summary>
    /// <param name="fields"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static T Create<T>(params object?[] fields) where T : Content
    {
        throw new NotSupportedException("This method is used in processing LINQ expressions only. Do not use it in your code.");
    }

    //============================================================================= Static API

    #region Obsolete methods with build time error

    /// <summary>
    /// Loads a content from the server.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadContentAsync(int id, CancellationToken cancel) method of the IRepository.", true)]
    public static Task<Content?> LoadAsync(int id, ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Loads a content from the server.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadContentAsync(string path, CancellationToken cancel) method of the IRepository.", true)]
    public static Task<Content> LoadAsync(string path, ServerContext? server = null) => throw new NotSupportedException();
    /// <summary>
    /// Loads a content from the server. Use this method to specify a detailed 
    /// content request, for example which fields you want to expand or select.
    /// </summary>
    /// <param name="requestData">Detailed information that will be sent as part of the request.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadContentAsync<T>(LoadContentRequest requestData, CancellationToken cancel) method of the IRepository.", true)]
    public static Task<Content?> LoadAsync(ODataRequest requestData, ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Checks whether a content exists on the server with the provided path.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use IsContentExistsAsync(string path, CancellationToken cancel) method of the IRepository.", true)]
    public static Task<bool> ExistsAsync(string path, ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Loads children of a container.
    /// </summary>
    /// <param name="path">Path of the container.</param>
    /// <param name="server">Target server.</param>
    /// <returns></returns>
    [Obsolete("Use LoadCollectionAsync or LoadCollectionAsync<T> methods of the IRepository.", true)]
    public static Task<IEnumerable<Content>> LoadCollectionAsync(string path, ServerContext? server = null) => throw new NotSupportedException();
    /// <summary>
    /// Queries the server for content items using the provided request data.
    /// </summary>
    /// <param name="requestData">Detailed information that will be sent as part of the request.
    /// For example Top, Skip, Select, etc.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadCollectionAsync or LoadCollectionAsync<T> methods of the IRepository.", true)]
    public static Task<IEnumerable<Content>> LoadCollectionAsync(ODataRequest requestData, ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Loads referenced content from a reference field.
    /// </summary>
    /// <param name="requestData">Detailed information that will be sent as part of the request.
    /// For example Top, Skip, Select, Expand</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use LoadReferencesAsync method of the IRepository.", true)]
    public static Task<IEnumerable<Content>> LoadReferencesAsync(ODataRequest requestData,
        ServerContext? server = null) => throw new NotSupportedException();

    #endregion

    public Task<Content> LoadReferenceAsync(string fieldName, CancellationToken cancel)
    {
        return Repository.LoadReferenceAsync<Content>(CreateLoadReferenceRequest(fieldName), cancel);
    }

    public Task<TContent> LoadReferenceAsync<TContent>(string fieldName, CancellationToken cancel) where TContent : Content
    {
        return Repository.LoadReferenceAsync<TContent>(CreateLoadReferenceRequest(fieldName), cancel);
    }
    public async Task<Content> LoadReferenceAsync(LoadReferenceRequest requestData, CancellationToken cancel)
    {
        AssertLoadReferenceRequest(requestData);
        requestData.ContentId = this.Id;
        return await Repository.LoadReferenceAsync<Content>(requestData, cancel).ConfigureAwait(false);
    }
    public async Task<TContent> LoadReferenceAsync<TContent>(LoadReferenceRequest requestData, CancellationToken cancel) where TContent : Content
    {
        AssertLoadReferenceRequest(requestData);
        requestData.ContentId = this.Id;
        return await Repository.LoadReferenceAsync<TContent>(requestData, cancel).ConfigureAwait(false);
    }
    public Task<IContentCollection<Content>> LoadReferencesAsync(string fieldName, CancellationToken cancel)
    {
        return Repository.LoadReferencesAsync<Content>(CreateLoadReferenceRequest(fieldName), cancel);
    }
    public Task<IContentCollection<TContent>> LoadReferencesAsync<TContent>(string fieldName, CancellationToken cancel) where TContent : Content
    {
        return Repository.LoadReferencesAsync<TContent>(CreateLoadReferenceRequest(fieldName), cancel);
    }
    public async Task<IContentCollection<Content>> LoadReferencesAsync(LoadReferenceRequest requestData, CancellationToken cancel)
    {
        AssertLoadReferenceRequest(requestData);
        requestData.ContentId = this.Id;
        return await Repository.LoadReferencesAsync<Content>(requestData, cancel).ConfigureAwait(false);
    }
    public async Task<IContentCollection<TContent>> LoadReferencesAsync<TContent>(LoadReferenceRequest requestData, CancellationToken cancel) where TContent : Content
    {
        AssertLoadReferenceRequest(requestData);
        requestData.ContentId = this.Id;
        return await Repository.LoadReferencesAsync<TContent>(requestData, cancel).ConfigureAwait(false);
    }

    private LoadReferenceRequest CreateLoadReferenceRequest(string fieldName)
    {
        if(fieldName == null)
            throw new ArgumentNullException(nameof(fieldName));

        var result = new LoadReferenceRequest {FieldName = fieldName};
        if (Id > 0)
            result.ContentId = Id;
        else if (Path != null)
            result.Path = Path;
        else
            throw new ApplicationException("Cannot load references of unsaved content.");

        return result;
    }
    private void AssertLoadReferenceRequest(LoadReferenceRequest request)
    {
        if (request.ContentId != 0)
            throw new InvalidOperationException("Do not provide ContentId when load reference of a content instance.");
        if (request.Path != null)
            throw new InvalidOperationException("Do not provide Path when load reference of a content instance.");
        if (this.Id == 0 && this.Path == null)
            throw new InvalidOperationException("Cannot load references of unsaved content.");
    }

    #region Obsolete methods with build time error

    /// <summary>
    /// Executes a count-only query in a subfolder on the server.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="query">Content query text. If it is empty, the count of children will be returned.</param>
    /// <param name="server">Target server.</param>
    /// <returns>Count of result content.</returns>
    [Obsolete("Use GetContentCountAsync, QueryCountAsync or QueryCountForAdminAsync methods of the IRepository.", true)]
    public static Task<int> GetCountAsync(string path, string query, ServerContext? server = null) => throw new NotSupportedException();

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
    [Obsolete("Use QueryForAdminAsync or QueryForAdminAsync<T> methods of the IRepository.", true)]
    public static async Task<IEnumerable<Content>> QueryForAdminAsync(
        string queryText, string[]? select = null, string[]? expand = null, QuerySettings? settings = null, ServerContext? server = null)
        => throw new NotSupportedException();
    /// <summary>
    /// Executes a query on the server and returns results filtered and expanded 
    /// based on the provided parameters. 
    /// </summary>
    /// <param name="queryText">Content query text.</param>
    /// <param name="select">Fields to select.</param>
    /// <param name="expand">Fields to expand.</param>
    /// <param name="settings">Query settings.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use QueryAsync or QueryAsync<T> method of the IRepository.", true)]
    public static Task<IEnumerable<Content>> QueryAsync(string queryText, string[]? select = null,
        string[]? expand = null, QuerySettings? settings = null, ServerContext? server = null) => throw new NotSupportedException();

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
    [Obsolete("Use the UploadAsync method of IRepository.", true)]
    public static Task<Content> UploadAsync(string parentPath, string fileName, Stream stream,
        string? contentType = null, string? propertyName = null, ServerContext? server = null, 
        Action<int>? progressCallback = null) => throw new NotSupportedException();

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
    [Obsolete("Use the UploadAsync method of IRepository.", true)]
    public static Task<Content> UploadAsync(int parentId, string fileName, Stream stream,
        string? contentType = null, string? propertyName = null, ServerContext? server = null,
        Action<int>? progressCallback = null) => throw new NotSupportedException();


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
    [Obsolete("Use the UploadTextAsync method of IRepository.", true)]
    public static Task<Content> UploadTextAsync(string parentPath, string fileName, string fileText,
        CancellationToken cancellationToken, string? contentType = null, string? propertyName = null,
        ServerContext? server = null) => throw new NotSupportedException();

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
    [Obsolete("Use the UploadTextAsync method of IRepository.", true)]
    public static Task<Content> UploadTextAsync(int parentId, string fileName, string fileText,
        CancellationToken cancellationToken,
        string? contentType = null, string? propertyName = null, ServerContext? server = null) => throw new NotSupportedException();

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
    [Obsolete("Do not use this method anymore.", true)]
    public static async Task UploadBlobAsync(string parentPath, string contentName, long fileSize,
        Func<int, int, string, Task> blobCallback, string? contentType = null, string? fileName = null, 
        string? propertyName = null, ServerContext? server = null) => throw new NotSupportedException();

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
    [Obsolete("Do not use this method anymore.", true)]
    public static async Task UploadBlobAsync(int parentId, string contentName, long fileSize,
        Func<int, int, string, Task> blobCallback, string? contentType = null, string? fileName = null,
        string? propertyName = null, ServerContext? server = null) => throw new NotSupportedException();

    [Obsolete("Do not use this method anymore.", true)]
    private static async Task SaveAndFinalizeBlobInternalAsync(string initResponse, long fileSize,
        Func<int, int, string, Task> blobCallback, string? fileName = null,
        string? propertyName = null, ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Gets a blob storage token that identifies a binary in the storage.
    /// </summary>
    /// <param name="id">Content id.</param>
    /// <param name="version">Content version (e.g. V2.3D). If not provided, the highest version 
    /// accessible to the current user will be served.</param>
    /// <param name="propertyName">Binary field name. Default is Binary.</param>
    /// <param name="server">Target server.</param>
    /// <returns>A token that can be used with the Blob storage API.</returns>
    [Obsolete("Do not use this method anymore.", true)]
    public static async Task<string> GetBlobToken(int id, string? version = null, string? propertyName = null,
        ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Gets a blob storage token that identifies a binary in the storage.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="version">Content version (e.g. V2.3D). If not provided, the highest version 
    /// accessible to the current user will be served.</param>
    /// <param name="propertyName">Binary field name. Default is Binary.</param>
    /// <param name="server">Target server.</param>
    /// <returns>A token that can be used with the Blob storage API.</returns>
    [Obsolete("Do not use this method anymore.", true)]
    public static async Task<string> GetBlobToken(string path, string? version = null, string? propertyName = null,
        ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Deletes the content by Path.
    /// </summary>
    /// <param name="path">Path of the <see cref="Content"/> to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(string, bool, CancellationToken) method of the IRepository.", true)]
    public static async Task DeleteAsync(string path, bool permanent, CancellationToken cancellationToken,
        ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Deletes one or more content by Path.
    /// </summary>
    /// <param name="paths">Paths of the <see cref="Content"/> objects to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(string[], bool, CancellationToken) method of the IRepository.", true)]
    public static Task DeleteAsync(string[] paths, bool permanent, CancellationToken cancellationToken,
        ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Deletes the content by Id.
    /// </summary>
    /// <param name="id">Id of the <see cref="Content"/> to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(int, bool, CancellationToken) method of the IRepository.", true)]
    public static async Task DeleteAsync(int id, bool permanent, CancellationToken cancellationToken,
        ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Deletes one or more content by Id.
    /// </summary>
    /// <param name="ids">Paths of the <see cref="Content"/> objects to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(int[], bool, CancellationToken) method of the IRepository.", true)]
    public static Task DeleteAsync(int[] ids, bool permanent, CancellationToken cancellationToken,
        ServerContext? server = null) => throw new NotSupportedException();

    /// <summary>
    /// Deletes one or more content by Id or Path.
    /// </summary>
    /// <param name="idsOrPaths">One or more id or path of the <see cref="Content"/> objects to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    [Obsolete("Use DeleteContentAsync(object[], bool, CancellationToken) method of the IRepository.", true)]
    public static Task DeleteAsync(object[] idsOrPaths, bool permanent, CancellationToken cancellationToken,
        ServerContext? server = null) => throw new NotSupportedException();

    #endregion

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

        // add local field values to post data
        if (_fields != null)
        {
            var dict = postData as IDictionary<string, object>;

            foreach (var field in _fields)
            {
                if(field.Key == "Existing")
                    continue;
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

        if (_restCaller == null)
            throw new NotSupportedException();

        var responseContent = Existing
            ? (this.Id > 0
                ? await PatchContentAsync(this.Id, postData, cancel).ConfigureAwait(false)
                : await PatchContentAsync(this.Path, postData, cancel).ConfigureAwait(false))
            : await PostContentAsync(this.ParentPath, postData, cancel).ConfigureAwait(false);

        // reset local values
        InitializeFromResponse(responseContent);
    }
    public async Task ResetAsync(object initialData, CancellationToken cancel)
    {
        await PutContentAsync(this.Path, initialData, cancel).ConfigureAwait(false);
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

    private Task ExecuteSimpleAction(string actionName, object? postData, CancellationToken cancel)
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
    private async Task ExecuteSimpleAction(ODataRequest request, object? postData, CancellationToken cancel)
    {
        if (_restCaller == null)
            throw new NotSupportedException();

        request.PostData = postData;
        await Repository.GetResponseStringAsync(request, HttpMethod.Post, cancel).ConfigureAwait(false);
    }

    //----------------------------------------------------------------------------- Security

    /// <summary>
    /// Checks whether a user has the provided permissions on the content.
    /// </summary>
    /// <param name="permissions">Permission names to check.</param>
    /// <param name="user">The user who's permissions need to be checked. If it is not provided, the server checks the current user.</param>
    /// <param name="server">Target server.</param>
    [Obsolete("Use HasPermissionAsync(string[], string?) overload instead.", true)]
    public async Task<bool> HasPermissionAsync(string[] permissions, string? user = null, ServerContext? server = null)
    {
        return await SecurityManager.HasPermissionAsync(this.Id, permissions, user, Repository, CancellationToken.None).ConfigureAwait(false);
    }
    /// <summary>
    /// Checks whether a user has the provided permissions on the content.
    /// </summary>
    /// <param name="permissions">Permission names to check.</param>
    /// <param name="user">The user who's permissions need to be checked. If it is not provided, the server checks the current user.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation and wraps a boolean value.</returns>
    public Task<bool> HasPermissionAsync(string[] permissions, string? user, CancellationToken cancel)
    {
        return SecurityManager.HasPermissionAsync(this.Id, permissions, user, Repository, cancel);
    }

    /// <summary>
    /// Breaks permissions on the content.
    /// </summary>
    /// <param name="server">Target server.</param>
    [Obsolete("Use BreakInheritanceAsync(CancellationToken) overload instead.", true)]
    public Task BreakInheritanceAsync(ServerContext? server = null)
    {
        throw new NotSupportedException();
    }
    /// <summary>
    /// Breaks permissions on the content.
    /// </summary>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    public Task BreakInheritanceAsync(CancellationToken cancel)
    {
        return SecurityManager.BreakInheritanceAsync(this.Id, Repository, cancel);
    }
    /// <summary>
    /// Removes permission break on the content.
    /// </summary>
    /// <param name="server">Target server.</param>
    [Obsolete("Use UnbreakInheritanceAsync(CancellationToken) overload instead.", true)]
    public Task UnbreakInheritanceAsync(ServerContext? server = null)
    {
        throw new NotSupportedException();
    }
    /// <summary>
    /// Removes permission break on the content.
    /// </summary>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    public Task UnbreakInheritanceAsync(CancellationToken cancel)
    {
        return SecurityManager.UnbreakInheritanceAsync(this.Id, Repository, cancel);
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

        HttpMethod? method = null;
        object? postData = null;

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

        requestData.PostData = postData;

        if (_restCaller == null)
            throw new NotSupportedException();

        result = Repository.GetResponseJsonAsync(requestData, method ?? HttpMethod.Get, CancellationToken.None);

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