using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Client;

public interface IRestCaller
{
    Task<string> GetResponseStringAsync(Uri uri, ServerContext server, CancellationToken cancel,
        HttpMethod method = null, string jsonBody = null);
}
