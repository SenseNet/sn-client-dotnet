using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Defines constants for enabling preview generation.
/// </summary>
public enum PreviewEnabled
{
    /// <summary>Preview generation depends on the parent content.</summary>
    [JsonProperty("0")] Inherited,
    /// <summary>Preview generation is disabled.</summary>
    [JsonProperty("1")] No,
    /// <summary>Preview generation is enabled.</summary>
    [JsonProperty("2")] Yes
}

public class Folder : Content
{
    public Folder(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

    public PreviewEnabled? PreviewEnabled { get; set; }
}