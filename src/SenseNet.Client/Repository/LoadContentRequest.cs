using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Represents a request for loading a content from the repository.
/// </summary>
public class LoadContentRequest : RequestBase
{
    /// <summary>
    /// Content path that will be the base of the OData request if the Content id is not provided.
    /// </summary>
    public string Path { get; set; }
    /// <summary>
    /// Content id that will be the base of the OData request.
    /// </summary>
    public int ContentId { get; set; }

    protected override void AddProperties(ODataRequest oDataRequest)
    {
        if (Path != default && ContentId != default)
            throw new InvalidOperationException("Invalid request properties: ContentId and Path cannot be specified at the same time.");
        oDataRequest.Path = this.Path;
        oDataRequest.ContentId = this.ContentId;
    }
}