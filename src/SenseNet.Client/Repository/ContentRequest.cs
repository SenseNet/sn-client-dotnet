// ReSharper disable once CheckNamespace
using System;
using System.Collections.Generic;
using System.Linq;

namespace SenseNet.Client;

public enum ResponseFormat
{
    Json, VerboseJson, TypeScript, Xml, Export, Table
}

public abstract class ContentRequest : RequestBase
{
    private static class P
    {
        public static readonly string Expand = "$expand";
        public static readonly string Select = "$select";
        public static readonly string Format = "$format";

        public static readonly string Metadata = "metadata";
        public static readonly string Version = "version";
    }
    protected override bool AddWellKnownItem(KeyValuePair<string, string> item)
    {
        if (item.Key == P.Expand) { Expand = item.Value.Split(',').Select(x => x.Trim()).ToArray(); return true; }
        if (item.Key == P.Select) { Select = item.Value.Split(',').Select(x => x.Trim()).ToArray(); return true; }
        if (item.Key == P.Version) { Version = item.Value; return true; }
        if (item.Key == P.Metadata)
        {
            var value = item.Value;
            if (value.ToLowerInvariant() == "no")
                value = "none";
            Metadata = (MetadataFormat)Enum.Parse(typeof(MetadataFormat), value, true);
            return true;
        }
        if (item.Key == P.Format)
        {
            Format = (ResponseFormat)Enum.Parse(typeof(ResponseFormat), item.Value, true);
            return true;
        }

        return false;
    }
    protected override bool RemoveWellKnownItem(KeyValuePair<string, string> item)
    {
        if (item.Key == P.Expand) { Expand = default; return true; }
        if (item.Key == P.Select) { Select = default; return true; }
        if (item.Key == P.Metadata) { Metadata = default; return true; }
        if (item.Key == P.Version) { Version = default; return true; }
        if (item.Key == P.Format) { Format = default; return true; }

        return false;
    }

    protected ContentRequest()
    {
        Metadata = MetadataFormat.None;
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
    /// Gets or sets the format of the OData response. Default is Json.
    /// </summary>
    public ResponseFormat Format { get; set; }

    protected override void AddProperties(ODataRequest oDataRequest)
    {
        oDataRequest.Version = this.Version;
        oDataRequest.Select = this.Select;
        oDataRequest.Expand = this.Expand;
        oDataRequest.Metadata = this.Metadata;

        if (Format != default)
            oDataRequest.Parameters.Add(P.Format, Format.ToString().ToLowerInvariant());
    }
}