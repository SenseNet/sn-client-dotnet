using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Contains common parameters of upload operations.
/// </summary>
/// <remarks>
/// One of the <see cref="ParentPath"/> and <see cref="ParentId"/> is required.
/// If both missing, a <see cref="InvalidOperationException"/> will be thrown.
/// </remarks>
public class UploadRequest : ODataRequestBase
{
    /// <summary>
    /// Gets or sets the Path of the content that will be the parent of the uploaded content.
    /// This property ignores the <see cref="ParentId"/> if the value is not null.
    /// </summary>
    public string ParentPath { get; set; }
    /// <summary>
    /// Gets or sets the Id of the content that will be the parent of the uploaded content.
    /// This value is ignored if the <see cref="ParentPath"/> is not null.
    /// </summary>
    public int ParentId { get; set; }
    /// <summary>
    /// Gets or sets the name of the field to upload to. Default: "Binary".
    /// </summary>
    public string PropertyName { get; set; }
    /// <summary>
    /// Gets or sets the name of the Content to upload.
    /// </summary>
    public string ContentName { get; set; }
    /// <summary>
    /// Gets or sets the content type name of the uploaded content when the it does not exist on the server.
    /// </summary>
    public string ContentType { get; set; }
    /// <summary>
    /// Gets or sets the name of the binary stream if it is different form ContentName.
    /// </summary>
    public string FileName { get; set; }

    protected override void AddProperties(ODataRequest oDataRequest)
    {
        if (ParentId == 0 && string.IsNullOrEmpty(ParentPath))
            throw new InvalidOperationException("Invalid request properties: either ParentId or ParentPath must be provided.");

        if (ParentId == 0)
            oDataRequest.Path = ParentPath;
        else
            oDataRequest.ContentId = ParentId;

        oDataRequest.ActionName = "Upload";
    }
}