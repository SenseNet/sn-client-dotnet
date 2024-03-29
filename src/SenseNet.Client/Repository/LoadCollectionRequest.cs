﻿using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Represents a request for a content collection.
/// </summary>
public class LoadCollectionRequest : QueryContentRequest
{
    private static class P
    {
        public static readonly string Filter = "$filter";
    }

    protected override bool AddWellKnownItem(KeyValuePair<string, string> item)
    {
        if (item.Key == P.Filter) { ChildrenFilter = item.Value; return true; }

        return base.AddWellKnownItem(item);
    }
    protected override bool RemoveWellKnownItem(KeyValuePair<string, string> item)
    {
        if (item.Key == P.Filter) { ChildrenFilter = default; return true; }

        return base.RemoveWellKnownItem(item);
    }

    //============================================================================= Properties

    /// <summary>
    /// Gets or sets a standard OData filter of child entities.
    /// </summary>
    public string ChildrenFilter { get; set; }

    protected override void AddProperties(ODataRequest oDataRequest)
    {
        base.AddProperties(oDataRequest);

        if (string.IsNullOrEmpty(Path))
            throw new InvalidOperationException("Invalid request properties: Path must be provided.");
        if (ContentQuery != default && ChildrenFilter != default)
            throw new InvalidOperationException("Invalid request properties: ContentQuery and ChildrenFilter cannot be specified at the same time.");

        oDataRequest.IsCollectionRequest = true;

        oDataRequest.Path = this.Path;
        oDataRequest.ChildrenFilter = this.ChildrenFilter;
    }
}
