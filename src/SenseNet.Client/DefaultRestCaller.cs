using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Threading;
using SenseNet.Tools;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using AngleSharp;
using SenseNet.Diagnostics;
using static SenseNet.Client.Constants;

namespace SenseNet.Client;

public class DefaultRestCaller : IRestCaller
{
    private readonly IRetrier _retrier;
    private readonly IHttpClientFactory _httpClientFactory;

    public ServerContext Server { get; set; }

    public DefaultRestCaller(
        IRetrier retrier,
        IHttpClientFactory httpClientFactory
    )
    {
        _retrier = retrier;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetResponseStringAsync(Uri uri, HttpMethod method, string postData,
        Dictionary<string, IEnumerable<string>> additionalHeaders, CancellationToken cancel)
    {
        string result = null;
        await ProcessWebResponseAsync(uri.ToString(), method, additionalHeaders,
            postData != null ? new StringContent(postData) : null,
            // ReSharper disable once UnusedParameter.Local
            async (response, c) =>
            {
                cancel.ThrowIfCancellationRequested();
                if (response != null)
#if NET6_0_OR_GREATER
                    result = await response.Content.ReadAsStringAsync(c);
#else
                    result = await response.Content.ReadAsStringAsync();
#endif
            }, cancel).ConfigureAwait(false);

        return result;
    }

    public Task ProcessWebResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        HttpContent postData, Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
    {
        return ProcessWebRequestResponseAsync(relativeUrl, method, additionalHeaders,
            (_, _, request) =>
            {
                if (postData != null)
                    request.Content = postData;
            },
            responseProcessor, cancel);
    }

    public Task ProcessWebRequestResponseAsync(string relativeUrl, HttpMethod method, Dictionary<string, IEnumerable<string>> additionalHeaders,
        Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor,
        Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
    {
        return _retrier.RetryAsync(
            () => ProcessWebRequestResponsePrivateAsync(relativeUrl, method, Server,
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

    private async Task ProcessWebRequestResponsePrivateAsync(string url, HttpMethod method, ServerContext server,
    Action<HttpClientHandler, HttpClient, HttpRequestMessage> requestProcessor,
    Func<HttpResponseMessage, CancellationToken, Task> responseProcessor, CancellationToken cancel)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        server ??= ClientContext.Current.Server;

        using var handler = new HttpClientHandler();

        using var client = _httpClientFactory.CreateClient(server.IsTrusted
            ? HttpClientName.Trusted
            : HttpClientName.Untrusted);
        var requestUri = GetRealUri(url, server.Url);
        using var request = new HttpRequestMessage(method, requestUri);

        // this will close the connection instead of keeping it alive
        request.Version = HttpVersion.Version10;

        SetAuthenticationForRequest(handler, request, server);

        requestProcessor?.Invoke(handler, client, request);

        cancel.ThrowIfCancellationRequested();

        if (SnTrace.Test.Enabled)
        {
            var content = request.Content;
            var body = content == null ? "null" : await content.ReadAsStringAsync();
            var msg = $">>>> RAW REQUEST: {method} {url} | {body}";
            msg = msg.Replace("{", "{{").Replace("}", "}}");
            SnTrace.Test.Write(msg);
        }

        try
        {
            using var response = await client.SendAsync(request, cancel).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    if (method == HttpMethod.Post)
                        throw new ClientException("Content not found", HttpStatusCode.NotFound);

                    server.Logger?.LogTrace($"Error response {response.StatusCode} when sending a {method} request to {url}.");
                }
                else
                {
                    // try parse error content as json
                    var exceptionData =
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var serverExceptionData = GetExceptionData(exceptionData);

                    server.Logger?.LogTrace($"Error response: {response.StatusCode} " +
                                            $"{serverExceptionData?.Message?.Value} {serverExceptionData?.ExceptionType}");

                    throw new ClientException(serverExceptionData, response.StatusCode);
                }
            }

            await responseProcessor(response, cancel);
        }
        catch (HttpRequestException ex)
        {
            throw await GetClientExceptionAsync(ex, url, method).ConfigureAwait(false);
        }
    }

    private Uri GetRealUri(string url, string serverUrl)
    {
        var serverUri = new Uri(serverUrl);
        // Avoid UriFormatException: "Invalid URI: The hostname could not be parsed."
        if (url.StartsWith("/"))
            url = "host" + url;
        var result = new UriBuilder(url)
        {
            Scheme = serverUri.Scheme,
            Host = serverUri.Host,
            Port = serverUri.Port
        };
        return result.Uri;
    }

    private void SetAuthenticationForRequest(HttpClientHandler handler, HttpRequestMessage request, ServerContext server)
    {
        server ??= ClientContext.Current.Server;

        // use token authentication
        if (!string.IsNullOrEmpty(server.Authentication.AccessToken))
        {
            request.Headers.Add("Authorization", "Bearer " + server.Authentication.AccessToken);
            return;
        }

        // api key authentication
        if (!string.IsNullOrEmpty(server.Authentication.ApiKey))
        {
            request.Headers.Add("apikey", server.Authentication.ApiKey);
            return;
        }

        if (string.IsNullOrEmpty(server.Username))
        {
            // use NTLM authentication
            handler.Credentials = CredentialCache.DefaultCredentials;
        }
        else
        {
            // use basic authentication
            var usernamePassword = server.Username + ":" + server.Password;
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(usernamePassword)));
        }
    }
    public Task<ClientException> GetClientExceptionAsync(HttpRequestException ex, string requestUrl = null, HttpMethod method = null, string body = null)
    {
        var ce = new ClientException("A request exception occurred.", ex)
        {
            Response = string.Empty,
            Data =
                {
                    ["Url"] = requestUrl,
                    ["Method"] = method?.Method ?? HttpMethod.Get.Method,
                    ["Body"] = body
                }
        };

        return Task.FromResult(ce);
    }
    private ErrorData GetExceptionData(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
            return null;

        responseText = responseText.Trim();

        try
        {
            if (responseText.StartsWith("{"))
            {
                // Try to deserialize the response as an object that contains
                // well-formatted information about the error (e.g. type).
                var er = JsonHelper.Deserialize<ErrorResponse>(responseText);
                if (er != null)
                    return er.ErrorData;
            }
            else if (responseText.StartsWith("<"))
            {
                // parse HTML
                var context = BrowsingContext.New(AngleSharp.Configuration.Default);

                // Create a virtual request to specify the document to load.
                var htmlDocument = context.OpenAsync(req =>
                {
                    req.Content(responseText)
                        .Header("charset", "UTF-8");
                }).GetAwaiter().GetResult();

                return new ErrorData
                {
                    ErrorCode = string.Empty,
                    Message = new ErrorMessage
                    {
                        Value = htmlDocument.Title
                    }
                };
            }
            else
            {
                return new ErrorData
                {
                    ErrorCode = string.Empty,
                    Message = new ErrorMessage
                    {
                        Value = responseText.Substring(0, Math.Min(500, responseText.Length - 1))
                    }
                };
            }
        }
        catch (Exception)
        {
            // parsing error ignored
        }

        return null;
    }

}