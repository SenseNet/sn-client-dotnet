using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class Link : ListItem
{
    public string Url { get; set; }

    public Link(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}