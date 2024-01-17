using System;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum WebHookHttpMethod { Get, Post, Patch, Put, Delete }

public class WebHookSubscription : Content
{
    //UNDONE: missing TryConvert*
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