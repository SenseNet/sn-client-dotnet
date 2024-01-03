using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class OperationRequest : LoadCollectionRequest
{
    public int ContentId { get; set; }
    public string OperationName { get; set; }
    public object PostData { get; set; }

    protected override void AddProperties(ODataRequest oDataRequest)
    {
        // Avoid InvalidOperationException
        if (ContentId > 0 && string.IsNullOrEmpty(Path))
            Path = "/Root";

        if (string.IsNullOrEmpty(OperationName))
            throw new InvalidOperationException("Invalid request properties: OperationName must be provided.");

        base.AddProperties(oDataRequest);

        oDataRequest.ContentId = ContentId;
        oDataRequest.ActionName = OperationName;
        oDataRequest.PostData = PostData;

        // Set back the "false" value because operation can be called on a single content.
        oDataRequest.IsCollectionRequest = false;

    }
}
