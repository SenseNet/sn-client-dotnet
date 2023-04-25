// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Defines parameters for a stream download operations.
/// </summary>
public class DownloadRequest : RequestBase
{
    /// <summary>
    /// Gets or sets the Path of the Content that has the stream to download.
    /// Avoid using this parameter because it uses an extra web request. Use ContentId instead.
    /// </summary>
    public string Path { get; set; }
    /// <summary>
    /// Gets or sets the Id of the Content that has the stream to download.
    /// If the value is set (greater than 0), the Path and MediaSrc properties are omitted.
    /// </summary>
    public int ContentId { get; set; }
    /// <summary>
    /// Gets or sets the raw url of the stream to download.
    /// If the value is set (not null), the Path and ContentId properties are omitted.
    /// </summary>
    public string MediaSrc { get; set; }
    /// <summary>
    /// Gets or sets the property name of the content that represents the stream to download.
    /// If not provided, the default is 'Binary'.
    /// </summary>
    public string PropertyName { get; set; }
    /// <summary>
    /// Gets or sets the version of the requested Content. If not provided, the default is 'lastminor'.
    /// For example: V1.0, 2.3, v12.3456, V0.1.D, lastmajor
    /// </summary>
    public string Version { get; set; }
}