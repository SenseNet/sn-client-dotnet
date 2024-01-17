using System;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Represents an email. Use DisplayName for Subject.
/// </summary>
public class Email : Folder
{
    public string From { get; set; }
    public string Body { get; set; }
    public DateTime? Sent { get; set; }

    public Email(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}