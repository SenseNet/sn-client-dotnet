// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

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
        oDataRequest.Path = this.Path;
        oDataRequest.ContentId = this.ContentId;
    }
}