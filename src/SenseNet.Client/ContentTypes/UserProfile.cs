using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class UserProfile : Workspace
{
    User User { get; set; }

    public UserProfile(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}