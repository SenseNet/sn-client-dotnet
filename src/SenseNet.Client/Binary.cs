using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace SenseNet.Client;

/// <summary>
/// Represents a metadata object of a Binary type property.
/// </summary>
/// <remarks>
/// This is a deserialized media resource of the standard OData with additional back-reference of the owner Content.
/// Do not instantiate this object in your code and do not write any property.
/// </remarks>
public class Binary
{
    /// <summary>
    /// Gets or sets the owner Content.
    /// </summary>
    [JsonIgnore]
    internal Content OwnerContent { get; set; }

    /// <summary>
    /// Gets or sets the deserialized "edit_media" of the "__mediaresource" object from OData response.
    /// </summary>
    [JsonProperty(PropertyName = "edit_media")]
    public string EditMedia { get; set; }
    /// <summary>
    /// Gets or sets the deserialized "media_src" of the "__mediaresource" object from OData response.
    /// </summary>
    [JsonProperty(PropertyName = "media_src")]
    public string MediaSrc { get; set; }
    /// <summary>
    /// Gets or sets the deserialized "content_type" of the "__mediaresource" object from OData response.
    /// </summary>
    [JsonProperty(PropertyName = "content_type")]
    public string ContentType { get; set; }
    /// <summary>
    /// Gets or sets the deserialized "media_etag" of the "__mediaresource" object from OData response.
    /// </summary>
    [JsonProperty(PropertyName = "media_etag")]
    public string MediaEtag { get; set; }

    public Task DownloadAsync(Func<Stream, StreamProperties, Task> responseProcessor, CancellationToken cancel)
    {
        return this.OwnerContent.Repository.DownloadAsync(MediaSrc, responseProcessor, cancel);
    }
}