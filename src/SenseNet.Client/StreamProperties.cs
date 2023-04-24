namespace SenseNet.Client;

/// <summary>
/// Contains additional information about the downloading stream decoded from response headers.
/// </summary>
public class StreamProperties
{
    /// <summary>
    /// Gets or sets the Mime Type of the downloading stream
    /// </summary>
    public string MediaType { get; set; }
    /// <summary>
    /// Gets or sets the recommended file name.
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// Gets or sets the length of the downloading stream.
    /// </summary>
    public long? ContentLength { get; set; }
}