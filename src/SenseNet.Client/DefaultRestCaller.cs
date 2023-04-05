using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Threading;
using SenseNet.Tools;
using System.Collections.Generic;

namespace SenseNet.Client;

public class DefaultRestCaller : IRestCaller
{
    private readonly IRetrier _retrier;

    public DefaultRestCaller(IRetrier retrier)
    {
        _retrier = retrier;
    }

    public Task<dynamic> PostContentAsync(string parentPath, object postData, ServerContext server, CancellationToken cancel)
    {
        return _retrier.RetryAsync(
            () => RESTCaller.PostContentAsync(parentPath, postData, server),
            shouldRetryOnError: (ex, _) => ex.ShouldRetry(),
            cancel: cancel);
    }

    public Task<dynamic> PatchContentAsync(int contentId, object postData, ServerContext server, CancellationToken cancel)
    {
        return _retrier.RetryAsync(
            () => RESTCaller.PatchContentAsync(contentId, postData, server),
            shouldRetryOnError: (ex, _) => ex.ShouldRetry(),
            cancel: cancel);
    }

    public Task<dynamic> PatchContentAsync(string path, object postData, ServerContext server, CancellationToken cancel)
    {
        return _retrier.RetryAsync(
            () => RESTCaller.PatchContentAsync(path, postData, server),
            shouldRetryOnError: (ex, _) => ex.ShouldRetry(),
            cancel: cancel);
    }






    public async Task<string> GetResponseStringAsync(Uri uri, HttpMethod method, string postData,
        Dictionary<string, IEnumerable<string>> additionalHeaders, ServerContext server, CancellationToken cancel)
    {
        string result = null;
        await ProcessWebResponseAsync(uri.ToString(), method, additionalHeaders,
            postData != null ? new StringContent(postData) : null,
            response =>
            {
                if (response != null)
                    result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }, server, cancel).ConfigureAwait(false);

        return result;
    }

    public Task ProcessWebResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        HttpContent postData, Action<HttpResponseMessage> responseProcessor,
        ServerContext server, CancellationToken cancel)
    {
        return _retrier.RetryAsync(
            () => ProcessWebRequestResponseAsync(relativeUrl, method, additionalHeaders,
                (handler, client, request) =>
                {
                    if (postData != null)
                        request.Content = postData;
                },
                responseProcessor, server, cancel),
            shouldRetryOnError: (ex, _) => ex.ShouldRetry(),
            cancel: cancel);
    }

    public Task ProcessWebRequestResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor, Action<HttpResponseMessage> responseProcessor,
        ServerContext server, CancellationToken cancel)
    {
        //UNDONE: relative url
        return _retrier.RetryAsync(
            () => RESTCaller.ProcessWebRequestResponseAsync(relativeUrl, method, server,
                (handler, client, request) =>
                {
                    if (additionalHeaders != null)
                        foreach (var header in additionalHeaders)
                            request.Headers.Add(header.Key, header.Value);
                    requestProcessor?.Invoke(handler, client, request);
                }, responseProcessor, cancel),
            shouldRetryOnError: (ex, _) => ex.ShouldRetry(),
            cancel: cancel);
    }
}