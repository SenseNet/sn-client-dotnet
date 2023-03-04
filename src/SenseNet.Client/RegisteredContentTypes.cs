using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;

namespace SenseNet.Client;

/// <summary>
/// Defines a class that holds the set of registered content types. Can be register one or more content types.
/// See the RepositoryExtensions.ConfigureSenseNetRepository extension method for registering content types
/// to the specific repository and the RepositoryExtensions.RegisterGlobalContentType extension method for
/// registering repository-independent content types.
/// </summary>
[DebuggerDisplay("RegisteredContentTypes: {ContentTypes.Count}")]
public class RegisteredContentTypes
{
    internal IDictionary<string, Type> ContentTypes { get; } = new Dictionary<string, Type>();

    /// <summary>
    /// Adds a content type to the set.
    /// </summary>
    /// <param name="contentType">The content type to register.</param>
    /// <param name="contentTypeName">The name if it is different from the name of the given <paramref name="contentType"/>'s name.</param>
    /// <returns>Itself to be used in fluent mode.</returns>
    public RegisteredContentTypes Add(Type contentType, string contentTypeName = null)
    {
        ContentTypes[contentTypeName ?? contentType.Name] = contentType;
        return this;
    }
    /// <summary>
    /// Adds a content type to the set.
    /// </summary>
    /// <typeparam name="T">The content type to register.</typeparam>
    /// <param name="contentTypeName">The name if it is different from the name of the given <paramref name="contentType"/>'s name.</param>
    /// <returns>Itself to be used in fluent mode.</returns>
    public RegisteredContentTypes Add<T>(string contentTypeName = null) where T : Content
    {
        ContentTypes[contentTypeName ?? typeof(T).Name] = typeof(T);
        return this;
    }

    internal string GetContentTypeNameByType(Type contentType)
    {
        return ContentTypes.FirstOrDefault(x => x.Value == contentType).Key;
    }
}