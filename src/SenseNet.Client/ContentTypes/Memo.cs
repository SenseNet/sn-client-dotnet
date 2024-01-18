using Microsoft.Extensions.Logging;
using System;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public enum MemoType
{
    [JsonProperty("generic")]
    Generic,
    [JsonProperty("iso")]
    Iso,
    [JsonProperty("iaudit")]
    InternalAudit
}

public class Memo : ListItem
{
    public DateTime? Date { get; set; }
    public MemoType? MemoType { get; set; } 
    public Content SeeAlso { get; set; }

    public Memo(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}