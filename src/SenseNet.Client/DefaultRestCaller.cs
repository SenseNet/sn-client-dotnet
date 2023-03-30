using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Threading;
using SenseNet.Tools;

namespace SenseNet.Client;

public class DefaultRestCaller : IRestCaller
{
    private readonly IRetrier _retrier;

    public DefaultRestCaller(IRetrier retrier)
    {
        _retrier = retrier;
    }

    public Task<string> GetResponseStringAsync(Uri uri, ServerContext server, CancellationToken cancel,
        HttpMethod method = null, string jsonBody = null)
    {
        return _retrier.RetryAsync(
            () => method == null
                ? RESTCaller.GetResponseStringAsync(uri, server)
                : RESTCaller.GetResponseStringAsync(uri, server, method, jsonBody),
            shouldRetryOnError: (ex, _) => ex.ShouldRetry(),
            cancel: cancel);
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
}