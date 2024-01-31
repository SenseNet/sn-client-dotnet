using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class Resource : SystemFile
{
    public Resource(IRestCaller restCaller, ILogger<File> logger) : base(restCaller, logger) { }
}