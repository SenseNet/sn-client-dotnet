using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SenseNet.Client;

public interface IRestCaller
{
    Task<string> GetResponseStringAsync(Uri uri, ServerContext server, HttpMethod method = null, string jsonBody = null);
}
