using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class SystemFolder : Folder
{
    public SystemFolder(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}