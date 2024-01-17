using System;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class OrganizationalUnit : Folder
{
    public string SyncGuid { get; set; }
    public DateTime? LastSync { get; set; }

    public OrganizationalUnit(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}