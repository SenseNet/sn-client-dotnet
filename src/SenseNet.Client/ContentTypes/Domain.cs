using Microsoft.Extensions.Logging;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class Domain : Folder
{
    public string? SyncGuid { get; set; }
    public DateTime? LastSync { get; set; }

    public Domain(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}