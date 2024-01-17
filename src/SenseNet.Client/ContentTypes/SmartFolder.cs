using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class SmartFolder : Folder
{
    private string Query { get; set; }
    FilterStatus? EnableAutofilters { get; set; }
    FilterStatus? EnableLifespanFilter { get; set; }

    public SmartFolder(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}