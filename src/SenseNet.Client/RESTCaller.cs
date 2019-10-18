using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

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
        public static readonly HttpMethod PATCH = new HttpMethod("PATCH");
        /// <summary>
        /// POST method.
        /// </summary>
        public static readonly HttpMethod POST = HttpMethod.Post;
    }

    /// <summary>
    /// Sends HTTP requests to the SenseNet OData REST API.
    /// </summary>
    public static class RESTCaller
    {
        // Retry feature is switched OFF. If it is needed, implement a configuration for this.
        private static int REQUEST_RETRY_COUNT = 1;
        private static readonly string UPLOAD_CONTENTTYPE = "application/x-www-form-urlencoded";
        private static readonly string UPLOAD_FORMDATA_TEMPLATE = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
        private static readonly string UPLOAD_HEADER_TEMPLATE = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

        //============================================================================= Static GET methods

        /// <summary>
        /// Loads a content from the server.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="server">Target server.</param>
        public static async Task<Content> GetContentAsync(int contentId, ServerContext server = null)
        {
            return await GetContentAsync(new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
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
            return await GetContentAsync(new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
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
            return await GetCollectionAsync(new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
                Path = path,
                IsCollectionRequest = true
            })
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
            var requestData = new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
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
            var requestData = new ODataRequest
            {
                SiteUrl = ServerContext.GetUrl(server),
                Path = path,
                ActionName = actionName
            };

            return await GetResponseStringAsync(requestData.GetUri(), server, method, body).ConfigureAwait(false);
        }
        /// <summary>
        /// Gets the raw response of a general HTTP request from the server.
        /// </summary>
        /// <param name="uri">Request URI.</param>
        /// <param name="server">Target server.</param>
        /// <param name="method">HTTP method (SenseNet.Client.HttpMethods class has a few predefined methods).</param>
        /// <param name="body">Request body.</param>
        /// <returns>Raw HTTP response.</returns>
        public static async Task<string> GetResponseStringAsync(Uri uri, ServerContext server = null, HttpMethod method = null, string body = null)
        {
            var retryCount = 0;

            while (retryCount < REQUEST_RETRY_COUNT)
            {
                SnTrace.Category(ClientContext.TraceCategory).Write("###>REQ: {0}", uri);

                var myRequest = GetRequest(uri, server);

                if (method != null)
                    myRequest.Method = method.Method;

                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        using (var requestWriter = new StreamWriter(myRequest.GetRequestStream()))
                        {
                            requestWriter.Write(body);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ClientException("Error during writing to request stream: " + ex.Message, ex);
                    }
                }
                else
                {
                    myRequest.ContentLength = 0;
                }

                try
                {
                    using (var wr = await myRequest.GetResponseAsync())
                    {
                        return await ReadResponseStringAsync(wr).ConfigureAwait(false);
                    }
                }
                catch (WebException ex)
                {
                    // a 404 result is not an error in case of simple get requests, so return silently
                    if (ex.Response is HttpWebResponse webResponse && webResponse.StatusCode == HttpStatusCode.NotFound && (method == null || method != HttpMethod.Post))
                        return null;

                    if (retryCount >= REQUEST_RETRY_COUNT - 1)
                    {
                        throw await GetClientExceptionAsync(ex, uri.ToString(), method, body).ConfigureAwait(false);
                    }
                    else
                    {
                        var responseString = await ReadResponseStringAsync(ex.Response).ConfigureAwait(false);

                        Thread.Sleep(50);

                        SnTrace.Category(ClientContext.TraceCategory).Write("###>REQ: {0} ERROR:{1}", uri,
                            responseString.Replace(Environment.NewLine, " ").Replace("\r\n", " ") + " " + ex);
                    }
                }

                retryCount++;
            }

            return string.Empty;
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

            var rs = await GetResponseStringAsync(requestData.GetUri(), server, method, postData == null ? null : JsonHelper.Serialize(postData)).ConfigureAwait(false);

            try
            {
                return JsonHelper.Deserialize(rs);
            }
            catch (Exception)
            {
                throw new ClientException(string.Format("Invalid response. Request: {0}. Response: {1}", requestData.GetUri(), rs));
            }
        }

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
        public static HttpWebRequest GetStreamRequest(int id, string version = null, string propertyName = null, ServerContext server = null)
        {
            var url = $"{ServerContext.GetUrl(server)}/binaryhandler.ashx?nodeid={id}&propertyname={propertyName ?? "Binary"}";
            if (!string.IsNullOrEmpty(version))
                url += "&version=" + version;

            return GetRequest(url, server);
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
            var reqData = new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
                Path = path
            };

            var rs = await GetResponseStringAsync(reqData.GetUri(), server, method, JsonHelper.GetJsonPostModel(postData)).ConfigureAwait(false);

            return JsonHelper.Deserialize(rs).d;
        }
        private static async Task<dynamic> PostContentInternalAsync(int contentId, object postData, HttpMethod method, ServerContext server = null)
        {
            var reqData = new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
                ContentId = contentId
            };

            var rs = await GetResponseStringAsync(reqData.GetUri(), server, method, JsonHelper.GetJsonPostModel(postData)).ConfigureAwait(false);

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
            var requestData = new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
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
            var requestData = new ODataRequest()
            {
                SiteUrl = ServerContext.GetUrl(server),
                ActionName = "Upload",
                Path = parentPath
            };

            return await UploadInternalAsync(binaryStream, uploadData, requestData, server, progressCallback).ConfigureAwait(false);
        }
        private static async Task<Content> UploadInternalAsync(Stream binaryStream, UploadData uploadData, ODataRequest requestData, ServerContext server = null, Action<int> progressCallback = null)
        {
            // force set values
            uploadData.UseChunk = binaryStream.Length > ClientContext.Current.ChunkSizeInBytes;
            if (uploadData.FileLength == 0)
                uploadData.FileLength = binaryStream.Length;

            requestData.Parameters["create"] = "1";

            dynamic uploadedContent = null;
            var retryCount = 0;

            // send initial request
            while (retryCount < REQUEST_RETRY_COUNT)
            {
                try
                {
                    var myReq = CreateInitUploadWebRequest(requestData.ToString(), server, uploadData);

                    SnTrace.Category(ClientContext.TraceCategory).Write("###>REQ: {0}", myReq.RequestUri);

                    using (var wr = await myReq.GetResponseAsync())
                    {
                        uploadData.ChunkToken = await ReadResponseStringAsync(wr).ConfigureAwait(false);
                    }

                    // succesful request: skip out from retry loop
                    break;
                }
                catch (WebException ex)
                {
                    if (retryCount >= REQUEST_RETRY_COUNT - 1)
                    {
                        var ce = new ClientException("Error during binary upload.", ex);

                        ce.Data["SiteUrl"] = requestData.SiteUrl;
                        ce.Data["Parent"] = requestData.ContentId != 0 ? requestData.ContentId.ToString() : requestData.Path;
                        ce.Data["FileName"] = uploadData.FileName;
                        ce.Data["ContentType"] = uploadData.ContentType;

                        throw ce;
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }

                retryCount++;
            }

            var boundary = "---------------------------" + DateTime.UtcNow.Ticks.ToString("x");
            var trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            // send subsequent requests
            var buffer = new byte[ClientContext.Current.ChunkSizeInBytes];
            int bytesRead;
            var start = 0;

            // reuse previous request data, but remove unnecessary parameters
            requestData.Parameters.Remove("create");

            while ((bytesRead = binaryStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                retryCount = 0;

                //get the request object for the actual chunk
                while (retryCount < REQUEST_RETRY_COUNT)
                {
                    Stream requestStream = null;
                    HttpWebRequest chunkRequest;

                    try
                    {
                        chunkRequest = CreateChunkUploadWebRequest(requestData.ToString(), server, uploadData, boundary, out requestStream);

                        SnTrace.Category(ClientContext.TraceCategory).Write("###>REQ: {0}", chunkRequest.RequestUri);

                        if (uploadData.UseChunk)
                            chunkRequest.Headers.Set("Content-Range", string.Format("bytes {0}-{1}/{2}", start, start + bytesRead - 1, binaryStream.Length));

                        //write the chunk into the request stream
                        requestStream.Write(buffer, 0, bytesRead);
                        requestStream.Write(trailer, 0, trailer.Length);

                        await requestStream.FlushAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        if (requestStream != null)
                            requestStream.Close();
                    }

                    //send the request
                    try
                    {
                        using (var wr = await chunkRequest.GetResponseAsync())
                        {
                            var rs = await ReadResponseStringAsync(wr).ConfigureAwait(false);

                            uploadedContent = JsonHelper.Deserialize(rs);
                        }

                        // successful request: skip out from the retry loop
                        break;
                    }
                    catch (WebException ex)
                    {
                        if (retryCount >= REQUEST_RETRY_COUNT - 1)
                        {
                            var ce = new ClientException("Error during binary upload.", ex);

                            ce.Data["SiteUrl"] = requestData.SiteUrl;
                            ce.Data["Parent"] = requestData.ContentId != 0 ? requestData.ContentId.ToString() : requestData.Path;
                            ce.Data["FileName"] = uploadData.FileName;
                            ce.Data["ContentType"] = uploadData.ContentType;

                            throw ce;
                        }
                        else
                        {
                            Thread.Sleep(50);
                        }
                    }

                    retryCount++;
                }

                start += bytesRead;

                // notify the caller about every chunk that was uploaded successfully
                if (progressCallback != null)
                    progressCallback(start);
            }

            if (uploadedContent == null)
                return null;

            int contentId = uploadedContent.Id;
            var content = Content.Create(contentId);

            content.Name = uploadedContent.Name;
            content.Path = uploadedContent.Url;

            return content;
        }

        //============================================================================= Helper methods

        /// <summary>
        /// Parses an error response and wraps all the information in it into a ClientException.
        /// </summary>
        /// <param name="ex">Original web exception.</param>
        /// <param name="requestUrl">Request url that caused the web exception.</param>
        /// <param name="method">Http method (e.g GET or POST)</param>
        /// <param name="body">Request body.</param>
        /// <returns>A client exception that contains parsed server info (e.g. OData exception type,
        /// status code, original response text, etc.) and the original exception as an inner exception.</returns>
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

        private static WebRequest CreateInitUploadWebRequest(string url, ServerContext server, UploadData uploadData)
        {
            var myRequest = GetRequest(url, server);
            myRequest.Method = "POST";

            var postDataBytes = Encoding.UTF8.GetBytes(uploadData.ToString());

            myRequest.ContentLength = postDataBytes.Length;
            myRequest.ContentType = UPLOAD_CONTENTTYPE;

            using (var reqStream = myRequest.GetRequestStream())
            {
                reqStream.Write(postDataBytes, 0, postDataBytes.Length);
            }

            return myRequest;
        }
        private static HttpWebRequest CreateChunkUploadWebRequest(string url, ServerContext server, UploadData uploadData, string boundary, out Stream requestStream)
        {
            var myRequest = GetRequest(url, server);

            myRequest.Method = "POST";
            myRequest.ContentType = "multipart/form-data; boundary=" + boundary;

            myRequest.Headers.Add("Content-Disposition", "attachment; filename=\"" + Uri.EscapeUriString(uploadData.FileName) + "\"");

            var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            // we must not close the stream after this as we need to write the chunk into it in the caller method
            requestStream = myRequest.GetRequestStream();

            //write form data values
            foreach (var kvp in uploadData.ToDictionary())
            {
                requestStream.Write(boundarybytes, 0, boundarybytes.Length);

                var formitem = string.Format(UPLOAD_FORMDATA_TEMPLATE, kvp.Key, kvp.Value);
                var formitembytes = Encoding.UTF8.GetBytes(formitem);

                requestStream.Write(formitembytes, 0, formitembytes.Length);
            }

            //write a boundary
            requestStream.Write(boundarybytes, 0, boundarybytes.Length);

            //write file name and content type
            var header = string.Format(UPLOAD_HEADER_TEMPLATE, "files[]", Uri.EscapeUriString(uploadData.FileName));
            var headerbytes = Encoding.UTF8.GetBytes(header);

            requestStream.Write(headerbytes, 0, headerbytes.Length);

            return myRequest;
        }

        private static HttpWebRequest GetRequest(string url, ServerContext server)
        {
            return GetRequest(new Uri(url), server);
        }
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

        private static ErrorData GetExceptionData(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
                return null;

            try
            {
                // Try to deserialize the response as an object that contains
                // well-formatted information about the error (e.g. type).
                var er = JsonHelper.Deserialize<ErrorResponse>(responseText);
                if (er != null)
                    return er.ErrorData;
            }
            catch (Exception)
            {
                // parsing error ignored
            }

            return null;
        }
    }
}
