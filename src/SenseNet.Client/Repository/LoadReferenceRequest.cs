using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class LoadReferenceRequest : ContentRequest
{
    private static class P
    {
        public static readonly string Top = "$top";
        public static readonly string Skip = "$skip";
        public static readonly string Filter = "$filter";
        public static readonly string OrderBy = "$orderby";
        public static readonly string InlineCount = "$inlinecount";
        public static readonly string AutoFilters = "enableautofilters";
        public static readonly string LifespanFilter = "enablelifespanfilter";
    }

    protected override bool AddWellKnownItem(KeyValuePair<string, string> item)
    {
        if (item.Key == P.Top) { Top = int.Parse(item.Value); return true; }
        if (item.Key == P.Skip) { Skip = int.Parse(item.Value); return true; }
        if (item.Key == P.Filter) { ReferenceFilter = item.Value; return true; }
        if (item.Key == P.OrderBy) { OrderBy = item.Value.Split(',').Select(x => x.Trim()).ToArray(); return true; }
        if (item.Key == P.InlineCount) { InlineCount = (InlineCountOptions)Enum.Parse(typeof(InlineCountOptions), item.Value, true); return true; }
        if (item.Key == P.AutoFilters)
        {
            var value = item.Value;
            if (value.ToLowerInvariant() == "true")
                value = "enabled";
            if (value.ToLowerInvariant() == "false")
                value = "disabled";
            AutoFilters = (FilterStatus)Enum.Parse(typeof(FilterStatus), value, true); return true;
        }
        if (item.Key == P.LifespanFilter)
        {
            var value = item.Value;
            if (value.ToLowerInvariant() == "true")
                value = "enabled";
            if (value.ToLowerInvariant() == "false")
                value = "disabled";
            LifespanFilter = (FilterStatus)Enum.Parse(typeof(FilterStatus), value, true); return true;
        }

        return base.AddWellKnownItem(item);
    }
    protected override bool RemoveWellKnownItem(KeyValuePair<string, string> item)
    {
        if (item.Key == P.Top) { Top = default; return true; }
        if (item.Key == P.Skip) { Skip = default; return true; }
        if (item.Key == P.Filter) { ReferenceFilter = default; return true; }
        if (item.Key == P.OrderBy) { OrderBy = default; return true; }
        if (item.Key == P.InlineCount) { InlineCount = default; return true; }
        if (item.Key == P.AutoFilters) { AutoFilters = default; return true; }
        if (item.Key == P.LifespanFilter) { LifespanFilter = default; return true; }

        return base.RemoveWellKnownItem(item);
    }

    //============================================================================= Properties

    /// <summary>
    /// Gets or sets the Content id that will be the base of the OData request
    /// e.g. OData.svc/content(2)
    /// </summary>
    public int ContentId { get; set; }
    /// <summary>
    /// Gets or sets the Content path that will be the base of the OData request if the Content id is not provided
    /// e.g. OData.svc/Root('Content')
    /// </summary>
    public string Path { get; set; }
    /// <summary>
    /// Gets or sets the member (last) segment of the single entity in the OData request
    /// e.g. OData.svc/content(2)/CreatedBy
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Gets or sets the "top" query parameter.
    /// </summary>
    public int Top { get; set; }
    /// <summary>
    /// Gets or sets the "skip" query parameter.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets a standard OData filter of the referenced entities.
    /// </summary>
    public string ReferenceFilter { get; set; }

    /// <summary>
    /// Gets or sets the total count parameter if the resource is a collection.
    /// </summary>
    public InlineCountOptions InlineCount { get; set; }
    /// <summary>
    /// Gets or sets the value of the auto filter parameter.
    /// </summary>
    public FilterStatus AutoFilters { get; set; }
    /// <summary>
    /// Gets or sets the value of the lifespan filter parameter.
    /// </summary>
    public FilterStatus LifespanFilter { get; set; }
    /// <summary>
    /// Gets or sets the sorting definition.
    /// It can contain an existing FieldName followed by the optional
    /// sorting direction (e.g. "CreationDate desc").
    /// </summary>
    public string[] OrderBy { get; set; }

    protected override void AddProperties(ODataRequest oDataRequest)
    {
        base.AddProperties(oDataRequest);

        if (string.IsNullOrEmpty(Path) && ContentId < 1)
            throw new InvalidOperationException("Invalid request properties: ContentId or Path must be provided.");
        if (string.IsNullOrEmpty(FieldName))
            throw new InvalidOperationException("Invalid request properties: FieldName must be provided.");

        oDataRequest.IsCollectionRequest = false;
        oDataRequest.ContentId = ContentId;
        oDataRequest.Path = Path;
        oDataRequest.ActionName = FieldName;

        oDataRequest.Top = this.Top;
        oDataRequest.Skip = this.Skip;
        oDataRequest.ContentQuery = this.ReferenceFilter;
        oDataRequest.InlineCount = this.InlineCount;
        oDataRequest.AutoFilters = this.AutoFilters;
        oDataRequest.LifespanFilter = this.LifespanFilter;
        oDataRequest.OrderBy = this.OrderBy;
    }
}