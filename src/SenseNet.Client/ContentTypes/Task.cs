using System;
using System.Linq;
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
    public TaskState? Status { get; set; }

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

    private string[] EnumNameToStringArray(string enumName)
    {
        if (string.IsNullOrEmpty(enumName))
            return null;
        return new[] {enumName};
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

    private bool StringArrayToEnum<TEnum>(JToken jsonValue, out object propertyValue) where TEnum : struct
    {
        var arrayValue = jsonValue as JArray;
        if (arrayValue != null && arrayValue.Count == 0)
        {
            propertyValue = null;
            return true;
        }

        TEnum parsed = default(TEnum);
        var stringValue = (arrayValue?.FirstOrDefault() as JValue)?.Value<string>();
        if (Enum.TryParse(stringValue, true, out parsed))
        {
            propertyValue = parsed;
            return true;
        }
        propertyValue = null;
        return false;

    }
    protected bool StringArrayToInt(JToken jsonValue, out int converted)
    {
        var stringValue = ((jsonValue as JArray)?.FirstOrDefault() as JValue)?.Value<string>();
        return int.TryParse(stringValue, out converted);
    }

}