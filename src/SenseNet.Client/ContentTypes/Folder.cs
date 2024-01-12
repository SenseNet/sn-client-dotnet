using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Defines constants for enabling preview generation.
/// </summary>
public enum PreviewEnabled
{
    /// <summary>Preview generation depends on the parent content.</summary>
    Inherited,
    /// <summary>Preview generation is disabled.</summary>
    No,
    /// <summary>Preview generation is enabled.</summary>
    Yes
}

public class Folder : Content
{
    public Folder(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

    public PreviewEnabled? PreviewEnabled { get; set; }

    protected override bool TryConvertToProperty(string propertyName, JToken jsonValue, out object propertyValue)
    {
        if (propertyName == nameof(PreviewEnabled))
        {
            if (StringArrayToInt(jsonValue, out var converted))
                propertyValue = (PreviewEnabled)converted;
            else
                propertyValue = null;
            return true;
        }
        return base.TryConvertToProperty(propertyName, jsonValue, out propertyValue);
    }

    protected override bool TryConvertFromProperty(string propertyName, out object convertedValue)
    {
        if (propertyName == nameof(PreviewEnabled))
        {
            convertedValue = EnumValueToStringArray((int?)PreviewEnabled);
            return true;
        }
        return base.TryConvertFromProperty(propertyName, out convertedValue);
    }
}