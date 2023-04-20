using System.Diagnostics;

namespace SenseNet.Client;

/// <summary>
/// Defines a strongly typed class for represent the result of an upload operation.
/// </summary>
[DebuggerDisplay("{Id} ({Type}): {Name}")]
public class UploadResult
{
    /// <summary>
    /// Gets or sets the Path of the uploaded content (e.g. "/Root/Content/File1")
    /// </summary>
    public string Url { get; set; }
    /// <summary>
    /// Gets or sets the Name of the uploaded content (e.g. "File1")
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the count of the uploaded bytes
    /// </summary>
    public long Length { get; set; }
    /// <summary>
    /// Gets or sets the name of the uploaded content's content type.
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Gets or sets the Id of the uploaded content.
    /// </summary>
    public int Id { get; set; }
}