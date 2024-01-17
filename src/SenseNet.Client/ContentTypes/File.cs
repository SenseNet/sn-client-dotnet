using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Represents a file in the sensenet repository.
/// </summary>
public class File : Content
{
    public long? Size { get; set; }
    public long? FullSize { get; set; }
    public int? PageCount { get; set; }
    public string MimeType { get; set; }
    public string Shapes { get; set; }
    public string PageAttributes { get; set; }
    public string Watermark { get; set; }

    public File(IRestCaller restCaller, ILogger<File> logger) : base(restCaller, logger) { }
}