using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class EmailTemplate : Content
{
    public string Subject { get; set; }
    public string Body { get; set; }

    public EmailTemplate(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}