using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Defines a base class for collecting parameters in derived classes that are used in simple or complex remote calls.
/// </summary>
public abstract class RequestBase
{
    /// <summary>
    /// Gets a dictionary for setting additional request headers.
    /// </summary>
    public Dictionary<string, IEnumerable<string>> AdditionalRequestHeaders { get; } = new();
}