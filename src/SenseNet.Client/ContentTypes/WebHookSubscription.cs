using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum WebHookHttpMethod
{
    [JsonProperty("GET")]    Get,
    [JsonProperty("POST")]   Post,
    [JsonProperty("PATCH")]  Patch,
    [JsonProperty("PUT")]    Put,
    [JsonProperty("DELETE")] Delete
}

public class WebHookSubscription : Content
{
    public WebHookHttpMethod? WebHookHttpMethod { get; set; }
    public string WebHookUrl { get; set; }
    public string WebHookFilter { get; set; }
    public string WebHookHeaders { get; set; }
    public bool? Enabled { get; set; }
    public bool? IsValid { get; set; }
    public string InvalidFields { get; set; }
    public int? SuccessfulCalls { get; set; }
    public string WebHookPayload { get; set; }

    public WebHookSubscription(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}