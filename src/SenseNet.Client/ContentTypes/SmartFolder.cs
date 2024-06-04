using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class SmartFolder : Folder
{
    public string? Query { get; set; }
    public FilterStatus? EnableAutofilters { get; set; }
    public FilterStatus? EnableLifespanFilter { get; set; }

    public SmartFolder(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}