using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Client;

public interface IRestCaller
{
    //UNDONE: OBSOLETE true
    [Obsolete("####", false)]
    Task<dynamic> PostContentAsync(string parentPath, object postData, ServerContext server, CancellationToken cancel);
    //UNDONE: OBSOLETE true
    [Obsolete("####", false)]
    Task<dynamic> PatchContentAsync(int contentId, object postData, ServerContext server, CancellationToken cancel);
    //UNDONE: OBSOLETE true
    [Obsolete("####", false)]
    Task<dynamic> PatchContentAsync(string path, object postData, ServerContext server, CancellationToken cancel);




    Task<string> GetResponseStringAsync(Uri uri, HttpMethod method, string postData,
        Dictionary<string, IEnumerable<string>> additionalHeaders, ServerContext server, CancellationToken cancel);

    Task ProcessWebResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        HttpContent postData,
        Action<HttpResponseMessage> responseProcessor, ServerContext server,
        CancellationToken cancel);

    Task ProcessWebRequestResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor,
        Action<HttpResponseMessage> responseProcessor, ServerContext server,
        CancellationToken cancel);

}
