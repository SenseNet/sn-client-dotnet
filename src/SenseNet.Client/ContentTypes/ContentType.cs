using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class ContentType : Content
{
    public Binary? Binary { get; set; }

    public ContentType(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}