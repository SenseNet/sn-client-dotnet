using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public abstract class ODataRequestBase : RequestBase
{
    /// <summary>
    /// Gets a container for any custom URL parameters.
    /// </summary>
    public ODataRequestParameterCollection Parameters { get; }

    /// <summary>
    /// Initializes an instance of the LoadContentRequest class.
    /// </summary>
    protected ODataRequestBase()
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