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
    public User? AssignedTo { get; set; }
    public TaskPriority? Priority { get; set; }
    public TaskState? Status { get; set; }
    public int? TaskCompletion { get; set; }
    public int? RemainingDays { get; set; }
    public string? DueText { get; set; }
    public string? DueCssClass { get; set; }

    public SnTask(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}