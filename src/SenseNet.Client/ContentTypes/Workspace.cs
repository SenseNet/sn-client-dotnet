using System;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

internal class Workspace : Folder
{
    public User Manager { get; set; }
    public DateTime? Deadline { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsCritical { get; set; }
    public bool? IsWallContainer { get; set; }
    public bool? IsFollowed { get; set; }

    public Workspace(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}