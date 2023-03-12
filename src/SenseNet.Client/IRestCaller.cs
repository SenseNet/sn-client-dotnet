﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Client;

public interface IRestCaller
{
    Task<string> GetResponseStringAsync(Uri uri, ServerContext server, CancellationToken cancel,
        HttpMethod method = null, string jsonBody = null);

    Task<dynamic> PostContentAsync(string parentPath, object postData, ServerContext server, CancellationToken cancel);

    Task<dynamic> PatchContentAsync(int contentId, object postData, ServerContext server, CancellationToken cancel);
    Task<dynamic> PatchContentAsync(string path, object postData, ServerContext server, CancellationToken cancel);
}
