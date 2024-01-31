using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class PortalRoot : Folder
{
    public PortalRoot(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}