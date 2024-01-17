using System;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    public class WebHookSubscription : Content
    {
        //UNDONE: Implement WebHookHttpMethod property (Choice)
        //public Choice WebHookHttpMethod
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
}
