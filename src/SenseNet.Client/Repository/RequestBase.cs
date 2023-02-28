using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public abstract class RequestBase
{
    private static class P
    {
        public static readonly string Expand = "$expand";
        public static readonly string Select = "$select";

        public static readonly string Metadata = "metadata";
        public static readonly string Version = "version";
    }

    protected virtual bool AddWellKnownItem(KeyValuePair<string, string> item)
    {
        if (item.Key == P.Expand) { Expand = item.Value.Split(',').Select(x => x.Trim()).ToArray(); return true; }
        if (item.Key == P.Select) { Select = item.Value.Split(',').Select(x => x.Trim()).ToArray(); return true; }
        if (item.Key == P.Version) { Version = item.Value; return true; }
        if (item.Key == P.Metadata)
        {
            var value = item.Value;
            if (value.ToLowerInvariant() == "no")
                value = "none";
            Metadata = (MetadataFormat)Enum.Parse(typeof(MetadataFormat), value, true); return true;
        }
        //TODO: Implement and use "Format" property if needed.
        //if (item.Key == P.Format) { Format = ?; return true; }

        return false;
    }
    protected virtual bool RemoveWellKnownItem(KeyValuePair<string, string> item)
    {
        if (item.Key == P.Expand) { Expand = default; return true; }
        if (item.Key == P.Select) { Select = default; return true; }
        if (item.Key == P.Metadata) { Metadata = default; return true; }
        if (item.Key == P.Version) { Version = default; return true; }
        //TODO: Implement and use "Format" property if needed.
        //if (item.Key == P.Format) { Format = default; return true; }

        return false;
    }

    //============================================================================= Properties

    /// <summary>
    /// Gets or sets the "version-request" parameter.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets the list of selectable field names.
    /// </summary>
    public IEnumerable<string> Select { get; set; }
    /// <summary>
    /// Gets or sets the list of expandable field names.
    /// </summary>
    public IEnumerable<string> Expand { get; set; }

    /// <summary>
    /// Gets or sets the format of the requested metadata information. Default is None.
    /// </summary>
    public MetadataFormat Metadata { get; set; }

    /// <summary>
    /// Gets a container for any custom URL parameters.
    /// </summary>
    public ODataRequestParameterCollection Parameters { get; }

    /// <summary>
    /// Initializes an instance of the LoadContentRequest class.
    /// </summary>
    public RequestBase()
    {
        Metadata = MetadataFormat.None;
        Parameters = new ODataRequestParameterCollection(AddWellKnownItem, RemoveWellKnownItem);
    }

    public ODataRequest ToODataRequest(ServerContext server)
    {
        var oDataRequest = new ODataRequest(server)
        {
            Version = this.Version,
            Select = this.Select,
            Expand = this.Expand,
            Metadata = this.Metadata
        };

        AddProperties(oDataRequest);

        foreach (var parameter in this.Parameters)
            oDataRequest.Parameters.Add(parameter);
        return oDataRequest;
    }

    protected abstract void AddProperties(ODataRequest oDataRequest);
}