using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum SavedQueryType{ Public, Private }

internal class SnQuery : Content
{
    public string Query { get; set; }
    //UNDONE: missing TryConvert*
    public SavedQueryType QueryType { get; set; }
    public string UiFilters { get; set; }

    public SnQuery(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}