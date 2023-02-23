using System.Threading.Tasks;
using System;

namespace SenseNet.Client;

public class DefaultRestCaller : IRestCaller
{
    public Task<string> GetResponseStringAsync(Uri uri, ServerContext server) => RESTCaller.GetResponseStringAsync(uri, server);
}