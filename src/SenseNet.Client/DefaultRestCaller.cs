using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Threading;

namespace SenseNet.Client;

public class DefaultRestCaller : IRestCaller
{
    public async Task<string> GetResponseStringAsync(Uri uri, ServerContext server, CancellationToken cancel,
        HttpMethod method = null, string jsonBody = null)
    {
        if (method == null)
            return await RESTCaller.GetResponseStringAsync(uri, server).ConfigureAwait(false);
        return await RESTCaller.GetResponseStringAsync(uri, server, method, jsonBody).ConfigureAwait(false);
    }

    public Task<dynamic> PostContentAsync(string parentPath, object postData, ServerContext server, CancellationToken cancel)
    {
        return RESTCaller.PostContentAsync(parentPath, postData, server);
    }

    public Task<dynamic> PatchContentAsync(int contentId, object postData, ServerContext server, CancellationToken cancel)
    {
        return RESTCaller.PatchContentAsync(contentId, postData, server);
    }

    public Task<dynamic> PatchContentAsync(string path, object postData, ServerContext server, CancellationToken cancel)
    {
        return RESTCaller.PatchContentAsync(path, postData, server);
    }
}