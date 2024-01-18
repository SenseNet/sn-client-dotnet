using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum TaskPriority
{
    [JsonProperty("1")] Urgent = 1,
    [JsonProperty("2")] Normal = 2,
    [JsonProperty("3")] NotUrgent = 3
}

public enum TaskState
{
    [JsonProperty("pending")] Pending,
    [JsonProperty("active")] Active,
    [JsonProperty("completed")] Completed,
    [JsonProperty("deferred")] Deferred,
    [JsonProperty("waiting")] Waiting
}

public class SnTask : ListItem
{
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public User AssignedTo { get; set; }
    public TaskPriority? Priority { get; set; }
    public TaskState? Status { get; set; }
    public int? TaskCompletion { get; set; }
    public int? RemainingDays { get; set; }
    public string DueText { get; set; }
    public string DueCssClass { get; set; }

    public SnTask(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
    /*
    protected override bool TryConvertFromProperty(string propertyName, out object convertedValue)
    {
        switch (propertyName)
        {
            case nameof(Priority):
                convertedValue = EnumValueToStringArray((int?)Priority);
                return true;
            case nameof(Status):
                convertedValue = EnumNameToStringArray(Status?.ToString());
                return true;
            default:
                return base.TryConvertFromProperty(propertyName, out convertedValue);
        }
    }
    protected override bool TryConvertToProperty(string propertyName, JToken jsonValue, out object propertyValue)
    {
        switch (propertyName)
        {
            case nameof(Priority):
            {
                if (StringArrayToInt(jsonValue, out var converted))
                    propertyValue = (TaskPriority)converted;
                else
                    propertyValue = null;
                return true;
            }
            case nameof(Status):
            {
                if (StringArrayToEnum<TaskState>(jsonValue, out var converted))
                    propertyValue = (TaskState)converted;
                else
                    propertyValue = null;
                return true;
            }
            default:
                return base.TryConvertToProperty(propertyName, jsonValue, out propertyValue);
        }
    }
    */
}