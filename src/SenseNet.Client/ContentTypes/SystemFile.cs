using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class SystemFile : File
{
    public SystemFile(IRestCaller restCaller, ILogger<File> logger) : base(restCaller, logger) { }
}