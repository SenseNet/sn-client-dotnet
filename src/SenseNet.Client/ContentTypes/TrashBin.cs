using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class TrashBin : Workspace
{
    public int? MinRetentionTime { get; set; }
    public int? SizeQuota { get; set; }
    public int? BagCapacity { get; set; }

    public TrashBin(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}