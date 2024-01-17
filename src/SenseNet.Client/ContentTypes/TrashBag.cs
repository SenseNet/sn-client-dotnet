using Microsoft.Extensions.Logging;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class TrashBag : Folder
{
    public DateTime? KeepUntil { get; set; }
    public string OriginalPath { get; set; }
    public string WorkspaceRelativePath { get; set; }
    public int? WorkspaceId { get; set; }
    public Content DeletedContent { get; set; }

    public TrashBag(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}