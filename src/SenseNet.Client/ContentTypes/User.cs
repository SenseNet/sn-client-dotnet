using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

//UNDONE: Add doc
public enum Gender { NotDefined, Female, Male }
//UNDONE: Add doc
public enum MaritalStatus { NotDefined, Single, Married }

/// <summary>
/// Represents a user in the sensenet repository.
/// </summary>
public class User : Content
{
    public User(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger)
    {
    }

    public string LoginName { get; set; }
    public string DisplayName { get; set; }
    public string JobTitle { get; set; }
    public bool Enabled { get; set; }
    public string Domain { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public Image ImageRef { get; set; }
    public Avatar Avatar { get; set; }
    public DateTime LastSync { get; set; }
    public User Manager { get; set; }
    public string Department { get; set; }
    public string Languages { get; set; }
    public string Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public string TwitterAccount { get; set; }
    public string FacebookURL { get; set; }
    public string LinkedInURL { get; set; }
    public string ProfilePath { get; set; }
    public bool MultiFactorEnabled { get; set; }
    public Gender? Gender { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }

    protected override bool TryConvertToProperty(string propertyName, JToken jsonValue, out object propertyValue)
    {
        switch (propertyName)
        {
            case nameof(Gender):
            {
                var arrayValue = jsonValue as JArray;
                if (arrayValue != null && arrayValue.Count == 0)
                {
                    propertyValue = null;
                    return true;
                }
                var stringValue = (arrayValue?.FirstOrDefault() as JValue)?.Value<string>();
                if (Enum.TryParse<Gender>(stringValue, out var parsed))
                {
                    propertyValue = parsed;
                    return true;
                }
                propertyValue = null;
                return true;
            }
            case nameof(MaritalStatus):
            {
                var arrayValue = jsonValue as JArray;
                if (arrayValue != null && arrayValue.Count == 0)
                {
                    propertyValue = Client.MaritalStatus.NotDefined;
                    return true;
                }
                var stringValue = (arrayValue?.FirstOrDefault() as JValue)?.Value<string>();
                if (Enum.TryParse<MaritalStatus>(stringValue, out var parsed))
                {
                    propertyValue = parsed;
                    return true;
                }
                propertyValue = null;
                return true;
            }
            default:
                return base.TryConvertToProperty(propertyName, jsonValue, out propertyValue);
        }
    }

    protected override bool TryConvertFromProperty(string propertyName, out object convertedValue)
    {
        switch (propertyName)
        {
            case nameof(Gender):
                convertedValue = GenderOrMaritalStatusToStringArray(Gender?.ToString());
                return true;
            case nameof(MaritalStatus):
                convertedValue = GenderOrMaritalStatusToStringArray(MaritalStatus?.ToString());
                return true;
            default:
                return base.TryConvertFromProperty(propertyName, out convertedValue);
        }
    }

    private string[] GenderOrMaritalStatusToStringArray(string enumName)
    {
        if (enumName == null)
            return null;
        if (enumName == "NotDefined")
            return new[] { "..." };
        return new[] {enumName};
    }
}

public class Avatar
{
    public string Url { get; set; }
}