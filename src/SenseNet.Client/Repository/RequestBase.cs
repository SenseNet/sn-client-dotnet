using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public abstract class RequestBase
{
    /// <summary>
    /// Gets a container for any custom URL parameters.
    /// </summary>
    public ODataRequestParameterCollection Parameters { get; }

    /// <summary>
    /// Gets a dictionary for setting additional request headers.
    /// </summary>
    public Dictionary<string, IEnumerable<string>> AdditionalRequestHeaders { get; } = new();

    /// <summary>
    /// Initializes an instance of the LoadContentRequest class.
    /// </summary>
    protected RequestBase()
    {
        Parameters = new ODataRequestParameterCollection(AddWellKnownItem, RemoveWellKnownItem);
    }
    protected virtual bool AddWellKnownItem(KeyValuePair<string, string> item) => false;
    protected virtual bool RemoveWellKnownItem(KeyValuePair<string, string> item) => false;

    public ODataRequest ToODataRequest(ServerContext server)
    {
        var oDataRequest = new ODataRequest(server);

        AddProperties(oDataRequest);

        foreach (var parameter in this.Parameters)
            oDataRequest.Parameters.Add(parameter);

        foreach (var item in AdditionalRequestHeaders)
            oDataRequest.AdditionalRequestHeaders.Add(item.Key, item.Value);

        return oDataRequest;
    }

    protected abstract void AddProperties(ODataRequest oDataRequest);
}