﻿using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    public class QueryContentRequest : RequestBase
    {
        private static class P
        {
            public static readonly string Top = "$top";
            public static readonly string Skip = "$skip";
            public static readonly string OrderBy = "$orderby";
            public static readonly string InlineCount = "$inlinecount";
            public static readonly string AutoFilters = "enableautofilters";
            public static readonly string LifespanFilter = "enablelifespanfilter";
            public static readonly string ContentQuery = "query";
        }

        protected override bool AddWellKnownItem(KeyValuePair<string, string> item)
        {
            if (item.Key == P.Top) { Top = int.Parse(item.Value); return true; }
            if (item.Key == P.Skip) { Skip = int.Parse(item.Value); return true; }
            if (item.Key == P.OrderBy) { OrderBy = item.Value.Split(',').Select(x => x.Trim()).ToArray(); return true; }
            if (item.Key == P.InlineCount) { InlineCount = (InlineCountOptions)Enum.Parse(typeof(InlineCountOptions), item.Value, true); return true; }
            if (item.Key == P.ContentQuery) { ContentQuery = item.Value; return true; }
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
            if (item.Key == P.OrderBy) { OrderBy = default; return true; }
            if (item.Key == P.InlineCount) { InlineCount = default; return true; }
            if (item.Key == P.AutoFilters) { AutoFilters = default; return true; }
            if (item.Key == P.LifespanFilter) { LifespanFilter = default; return true; }
            if (item.Key == P.ContentQuery) { ContentQuery = default; return true; }

            return base.RemoveWellKnownItem(item);
        }

        //============================================================================= Properties

        /// <summary>
        /// Gets or sets the "top" query parameter.
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Gets or sets the "skip" query parameter.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Gets or sets a Content Query.
        /// </summary>
        public string ContentQuery { get; set; }

        /// <summary>
        /// Gets or sets the total count request if the resource is a collection.
        /// </summary>
        public InlineCountOptions InlineCount { get; set; }
        /// <summary>
        /// Gets or sets the value of the switch that controls the auto filtering.
        /// </summary>
        public FilterStatus AutoFilters { get; set; }
        /// <summary>
        /// Gets or sets the value of the switch that controls the lifespan filtering.
        /// </summary>
        public FilterStatus LifespanFilter { get; set; }
        /// <summary>
        /// Gets or sets the sorting of the children in priority order.
        /// Every item can be an existing FieldName optionally followed by the sorting direction
        /// (space + "asc" or "desc" e. g. "CreationDate desc")
        /// </summary>
        public string[] OrderBy { get; set; }

        protected override void AddProperties(ODataRequest oDataRequest)
        {
            oDataRequest.Path ??= "/Root";
            oDataRequest.IsCollectionRequest = true;

            oDataRequest.Top = this.Top;
            oDataRequest.Skip = this.Skip;
            oDataRequest.ContentQuery = this.ContentQuery;
            oDataRequest.InlineCount = this.InlineCount;
            oDataRequest.AutoFilters = this.AutoFilters;
            oDataRequest.LifespanFilter = this.LifespanFilter;
            oDataRequest.OrderBy = this.OrderBy;
        }
    }
}
