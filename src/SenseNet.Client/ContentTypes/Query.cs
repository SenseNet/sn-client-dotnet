using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

internal class SnQuery : Content
{
    public string Query { get; set; }
    //UNDONE: Implement QueryType property (Choice)
    //public Choice QueryType { get; set; }
    public string UiFilters { get; set; }

    public SnQuery(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}