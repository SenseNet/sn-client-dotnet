using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum SavedQueryType{ Public, Private }

internal class SnQuery : Content
{
    public string Query { get; set; }
    public SavedQueryType? QueryType { get; set; }
    public string UiFilters { get; set; }

    public SnQuery(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}