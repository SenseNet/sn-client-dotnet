using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Technical content type. This is the common ancestor of the Task and
/// later the CalendarEvent, CustomListItem, Link and Memo types
/// </summary>
public abstract class ListItem : Content
{
    protected ListItem(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}