using System;
using System.Threading.Tasks;

namespace SenseNet.Client;

public interface IRestCaller
{
    Task<string> GetResponseStringAsync(Uri uri, ServerContext server);
}
