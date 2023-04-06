using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Client;

public interface IRestCaller
{
    public ServerContext Server { get; set; }

    Task<string> GetResponseStringAsync(Uri uri, HttpMethod method, string postData,
        Dictionary<string, IEnumerable<string>> additionalHeaders, CancellationToken cancel);

    Task ProcessWebResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        HttpContent postData,
        Action<HttpResponseMessage> responseProcessor, CancellationToken cancel);

    //UNDONE: responseProcessor and requestProcessor(?) should be async.
    Task ProcessWebRequestResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor,
        Action<HttpResponseMessage> responseProcessor, CancellationToken cancel);
}
