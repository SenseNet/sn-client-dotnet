using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class ContentLink : Content
{
    public Content? Link { get; set; }

    public ContentLink(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}