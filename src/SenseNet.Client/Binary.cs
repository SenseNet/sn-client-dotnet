using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace SenseNet.Client;

/// <summary>
/// A metadata object representing a Binary field.
/// </summary>
/// <remarks>
/// This is a deserialized media resource of the standard OData response with additional back-reference of the owner Content.
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
    /// Gets or sets the "edit_media" property of the "__mediaresource" object from the OData response.
    /// </summary>
    [JsonProperty(PropertyName = "edit_media")]
    public string EditMedia { get; set; }
    /// <summary>
    /// Gets or sets the "media_src" property of the "__mediaresource" object from the OData response.
    /// </summary>
    [JsonProperty(PropertyName = "media_src")]
    public string MediaSrc { get; set; }
    /// <summary>
    /// Gets or sets the "content_type" property of the "__mediaresource" object from the OData response.
    /// </summary>
    [JsonProperty(PropertyName = "content_type")]
    public string ContentType { get; set; }
    /// <summary>
    /// Gets or sets the "media_etag" property of the "__mediaresource" object from the OData response.
    /// </summary>
    [JsonProperty(PropertyName = "media_etag")]
    public string MediaEtag { get; set; }

    /// <summary>
    /// Downloads a binary stream that this object represents. The provided callback method
    /// is called by the API with the stream and its properties.
    /// </summary>
    /// <remarks>
    /// An example for downloading a text file:
    /// <code>
    /// string text;
    /// await content.Binary.DownloadAsync(async (stream, properties) =>
    /// {
    ///     using var reader = new StreamReader(stream);
    ///     text = await reader.ReadToEndAsync().ConfigureAwait(false);
    /// }, cancellationToken);
    /// </code>
    /// </remarks>
    /// <param name="responseProcessor">Callback for controlling the download.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DownloadAsync(Func<Stream, StreamProperties, Task> responseProcessor, CancellationToken cancel)
    {
        return this.OwnerContent.Repository.DownloadAsync(new DownloadRequest {MediaSrc = this.MediaSrc},
            responseProcessor, cancel);
    }
}