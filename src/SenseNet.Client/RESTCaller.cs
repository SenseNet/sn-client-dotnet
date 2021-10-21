using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using Microsoft.Extensions.Logging;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.Client
{
    /// <summary>
    /// Predefined HTTP methods to be used in OData requests.
    /// </summary>
    public static class HttpMethods
    {
        /// <summary>
        /// PATCH method.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static readonly HttpMethod PATCH = new HttpMethod("PATCH");
        /// <summary>
        /// POST method.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static readonly HttpMethod POST = HttpMethod.Post;
    }

    /// <summary>
    /// Sends HTTP requests to the SenseNet OData REST API.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class RESTCaller
    {
        private static readonly string JsonContentMimeType = "application/json";

        //============================================================================= Static GET methods

        /// <summary>
        /// Loads a content from the server.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="server">Target server.</param>
        public static async Task<Content> GetContentAsync(int contentId, ServerContext server = null)
        {
            return await GetContentAsync(new ODataRequest(server)
            {
                ContentId = contentId
            },
            server)
            .ConfigureAwait(false);
        }
        /// <summary>
        /// Loads a content from the server.
        /// </summary>
        /// <param name="path">Content path.</param>
        /// <param name="server">Target server.</param>
        public static async Task<Content> GetContentAsync(string path, ServerContext server = null)
        {
            return await GetContentAsync(new ODataRequest(server)
            {
                Path = path
            },
            server)
            .ConfigureAwait(false);
        }
        /// <summary>
        /// Loads a content from the server.
        /// </summary>
        /// <param name="requestData">OData request parameters, for example select or expand.</param>
        /// <param name="server">Target server.</param>
        public static async Task<Content> GetContentAsync(ODataRequest requestData, ServerContext server = null)
        {
            // just to make sure
            requestData.IsCollectionRequest = false;

            var rs = await GetResponseStringAsync(requestData.GetUri(), server).ConfigureAwait(false);
            if (rs == null)
                return null;

            return Content.CreateFromResponse(JsonHelper.Deserialize(rs).d, server);
        }

        /// <summary>
        /// Loads children of a container.
        /// </summary>
        /// <param name="path">Content path.</param>
        /// <param name="server">Target server.</param>
        public static async Task<IEnumerable<Content>> GetCollectionAsync(string path, ServerContext server = null)
        {
            return await GetCollectionAsync(new ODataRequest(server)
            {
                Path = path,
                IsCollectionRequest = true
            }, 
            server)
            .ConfigureAwait(false);
        }
        /// <summary>
        /// Queries the server for content items using the provided request data.
        /// </summary>
        /// <param name="requestData">OData request parameters, for example select or expand.</param>
        /// <param name="server">Target server.</param>
        public static async Task<IEnumerable<Content>> GetCollectionAsync(ODataRequest requestData, ServerContext server = null)
        {
            requestData.SiteUrl = ServerContext.GetUrl(server);

            // just to make sure
            requestData.IsCollectionRequest = true;

            var rs = await GetResponseStringAsync(requestData.GetUri(), server).ConfigureAwait(false);
            var items = JsonHelper.Deserialize(rs).d.results as JArray;

            return items?.Select(c => Content.CreateFromResponse(c, server)) ?? new Content[0];
        }

        /// <summary>
        /// Executes a count-only query on the server.
        /// </summary>
        /// <param name="requestData">OData request parameters, most importantly the query.</param>
        /// <param name="server">Target server.</param>
        public static async Task<int> GetCountAsync(ODataRequest requestData, ServerContext server)
        {
            // just to make sure
            requestData.IsCollectionRequest = true;
            requestData.CountOnly = true;

            var rs = await GetResponseStringAsync(requestData.GetUri(), server).ConfigureAwait(false);
            int count;

            if (int.TryParse(rs, out count))
                return count;

            throw new ClientException(string.Format("Invalid count response. Request: {0}. Response: {1}", requestData.GetUri(), rs));
        }

        /// <summary>
        /// Gets the raw response of an OData single content request from the server.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="actionName">Action name.</param>
        /// <param name="method">HTTP method (SenseNet.Client.HttpMethods class has a few predefined methods).</param>
        /// <param name="body">Request body.</param>
        /// <param name="server">Target server.</param>
        /// <returns>Raw HTTP response.</returns>
        public static async Task<string> GetResponseStringAsync(int contentId, string actionName = null, HttpMethod method = null, string body = null, ServerContext server = null)
        {
            var requestData = new ODataRequest(server)
            {
                ContentId = contentId,
                ActionName = actionName
            };

            return await GetResponseStringAsync(requestData.GetUri(), server, method, body).ConfigureAwait(false);
        }
        /// <summary>
        /// Gets the raw response of an OData single content request from the server.
        /// </summary>
        /// <param name="path">Content path.</param>
        /// <param name="actionName">Action name.</param>
        /// <param name="method">HTTP method (SenseNet.Client.HttpMethods class has a few predefined methods).</param>
        /// <param name="body">Request body.</param>
        /// <param name="server">Target server.</param>
        /// <returns>Raw HTTP response.</returns>
        public static async Task<string> GetResponseStringAsync(string path, string actionName = null, HttpMethod method = null, string body = null, ServerContext server = null)
        {
            var requestData = new ODataRequest(server)
            {
                Path = path,
                ActionName = actionName
            };

            return await GetResponseStringAsync(requestData.GetUri(), server, method, body).ConfigureAwait(false);
        }
        /// <summary>
        /// Gets the raw response of an OData request from the server.
        /// </summary>
        /// <param name="requestData">OData request parameters, for example select or expand.</param>
        /// <param name="method">HTTP method (SenseNet.Client.HttpMethods class has a few predefined methods).</param>
        /// <param name="body">Request body.</param>
        /// <param name="server">Target server.</param>
        /// <returns>Raw HTTP response.</returns>
        public static async Task<string> GetResponseStringAsync(ODataRequest requestData, HttpMethod method = null,
            string body = null, ServerContext server = null)
        {
            return await GetResponseStringAsync(requestData.GetUri(), server, method, body).ConfigureAwait(false);
        }
        /// <summary>
        /// Gets the raw response of a general HTTP request from the server.
        /// </summary>
        /// <param name="uri">Request URI.</param>
        /// <param name="server">Target server.</param>
        /// <param name="method">HTTP method (SenseNet.Client.HttpMethods class has a few predefined methods).</param>
        /// <param name="jsonBody">Request body in JSON format.</param>
        /// <returns>Raw HTTP response.</returns>
        public static async Task<string> GetResponseStringAsync(Uri uri, ServerContext server = null,
            HttpMethod method = null, string jsonBody = null)
        {
            string result = null;

            SnTrace.Category(ClientContext.TraceCategory).Write("###>REQ: {0}", uri);
            //server?.Logger?.LogTrace($"Sending {method} request to {uri}");

            await ProcessWebResponseAsync(uri.ToString(), method, server,
                jsonBody != null ? new StringContent(jsonBody) : null,
                response =>
                {
                    if (response != null)
                        result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Gets the response of an OData request as a dynamic JSON object.
        /// </summary>
        /// <param name="requestData">OData request parameters, for example select or expand.</param>
        /// <param name="server">Target server.</param>
        /// <param name="method">Http method (e.g. Post). Default is Get.</param>
        /// <param name="postData">An object containing properties to be sent in the body. It will be serialized to JSON.</param>
        /// <returns>A dynamic JSON object deserialized from the response.</returns>
        public static async Task<dynamic> GetResponseJsonAsync(ODataRequest requestData, ServerContext server = null, HttpMethod method = null, object postData = null)
        {
            // it wouldn't work if we tried to post some data with the default GET verb
            if (postData != null && method == null)
                method = HttpMethod.Post;

            var rs = await GetResponseStringAsync(requestData.GetUri(), server, method, postData == null ? null : JsonHelper.Serialize(postData))
                .ConfigureAwait(false);

            try
            {
                return JsonHelper.Deserialize(rs);
            }
            catch (Exception)
            {
                throw new ClientException(string.Format("Invalid response. Request: {0}. Response: {1}", requestData.GetUri(), rs));
            }
        }

        #region OBSOLETE
        /// <summary>
        /// Assembles an http request that gets a stream from the portal containing binary data.
        /// Use this inside a using block to asynchronously get the response stream.
        /// Please catch WebExceptions and parse them using the GetClientExceptionAsync method.
        /// </summary>
        /// <param name="id">Content id.</param>
        /// <param name="version">Content version (e.g. V2.3D). If not provided, the highest version
        /// accessible to the current user will be served.</param>
        /// <param name="propertyName">Binary field name. Default is Binary.</param>
        /// <param name="server">Target server.</param>
        [Obsolete("Use GetStreamResponseAsync instead.")]
        public static HttpWebRequest GetStreamRequest(int id, string version = null, string propertyName = null, ServerContext server = null)
        {
            var url = $"{ServerContext.GetUrl(server)}/binaryhandler.ashx?nodeid={id}&propertyname={propertyName ?? "Binary"}";
            if (!string.IsNullOrEmpty(version))
                url += "&version=" + version;

            return GetRequest(url, server);
        }
        [Obsolete("Do not use this method anymore.", true)]
        private static HttpWebRequest GetRequest(string url, ServerContext server)
        {
            return GetRequest(new Uri(url), server);
        }
        [Obsolete("Do not use this method anymore.", true)]
        private static HttpWebRequest GetRequest(Uri uri, ServerContext server)
        {
            // WebRequest.Create returns HttpWebRequest only if the url
            // is an HTTP url. It may return FtpWebRequest also!
            var myRequest = (HttpWebRequest)WebRequest.Create(uri);

            myRequest.Timeout = -1;
            myRequest.KeepAlive = false;
            myRequest.ProtocolVersion = HttpVersion.Version10;

            SetAuthenticationForRequest(myRequest, server);

            return myRequest;
        }
        [Obsolete("Use this method with HttpRequestException type.")]
        public static async Task<ClientException> GetClientExceptionAsync(WebException ex, string requestUrl = null, HttpMethod method = null, string body = null)
        {
            var responseString = await ReadResponseStringAsync(ex.Response).ConfigureAwait(false);
            var exceptionData = GetExceptionData(responseString);

            var ce = new ClientException(exceptionData, ex)
            {
                Response = responseString
            };

            ce.Data["Url"] = requestUrl;
            ce.Data["Method"] = method?.Method ?? HttpMethod.Get.Method;
            ce.Data["Body"] = body;

            return ce;
        }
        #endregion

        public static Task GetStreamResponseAsync(int contentId, Action<HttpResponseMessage> responseProcessor, CancellationToken cancellationToken)
        {
            return GetStreamResponseAsync(contentId, null, responseProcessor, cancellationToken);
        }
        public static Task GetStreamResponseAsync(int contentId, string version, Action<HttpResponseMessage> responseProcessor, CancellationToken cancellationToken)
        {
            return GetStreamResponseAsync(contentId, version, null, responseProcessor, cancellationToken);
        }
        public static Task GetStreamResponseAsync(int contentId, string version, string propertyName, Action<HttpResponseMessage> responseProcessor, CancellationToken cancellationToken)
        {
            return GetStreamResponseAsync(contentId, version, propertyName, null, responseProcessor, cancellationToken);
        }
        public static async Task GetStreamResponseAsync(int contentId, string version, string propertyName, ServerContext server, Action<HttpResponseMessage> responseProcessor, CancellationToken cancellationToken)
        {
            if (server == null)
                server = ClientContext.Current.Server;
            var url = $"{ServerContext.GetUrl(server)}/binaryhandler.ashx?nodeid={contentId}&propertyname={propertyName ?? "Binary"}";
            if (!string.IsNullOrEmpty(version))
                url += "&version=" + version;

            await ProcessWebResponseAsync(url, HttpMethod.Get, server, responseProcessor, cancellationToken)
                .ConfigureAwait(false);
        }

        //============================================================================= Static POST methods

        /// <summary>
        /// Sends a POST OData request to the server containing the specified data.
        /// </summary>
        /// <param name="parentPath">Content Repository path to send the response to.</param>
        /// <param name="postData">A .NET object to serialize as post data.</param>
        /// <param name="server">Target server.</param>
        /// <returns>A deserialized dynamic JSON object parsed from the response.</returns>
        public static async Task<dynamic> PostContentAsync(string parentPath, object postData, ServerContext server = null)
        {
            return await PostContentInternalAsync(parentPath, postData, HttpMethod.Post, server).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PATCH OData request to the server containing the specified data.
        /// </summary>
        /// <param name="contentId">Content id</param>
        /// <param name="postData">A .NET object to serialize as post data.</param>
        /// <param name="server">Target server.</param>
        /// <returns>A deserialized dynamic JSON object parsed from the response.</returns>
        public static async Task<dynamic> PatchContentAsync(int contentId, object postData, ServerContext server = null)
        {
            return await PostContentInternalAsync(contentId, postData, HttpMethods.PATCH, server).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PATCH OData request to the server containing the specified data.
        /// </summary>
        /// <param name="path">Content path</param>
        /// <param name="postData">A .NET object to serialize as post data.</param>
        /// <param name="server">Target server.</param>
        /// <returns>A deserialized dynamic JSON object parsed from the response.</returns>
        public static async Task<dynamic> PatchContentAsync(string path, object postData, ServerContext server = null)
        {
            return await PostContentInternalAsync(path, postData, HttpMethods.PATCH, server).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PUT OData request to the server containing the specified data.
        /// </summary>
        /// <param name="path">Content path</param>
        /// <param name="postData">A .NET object to serialize as post data.</param>
        /// <param name="server">Target server.</param>
        /// <returns>A deserialized dynamic JSON object parsed from the response.</returns>
        public static async Task<dynamic> PutContentAsync(string path, object postData, ServerContext server = null)
        {
            return await PostContentInternalAsync(path, postData, HttpMethod.Put, server).ConfigureAwait(false);
        }

        private static async Task<dynamic> PostContentInternalAsync(string path, object postData, HttpMethod method, ServerContext server = null)
        {
            var reqData = new ODataRequest(server)
            {
                Path = path
            };

            var rs = await GetResponseStringAsync(reqData.GetUri(), server, method, JsonHelper.GetJsonPostModel(postData))
                .ConfigureAwait(false);

            return JsonHelper.Deserialize(rs).d;
        }
        private static async Task<dynamic> PostContentInternalAsync(int contentId, object postData, HttpMethod method, ServerContext server = null)
        {
            var reqData = new ODataRequest(server)
            {
                ContentId = contentId
            };

            var rs = await GetResponseStringAsync(reqData.GetUri(), server, method, JsonHelper.GetJsonPostModel(postData))
                .ConfigureAwait(false);

            return JsonHelper.Deserialize(rs).d;
        }

        //============================================================================= Upload API

        /// <summary>
        /// Uploads a file to the server into the provided container.
        /// </summary>
        /// <param name="binaryStream">File contents.</param>
        /// <param name="uploadData">Upload parameters.</param>
        /// <param name="parentId">Parent id.</param>
        /// <param name="server">Target server.</param>
        /// <param name="progressCallback">An optional callback method that is called after each chunk is uploaded to the server.</param>
        /// <returns>The uploaded file content returned at the end of the upload request.</returns>
        public static async Task<Content> UploadAsync(Stream binaryStream, UploadData uploadData, int parentId, ServerContext server = null, Action<int> progressCallback = null)
        {
            var requestData = new ODataRequest(server)
            {
                ActionName = "Upload",
                ContentId = parentId
            };

            return await UploadInternalAsync(binaryStream, uploadData, requestData, server, progressCallback).ConfigureAwait(false);
        }
        /// <summary>
        /// Uploads a file to the server into the provided container.
        /// </summary>
        /// <param name="binaryStream">File contents.</param>
        /// <param name="uploadData">Upload parameters.</param>
        /// <param name="parentPath">Parent path.</param>
        /// <param name="server">Target server.</param>
        /// <param name="progressCallback">An optional callback method that is called after each chunk is uploaded to the server.</param>
        /// <returns>The uploaded file content returned at the end of the upload request.</returns>
        public static async Task<Content> UploadAsync(Stream binaryStream, UploadData uploadData, string parentPath, ServerContext server = null, Action<int> progressCallback = null)
        {
            var requestData = new ODataRequest(server)
            {
                ActionName = "Upload",
                Path = parentPath
            };

            return await UploadInternalAsync(binaryStream, uploadData, requestData, server, progressCallback)
                .ConfigureAwait(false);
        }
        private static async Task<Content> UploadInternalAsync(Stream binaryStream, UploadData uploadData, ODataRequest requestData, ServerContext server = null, Action<int> progressCallback = null)
        {
            server ??= ClientContext.Current.Server;

            // force set values
            uploadData.UseChunk = binaryStream.Length > ClientContext.Current.ChunkSizeInBytes;
            if (uploadData.FileLength == 0)
                uploadData.FileLength = binaryStream.Length;

            requestData.Parameters.Add("create", "1");

            dynamic uploadedContent = null;

            // Get ChunkToken
            try
            {
                SnTrace.Category(ClientContext.TraceCategory).Write("###>REQ: {0}", requestData);

                server.Logger?.LogTrace($"Uploading initial data of {uploadData.FileName}.");

                var httpContent = new StringContent(uploadData.ToString());
                httpContent.Headers.ContentType = new MediaTypeHeaderValue(JsonContentMimeType);
                await ProcessWebResponseAsync(requestData.ToString(), HttpMethod.Post, server, httpContent,
                    response =>
                    {
                        uploadData.ChunkToken = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebException ex)
            {
                var ce = new ClientException("Error during binary upload.", ex)
                {
                    Data =
                    {
                        ["SiteUrl"] = requestData.SiteUrl,
                        ["Parent"] = requestData.ContentId != 0 ? requestData.ContentId.ToString() : requestData.Path,
                        ["FileName"] = uploadData.FileName,
                        ["ContentType"] = uploadData.ContentType
                    }
                };

                throw ce;
            }

            // Reuse previous request data, but remove unnecessary parameters
            requestData.Parameters.Remove("create");

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

            while ((bytesRead = await binaryStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                chunkCount++;
                var retryCount = 0;

                await Retrier.RetryAsync(10, 1000, async () =>
                {
                    retryCount++;
                    var retryText = retryCount > 1 ? $" (retry {retryCount})" : string.Empty;
                    server.Logger?.LogTrace($"Uploading chunk {chunkCount}{retryText}: {bytesRead} bytes of {uploadData.FileName}.");

                    // Prepare the current chunk request
                    using var httpContent = new MultipartFormDataContent(boundary);
                    foreach (var item in uploadFormData)
                        httpContent.Add(new StringContent(item.Value), item.Key);
                    httpContent.Headers.ContentDisposition = contentDispositionHeaderValue;

                    if (uploadData.UseChunk)
                        httpContent.Headers.ContentRange =
                            new ContentRangeHeaderValue(start, start + bytesRead - 1, binaryStream.Length);

                    // Add the chunk as a stream into the request content
                    var postedStream = new MemoryStream(buffer, 0, bytesRead);
                    httpContent.Add(new StreamContent(postedStream), "files[]", uploadData.FileName);

                    // Process
                    await ProcessWebResponseAsync(requestData.ToString(), HttpMethod.Post, server,
                        httpContent,
                        response =>
                        {
                            var rs = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            uploadedContent = JsonHelper.Deserialize(rs);
                        }, CancellationToken.None).ConfigureAwait(false);

                }, (i, exception) =>
                {
                    // choose the exceptions when we can retry the operation
                    return exception switch
                    {
                        null => true,
                        ClientException cex when 
                            (int)cex.StatusCode == 429 ||
                            cex.ErrorData?.ExceptionType == "NodeIsOutOfDateException"
                            => false,
                        _ => throw exception
                    };
                });

                start += bytesRead;

                // Notify the caller about every chunk that was uploaded successfully
                progressCallback?.Invoke(start);
            }

            if (uploadedContent == null)
                return null;

            int contentId = uploadedContent.Id;
            var content = Content.Create(contentId, server);

            content.Name = uploadedContent.Name;
            content.Path = uploadedContent.Url;

            return content;
        }

        /// <summary>
        /// Uploads a file to the server into the provided container.
        /// </summary>
        /// <param name="text">File contents.</param>
        /// <param name="uploadData">Upload parameters.</param>
        /// <param name="parentId">Parent id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="server">Target server.</param>
        /// <returns>The uploaded file content returned at the end of the upload request.</returns>
        public static async Task<Content> UploadTextAsync(string text, UploadData uploadData, int parentId,
            CancellationToken cancellationToken, ServerContext server = null)
        {
            var requestData = new ODataRequest(server)
            {
                ActionName = "Upload",
                ContentId = parentId
            };

            return await UploadTextInternalAsync(text, uploadData, requestData, cancellationToken, server).ConfigureAwait(false);
        }
        /// <summary>
        /// Uploads a file to the server into the provided container.
        /// </summary>
        /// <param name="text">File contents.</param>
        /// <param name="uploadData">Upload parameters.</param>
        /// <param name="parentPath">Parent path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="server">Target server.</param>
        /// <returns>The uploaded file content returned at the end of the upload request.</returns>
        public static async Task<Content> UploadTextAsync(string text, UploadData uploadData, string parentPath,
            CancellationToken cancellationToken, ServerContext server = null)
        {
            var requestData = new ODataRequest(server)
            {
                ActionName = "Upload",
                Path = parentPath
            };

            return await UploadTextInternalAsync(text, uploadData, requestData, cancellationToken, server).ConfigureAwait(false);
        }
        private static async Task<Content> UploadTextInternalAsync(string text, UploadData uploadData, ODataRequest requestData,
            CancellationToken cancellationToken, ServerContext server = null)
        {
            // force set values
            if(text.Length > ClientContext.Current.ChunkSizeInBytes)
                throw new InvalidOperationException($"Cannot upload a text that longer than the chunk size " +
                                                    $"({ClientContext.Current.ChunkSizeInBytes}).");
            if (uploadData.FileLength == 0)
                uploadData.FileLength = text.Length;
            uploadData.FileText = text;

            dynamic uploadedContent = null;

            var model = JsonHelper.GetJsonPostModel(uploadData.ToDictionary());
            var httpContent = new StringContent(model);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue(JsonContentMimeType);

            server?.Logger?.LogTrace($"Uploading text content to the {uploadData.PropertyName} field of {uploadData.FileName}");

            await ProcessWebResponseAsync(requestData.ToString(), HttpMethod.Post, server, httpContent,
                async response =>
                {
                    if (response != null)
                    {
                        var rs = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        uploadedContent = JsonHelper.Deserialize(rs);
                    }
                }, cancellationToken).ConfigureAwait(false);

            if (uploadedContent == null)
                return null;

            int contentId = uploadedContent.Id;
            var content = Content.Create(contentId);

            content.Name = uploadedContent.Name;
            content.Path = uploadedContent.Url;

            return content;
        }

        //============================================================================= Helper methods

        private static async Task<string> ReadResponseStringAsync(WebResponse response)
        {
            if (response == null)
                return string.Empty;

            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                    return string.Empty;

                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        private static void SetAuthenticationForRequest(WebRequest myReq, ServerContext server)
        {
            if (server == null)
                server = ClientContext.Current.Server;

            // use token authentication
            if (!string.IsNullOrEmpty(server.Authentication.AccessToken))
            {
                myReq.Headers.Add("Authorization", "Bearer " + server.Authentication.AccessToken);
                return;
            }

            if (string.IsNullOrEmpty(server.Username))
            {
                // use NTLM authentication
                myReq.Credentials = CredentialCache.DefaultCredentials;
            }
            else
            {
                // use basic authentication
                var usernamePassword = server.Username + ":" + server.Password;
                myReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(usernamePassword)));
            }
        }

        /* ================================================================================ LOW LEVEL API */

        public static Task ProcessWebResponseAsync(string url, HttpMethod method, ServerContext server,
            Action<HttpResponseMessage> responseProcessor, CancellationToken cancellationToken)
        {
            return ProcessWebResponseAsync(url, method ?? HttpMethod.Get, server, null,
                responseProcessor, cancellationToken);
        }
        public static async Task ProcessWebResponseAsync(string url, HttpMethod method, ServerContext server,
            HttpContent httpContent,
            Action<HttpResponseMessage> responseProcessor, CancellationToken cancellationToken)
        {
            if (method == null)
                method = httpContent == null ? HttpMethod.Get : HttpMethod.Post;

            await ProcessWebRequestResponseAsync(url, method, server,
                (handler, client, request) =>
                {
                    if (httpContent != null)
                        request.Content = httpContent;
                }
                , responseProcessor, cancellationToken).ConfigureAwait(false);
        }
        public static async Task ProcessWebRequestResponseAsync(string url, HttpMethod method, ServerContext server,
            Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor,
            Action<HttpResponseMessage> responseProcessor, CancellationToken cancellationToken)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            
            server ??= ClientContext.Current.Server;

            using var handler = new HttpClientHandler();

            if (server.IsTrusted)
                handler.ServerCertificateCustomValidationCallback =
                    server.ServerCertificateCustomValidationCallback
                    ?? ServerContext.DefaultServerCertificateCustomValidationCallback;

            using var client = new HttpClient(handler);
            using var request = new HttpRequestMessage(method, url);

            // this will close the connection instead of keeping it alive
            request.Version = HttpVersion.Version10;

            SetAuthenticationForRequest(handler, request, server);

            requestProcessor?.Invoke(handler, client, request);

            try
            {
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        if (method == HttpMethod.Post)
                            throw new ClientException("Content not found", HttpStatusCode.NotFound);

                        server.Logger?.LogTrace($"Error response {response.StatusCode} when sending a {method} request to {url}.");
                    }
                    else
                    {
                        // try parse error content as json
                        var exceptionData =
                            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var serverExceptionData = GetExceptionData(exceptionData);

                        server.Logger?.LogTrace($"Error response: {response.StatusCode} " +
                                                $"{serverExceptionData?.Message} {serverExceptionData?.ExceptionType}");

                        throw new ClientException(serverExceptionData, response.StatusCode);
                    }
                }

                responseProcessor(response);
            }
            catch (HttpRequestException ex)
            {
                throw await GetClientExceptionAsync(ex, url, method).ConfigureAwait(false);
            }
        }

        private static void SetAuthenticationForRequest(HttpClientHandler handler, HttpRequestMessage request, ServerContext server)
        {
            if (server == null)
                server = ClientContext.Current.Server;

            // use token authentication
            if (!string.IsNullOrEmpty(server.Authentication.AccessToken))
            {
                request.Headers.Add("Authorization", "Bearer " + server.Authentication.AccessToken);
                return;
            }

            if (string.IsNullOrEmpty(server.Username))
            {
                // use NTLM authentication
                handler.Credentials = CredentialCache.DefaultCredentials;
            }
            else
            {
                // use basic authentication
                var usernamePassword = server.Username + ":" + server.Password;
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(usernamePassword)));
            }
        }

        /// <summary>
        /// Parses an error response and wraps all the information in it into a ClientException.
        /// </summary>
        /// <param name="ex">Original web exception.</param>
        /// <param name="requestUrl">Request url that caused the web exception.</param>
        /// <param name="method">Http method (e.g GET or POST)</param>
        /// <param name="body">Request body.</param>
        /// <returns>A client exception that contains parsed server info (e.g. OData exception type,
        /// status code, original response text, etc.) and the original exception as an inner exception.</returns>
        public static Task<ClientException> GetClientExceptionAsync(HttpRequestException ex, string requestUrl = null, HttpMethod method = null, string body = null)
        {
            var ce = new ClientException("A request exception occurred.", ex)
            {
                Response = string.Empty,
                Data =
                {
                    ["Url"] = requestUrl,
                    ["Method"] = method?.Method ?? HttpMethod.Get.Method,
                    ["Body"] = body
                }
            };

            return Task.FromResult(ce);
        }

        private static ErrorData GetExceptionData(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
                return null;

            responseText = responseText.Trim();

            try
            {
                if (responseText.StartsWith("{"))
                {
                    // Try to deserialize the response as an object that contains
                    // well-formatted information about the error (e.g. type).
                    var er = JsonHelper.Deserialize<ErrorResponse>(responseText);
                    if (er != null)
                        return er.ErrorData;
                }
                else if (responseText.StartsWith("<"))
                {
                    // parse HTML
                    var context = BrowsingContext.New(AngleSharp.Configuration.Default);

                    // Create a virtual request to specify the document to load.
                    var htmlDocument = context.OpenAsync(req =>
                    {
                        req.Content(responseText)
                            .Header("charset", "UTF-8");
                    }).GetAwaiter().GetResult();

                    return new ErrorData
                    {
                        ErrorCode = string.Empty,
                        Message = new ErrorMessage
                        {
                            Value = htmlDocument.Title
                        }
                    };
                }
                else
                {
                    return new ErrorData
                    {
                        ErrorCode = string.Empty,
                        Message = new ErrorMessage
                        {
                            Value = responseText.Substring(0, Math.Min(500, responseText.Length - 1))
                        }
                    };
                }
            }
            catch (Exception)
            {
                // parsing error ignored
            }

            return null;
        }
    }
}
