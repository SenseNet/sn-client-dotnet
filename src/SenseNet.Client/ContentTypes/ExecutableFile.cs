using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

internal class ExecutableFile : File
{
    public ExecutableFile(IRestCaller restCaller, ILogger<File> logger) : base(restCaller, logger) { }
}