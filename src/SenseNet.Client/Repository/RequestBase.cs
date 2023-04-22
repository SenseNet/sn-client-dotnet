using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public abstract class RequestBase
{
    /// <summary>
    /// Gets a dictionary for setting additional request headers.
    /// </summary>
    public Dictionary<string, IEnumerable<string>> AdditionalRequestHeaders { get; } = new();
}