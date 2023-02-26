using System.Threading.Tasks;
using System;
using System.Net.Http;

namespace SenseNet.Client;

public class DefaultRestCaller : IRestCaller
{
    public async Task<string> GetResponseStringAsync(Uri uri, ServerContext server, HttpMethod method = null, string jsonBody = null)
    {
        if (method == null)
            return await RESTCaller.GetResponseStringAsync(uri, server).ConfigureAwait(false);
        return await RESTCaller.GetResponseStringAsync(uri, server, method, jsonBody).ConfigureAwait(false);
    }
}