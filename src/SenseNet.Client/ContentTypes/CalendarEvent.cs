using System;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class CalendarEvent : ListItem
{
    public string Location { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Lead { get; set; }
    public bool? AllDay { get; set; }
    public string EventUrl { get; set; }
    public bool? RequiresRegistration { get; set; }
    //UNDONE:!! not implemented reference: RegistrationForm:EventRegistrationForm
    //public Reference RegistrationForm { get; set; }
    public string OwnerEmail { get; set; }
    //UNDONE: Implement NotificationMode property
    //public Choice NotificationMode { get; set; }
    public string EmailTemplate { get; set; }
    public string EmailTemplateSubmitter { get; set; }
    public string EmailFrom { get; set; }
    public string EmailFromSubmitter { get; set; }
    public string EmailField { get; set; }
    public int? MaxParticipants { get; set; }
    public int? NumParticipants { get; set; }
    //UNDONE: Implement EventType property
    //public Choice EventType { get; set; }

    public CalendarEvent(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}