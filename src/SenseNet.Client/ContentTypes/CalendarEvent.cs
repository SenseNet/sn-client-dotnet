using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum EventNotificationMode
{
    [JsonProperty("E-mail")]
    Email,
    [JsonProperty("E-mail digest")]
    EmailDigest,
    None
}

[Flags] public enum EventType { Deadline = 1, Meeting = 2, Demo = 4 }

public class CalendarEvent : ListItem
{
    public string Location { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Lead { get; set; }
    public bool? AllDay { get; set; }
    public string EventUrl { get; set; }
    public bool? RequiresRegistration { get; set; }
    public string OwnerEmail { get; set; }
    public EventNotificationMode? NotificationMode { get; set; }
    public string EmailTemplate { get; set; }
    public string EmailTemplateSubmitter { get; set; }
    public string EmailFrom { get; set; }
    public string EmailFromSubmitter { get; set; }
    public string EmailField { get; set; }
    public int? MaxParticipants { get; set; }
    public int? NumParticipants { get; set; }
    public EventType? EventType { get; set; }

    public CalendarEvent(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}