using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum TaskPriority{ Urgent = 1, Normal = 2, NotUrgent = 3 }
public enum TaskState{ Pending, Active, Completed, Deferred, Waiting }

public class SnTask : ListItem
{
    public SnTask(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

    public TaskPriority? Priority { get; set; }
    public TaskState? State { get; set; }

    protected override bool TryConvertFromProperty(string propertyName, out object convertedValue)
    {
        switch (propertyName)
        {
            case nameof(Priority):
                convertedValue = EnumValueToStringArray((int?)Priority);
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
            default:
                return base.TryConvertToProperty(propertyName, jsonValue, out propertyValue);
        }
    }
}