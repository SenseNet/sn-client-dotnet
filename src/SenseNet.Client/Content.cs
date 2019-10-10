using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using SenseNet.Client.Security;

namespace SenseNet.Client
{
    /// <summary>
    /// Central class for all content-related client operations. It contains predefined content 
    /// properties and can be extended with custom fields as it is a dynamic type.
    /// </summary>
    public class Content : DynamicObject
    {
        private IDictionary<string, object> _fields;

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

        //============================================================================= Technical properties

        private bool Existing { get; set; }

        /// <summary>
        /// The target server that this content belongs to.
        /// </summary>
        public ServerContext Server { get; }

        private dynamic _responseContent;

        //============================================================================= Constructors

        /// <summary>
        /// Internal constructor for client content.
        /// </summary>
        /// <param name="server">Target server. If null, the first one will be used from the configuration.</param>
        protected Content(ServerContext server)
        {
            Server = server ?? ClientContext.Current.Server;
            _fields = new Dictionary<string, object>();
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

        private void InitializeFromResponse(dynamic responseContent)
        {
            _responseContent = responseContent;
            _fields = new Dictionary<string, object>();

            var jo = _responseContent as JObject;

            // fill local properties from the response object
            if (jo != null)
            {
                if (jo.Properties().Any(p => p.Name == "Id"))
                    Id = _responseContent.Id;
                if (jo.Properties().Any(p => p.Name == "Path"))
                    Path = _responseContent.Path;
                if (jo.Properties().Any(p => p.Name == "Name"))
                    Name = _responseContent.Name;
            }

            Existing = true;
        }

        //============================================================================= Creators

        /// <summary>
        /// Creates a new in-memory local representation of an existing content without loading it from the server.
        /// </summary>
        /// <param name="id">Content id.</param>
        /// <param name="server">Target server.</param>
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
        public static T CreateNew<T>(string parentPath, string contentType, string name, string contentTemplate = null, ServerContext server = null) where T : Content
        {
            if (string.IsNullOrEmpty(parentPath))
                throw new ArgumentNullException(nameof(parentPath));
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));

            var ctor = typeof (T).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance, 
                null, new[] { typeof (ServerContext) }, null);

            dynamic dc = ctor.Invoke(new object[] { server }) as T;

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
        public static async Task<Content> LoadAsync(int id, ServerContext server = null)
        {
            return await RESTCaller.GetContentAsync(id, server);
        }
        /// <summary>
        /// Loads a content from the server.
        /// </summary>
        /// <param name="path">Content path.</param>
        /// <param name="server">Target server.</param>
        public static async Task<Content> LoadAsync(string path, ServerContext server = null)
        {
            return await RESTCaller.GetContentAsync(path, server);
        }
        /// <summary>
        /// Loads a content from the server. Use this method to specify a detailed 
        /// content request, for example wich fields you want to expand or select.
        /// </summary>
        /// <param name="requestData">Detailed information that will be sent as part of the request.</param>
        /// <param name="server">Target server.</param>
        public static async Task<Content> LoadAsync(ODataRequest requestData, ServerContext server = null)
        {
            return await RESTCaller.GetContentAsync(requestData, server);
        }

        /// <summary>
        /// Checks whether a content exists on the server with the provided path.
        /// </summary>
        /// <param name="path">Content path.</param>
        /// <param name="server">Target server.</param>
        public static async Task<bool> ExistsAsync(string path, ServerContext server = null)
        {
            var requestData = new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
                Path = path,
                Metadata = MetadataFormat.None,
                Select = new[] { "Id" }
            };

            var content = await RESTCaller.GetContentAsync(requestData);
            return content != null;
        }
        
        /// <summary>
        /// Loads children of a container.
        /// </summary>
        /// <param name="path">Path of the container.</param>
        /// <param name="server">Target server.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<Content>> LoadCollectionAsync(string path, ServerContext server = null)
        {
            return await RESTCaller.GetCollectionAsync(path, server);
        }
        /// <summary>
        /// Queries the server for content items using the provided request data.
        /// </summary>
        /// <param name="requestData">Detailed information that will be sent as part of the request.
        /// For example Top, Skip, Select, etc.</param>
        /// <param name="server">Target server.</param>
        public static async Task<IEnumerable<Content>> LoadCollectionAsync(ODataRequest requestData, ServerContext server = null)
        {
            return await RESTCaller.GetCollectionAsync(requestData, server);
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
            return await LoadReferencesAsync(null, id, fieldName, select, server);
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
            return await LoadReferencesAsync(path, 0, fieldName, select, server);
        }
        private static async Task<IEnumerable<Content>> LoadReferencesAsync(string path, int id, string fieldName, string[] select = null, ServerContext server = null)
        {
            if (select == null || select.Length == 0)
                select = new[] { "*" };
            var projection = new[] { "Id", "Path", "Type" };
            projection = projection.Union(select.Select(p => fieldName + "/" + p)).ToArray();

            var oreq = new ODataRequest
            {
                SiteUrl = ServerContext.GetUrl(server),
                Expand = new[] { fieldName },
                Select = projection,
                ContentId = id,
                Path = path
            };

            dynamic content = await Content.LoadAsync(oreq, server);

            // we assume that this is an array of content json objects
            var _itemToken = (JToken)content[fieldName];
            var items = new JArray();

            if (((JToken)_itemToken).Type == JTokenType.Array)
            {
                items = (JArray)_itemToken;
            }
            else if (((JToken)_itemToken).Type == JTokenType.Object)
            {
                items.Add((JObject)_itemToken);
            }

            return items.Select(c => CreateFromResponse(c, server));
        }

        /// <summary>
        /// Executes a count-only query in a subfolder on the server.
        /// </summary>
        /// <param name="path">Content path.</param>
        /// <param name="query">Content query text. If it is empty, the count of children will be returned.</param>
        /// <param name="server">Target server.</param>
        /// <returns>Count of result content.</returns>
        public static async Task<int> GetCountAsync(string path, string query, ServerContext server = null)
        {
            var request = new ODataRequest
            {
                SiteUrl = ServerContext.GetUrl(server),
                Path = path,
                IsCollectionRequest = true,
                CountOnly = true
            };

            if (!string.IsNullOrEmpty(query))
                request.Parameters.Add("query", query);

            return await RESTCaller.GetCountAsync(request, server);
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
        public static async Task<IEnumerable<Content>> QueryForAdminAsync(string queryText, string[] select = null, string[] expand = null, QuerySettings settings = null, ServerContext server = null)
        {
            if (settings == null)
                settings = new QuerySettings();
            settings.EnableAutofilters = FilterStatus.Disabled;
            settings.EnableLifespanFilter = FilterStatus.Disabled;

            return await QueryAsync(queryText, select, expand, settings, server);
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
        public static async Task<IEnumerable<Content>> QueryAsync(string queryText, string[] select = null, string[] expand = null, QuerySettings settings = null, ServerContext server = null)
        {
            if (settings == null)
                settings = QuerySettings.Default;

            var oreq = new ODataRequest
            {
                Path = "/Root",
                Select = select,
                Expand = expand,
                Top = settings.Top,
                Skip = settings.Skip,
                SiteUrl = ServerContext.GetUrl(server)
            };

            oreq.Parameters.Add("query", Uri.EscapeDataString(queryText));

            if (settings.EnableAutofilters != FilterStatus.Default)
                oreq.Parameters.Add("enableautofilters", settings.EnableAutofilters.ToString().ToLower());
            if (settings.EnableLifespanFilter != FilterStatus.Default)
                oreq.Parameters.Add("enablelifespanfilter", settings.EnableLifespanFilter.ToString().ToLower());

            return await Content.LoadCollectionAsync(oreq, server);
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

            return await RESTCaller.UploadAsync(stream, uploadData, parentPath, server, progressCallback);
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

            return await RESTCaller.UploadAsync(stream, uploadData, parentId, server, progressCallback);
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
                server);

            // call the common method that contains the part that is the same for all implementations
            await SaveAndFinalizeBlobInternalAsync(responseText, fileSize, blobCallback, fileName, propertyName, server);
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
                server);

            // call the common method that contains the part that is the same for all implementations
            await SaveAndFinalizeBlobInternalAsync(responseText, fileSize, blobCallback, fileName, propertyName, server);
        }
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
            await blobCallback(contentId, versionId, token);

            // send final request
            await RESTCaller.GetResponseStringAsync(contentId, "FinalizeBlobUpload", HttpMethod.Post,
                JsonHelper.Serialize(new
                {
                    token,
                    fullSize = fileSize,
                    fieldName = propertyName,
                    fileName
                }),
                server);
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
        public static async Task<string> GetBlobToken(int id, string version = null, string propertyName = null, ServerContext server = null)
        {
            var responseText = await RESTCaller.GetResponseStringAsync(id, "GetBinaryToken", HttpMethod.Post,
                JsonHelper.Serialize(new { version, fieldName = propertyName }), server);

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
        public static async Task<string> GetBlobToken(string path, string version = null, string propertyName = null, ServerContext server = null)
        {
            var responseText = await RESTCaller.GetResponseStringAsync(path, "GetBinaryToken", HttpMethod.Post,
                JsonHelper.Serialize(new { version, fieldName = propertyName }), server);

            var response = JsonHelper.Deserialize(responseText);

            return response.token;
        }

        //============================================================================= Instance API

        /// <summary>
        /// Saves the content to the server.
        /// </summary>
        public async Task SaveAsync()
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

            dynamic responseContent = Existing
                ? (this.Id > 0
                    ? await RESTCaller.PatchContentAsync(this.Id, postData, Server)
                    : await RESTCaller.PatchContentAsync(this.Path, postData, Server))
                : await RESTCaller.PostContentAsync(this.ParentPath, postData, Server);

            // reset local values
            InitializeFromResponse(responseContent);
        }

        /// <summary>
        /// Deletes the content.
        /// </summary>
        /// <param name="permanent">Delete the content permanently or into the Trash.</param>
        public async Task DeleteAsync(bool permanent = true)
        {
            var requestData = new ODataRequest()
            {
                SiteUrl = Server.Url,
                ContentId = this.Id,
                Path = this.Path,
                ActionName = "Delete"
            };

            await RESTCaller.GetResponseStringAsync(requestData.GetUri(), Server, HttpMethod.Post, JsonHelper.GetJsonPostModel(new
            {
                permanent
            }));
        }
        /// <summary>
        /// Moves the content to the target location.
        /// </summary>
        /// <param name="targetPath">Target path.</param>
        public async Task MoveToAsync(string targetPath)
        {
            var requestData = new ODataRequest()
            {
                SiteUrl = Server.Url,
                ContentId = this.Id,
                Path = this.Path,
                ActionName = "MoveTo"
            };

            await RESTCaller.GetResponseStringAsync(requestData.GetUri(), Server, HttpMethod.Post, JsonHelper.GetJsonPostModel(new 
            {
                targetPath 
            }));
        }
        /// <summary>
        /// Creates a copy of the content to the target location.
        /// </summary>
        /// <param name="targetPath">Target path.</param>
        public async Task CopyToAsync(string targetPath)
        {
            var requestData = new ODataRequest()
            {
                SiteUrl = Server.Url,
                ContentId = this.Id,
                Path = this.Path,
                ActionName = "CopyTo"
            };

            await RESTCaller.GetResponseStringAsync(requestData.GetUri(), Server, HttpMethod.Post, JsonHelper.GetJsonPostModel(new
            {
                targetPath
            }));
        }

        /// <summary>
        /// Locks the content for the current user.
        /// </summary>
        public async Task CheckOutAsync()
        {
            var requestData = new ODataRequest()
            {
                SiteUrl = Server.Url,
                ContentId = this.Id,
                Path = this.Path,
                ActionName = "CheckOut",
                Select = new[] {"Id"}
            };

            await RESTCaller.GetResponseStringAsync(requestData.GetUri(), Server, HttpMethod.Post);
        }
        /// <summary>
        /// Check in the content.
        /// </summary>
        public async Task CheckInAsync()
        {
            var requestData = new ODataRequest()
            {
                SiteUrl = Server.Url,
                ContentId = this.Id,
                Path = this.Path,
                ActionName = "CheckIn",
                Select = new[] { "Id" }
            };

            await RESTCaller.GetResponseStringAsync(requestData.GetUri(), Server, HttpMethod.Post);
        }
        /// <summary>
        /// Undo all modifications on the content since the last checkout operation.
        /// </summary>
        public async Task UndoCheckOutAsync()
        {
            var requestData = new ODataRequest
            {
                SiteUrl = Server.Url,
                ContentId = this.Id,
                Path = this.Path,
                ActionName = "UndoCheckOut",
                Select = new [] { "Id" }
            };

            await RESTCaller.GetResponseStringAsync(requestData.GetUri(), Server, HttpMethod.Post);
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
            return await SecurityManager.HasPermissionAsync(this.Id, permissions, user, server);
        }

        /// <summary>
        /// Breaks permissions on the content.
        /// </summary>
        /// <param name="server">Target server.</param>
        public async Task BreakInheritanceAsync(ServerContext server = null)
        {
            await SecurityManager.BreakInheritanceAsync(this.Id, server);
        }
        /// <summary>
        /// Removes permission break on the content.
        /// </summary>
        /// <param name="server">Target server.</param>
        public async Task UnbreakInheritanceAsync(ServerContext server = null)
        {
            await SecurityManager.UnbreakInheritanceAsync(this.Id, server);
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
            if (_fields == null)
                _fields = new Dictionary<string, object>();

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
            var requestData = new ODataRequest
            {
                SiteUrl = Server.Url,
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
                if (_fields == null)
                    _fields = new Dictionary<string, object>();

                _fields[fieldName] = value;
            }
        }
    }
}
