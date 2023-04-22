// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class DownloadRequest : RequestBase
{
    public int ContentId { get; set; }
    public string Path { get; set; }
    public string PropertyName { get; set; }
    public string Version { get; set; }
}