﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using SenseNet.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

/// <summary>
/// Defines the main client API for managing content items in a sensenet repository.
/// </summary>
public interface IRepository
{
    /// <summary>
    /// A context object that represents a connection to a sensenet service.
    /// </summary>
    /// <remarks>A repository instance always belongs to a single sensenet service.</remarks>
    public ServerContext Server { get; set; }

    /// <summary>
    /// Gets the registered repository-independent content types.
    /// </summary>
    public RegisteredContentTypes GlobalContentTypes { get; }

    /* ============================================================================ CREATION */

    /// <summary>
    /// Creates a new content instance in memory.
    /// If the requested type is not registered, the return value will be a <see cref="Content"/>.
    /// </summary>
    /// <param name="parentPath">Path of the already existing parent.</param>
    /// <param name="contentTypeName">Content type name.</param>
    /// <param name="name">Name of the content.</param>
    /// <returns>A new content instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>
    /// or <paramref name="contentTypeName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="parentPath"/>
    /// or <paramref name="contentTypeName"/> is empty.</exception>
    public Content CreateContent(string parentPath, string contentTypeName, string name);
    /// <summary>
    /// Creates a new content instance in memory by the given type parameter.
    /// The requested type should be registered or an <see cref="ApplicationException"/> will be thrown.
    /// </summary>
    /// <typeparam name="T">Type of the content (should be registered).</typeparam>
    /// <param name="parentPath">Path of the already existing parent.</param>
    /// <param name="name">Name of the content.</param>
    /// <returns>A new content instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="parentPath"/> is empty.</exception>
    /// <exception cref="ApplicationException">Thrown when the requested type is registered.</exception>
    public T CreateContent<T>(string parentPath, string name) where T : Content;

    /// <summary>
    /// Creates a new content instance in memory. When saved, the content is created from the
    /// given content template on the server.
    /// If the requested type is not registered, the return value will be a <see cref="Content"/>.
    /// </summary>
    /// <param name="parentPath">Path of the already existing parent.</param>
    /// <param name="contentTypeName">Content type name.</param>
    /// <param name="name">Name of the content. If it is null, the server will generate a name for the content.</param>
    /// <param name="contentTemplate">Content template name.</param>
    /// <returns>A new content instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>, <paramref name="contentTypeName"/>
    /// or <paramref name="contentTemplate"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>, <paramref name="contentTypeName"/>
    /// or <paramref name="contentTemplate"/> is empty.</exception>
    public Content CreateContentByTemplate(string parentPath, string contentTypeName, string name, string contentTemplate);
    //UNDONE: missing tests CreateContentByTemplate<T>()
    /// <summary>
    /// Creates a new content instance in memory. When saved, the content is created from the
    /// given content template on the server.
    /// The requested type should be registered or an <see cref="ApplicationException"/> will be thrown.
    /// </summary>
    /// <typeparam name="T">Type of the content (should be registered).</typeparam>
    /// <param name="parentPath">Path of the already existing parent.</param>
    /// <param name="name">Name of the content. If it is null, the server will generate a name for the content.</param>
    /// <param name="contentTemplate">Content template name.</param>
    /// <returns>A new content instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>
    /// or <paramref name="contentTemplate"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentPath"/>
    /// or <paramref name="contentTemplate"/> is empty.</exception>
    /// <exception cref="ApplicationException">Thrown when the requested type is registered.</exception>
    public T CreateContentByTemplate<T>(string parentPath, string name, string contentTemplate) where T : Content;

    /* ============================================================================ LOAD CONTENT */

    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <param name="id">Content id</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<Content> LoadContentAsync(int id, CancellationToken cancel);
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <param name="path">Content path</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<Content> LoadContentAsync(string path, CancellationToken cancel);
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <param name="requestData">Detailed request information.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<Content> LoadContentAsync(LoadContentRequest requestData, CancellationToken cancel);

    //UNDONE: missing tests LoadContentAsync<T>(int id, ...
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <typeparam name="T">Well known type of the content (should be registered)</typeparam>
    /// <param name="id">Content id</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<T> LoadContentAsync<T>(int id, CancellationToken cancel) where T : Content;
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <typeparam name="T">Well known type of the content (should be registered)</typeparam>
    /// <param name="path">Content path</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<T> LoadContentAsync<T>(string path, CancellationToken cancel) where T : Content;
    //UNDONE: missing tests LoadContentAsync<T>(LoadContentRequest requestData, ...
    /// <summary>
    /// Loads an existing content.
    /// </summary>
    /// <typeparam name="T">Well known type of the content (should be registered)</typeparam>
    /// <param name="requestData">Detailed request information.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that wraps the content or null.</returns>
    public Task<T> LoadContentAsync<T>(LoadContentRequest requestData, CancellationToken cancel) where T : Content;

    /* ============================================================================ LOAD COLLECTION */

    /// <summary>
    /// Loads child elements of the provided content.
    /// </summary>
    /// <remarks>This method loads only child elements, not the whole subtree.</remarks>
    /// <param name="requestData">Collection request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>Children of the provided content.</returns>
    public Task<IEnumerable<Content>> LoadCollectionAsync(LoadCollectionRequest requestData, CancellationToken cancel);

    /// <summary>
    /// Gets the count of a children collection. 
    /// </summary>
    /// <remarks>This method counts only child elements, not the whole subtree.</remarks>
    /// <param name="requestData">Collection request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>The count of a children collection.</returns>
    public Task<int> GetContentCountAsync(LoadCollectionRequest requestData, CancellationToken cancel);

    /* ============================================================================ EXISTENCE */

    /// <summary>
    /// Checks if a content with the provided path exists in the repository.
    /// </summary>
    /// <param name="path">Content path.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>True if the content exists and the current user can access it.</returns>
    public Task<bool> IsContentExistsAsync(string path, CancellationToken cancel);

    /* ============================================================================ QUERY */

    /// <summary>
    /// Loads content items by a query with lifespan and system filters switched OFF.
    /// </summary>
    /// <remarks>This method is able to load contents from the whole repository, not only a single folder.</remarks>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>List of contents returned by the provided content query.</returns>
    public Task<IEnumerable<Content>> QueryForAdminAsync(QueryContentRequest requestData, CancellationToken cancel);
    /// <summary>
    /// Loads content items by a query.
    /// </summary>
    /// <remarks>This method is able to load contents from the whole repository, not only a single folder.</remarks>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>List of contents returned by the provided content query.</returns>
    public Task<IEnumerable<Content>> QueryAsync(QueryContentRequest requestData, CancellationToken cancel);

    /// <summary>
    /// Gets the count of content items by a query with lifespan and system filters switched OFF.
    /// </summary>
    /// <remarks>This method is able to count contents in the whole repository, not only a single folder.</remarks>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>Count of contents returned by the provided content query.</returns>
    public Task<int> QueryCountForAdminAsync(QueryContentRequest requestData, CancellationToken cancel);
    /// <summary>
    /// Gets the count of content items by a query.
    /// </summary>
    /// <remarks>This method is able to count contents in the whole repository, not only a single folder.</remarks>
    /// <param name="requestData">Query request parameters.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>Count of contents returned by the provided content query.</returns>
    public Task<int> QueryCountAsync(QueryContentRequest requestData, CancellationToken cancel);

    /* ============================================================================ DELETE */

    /// <summary>
    /// Deletes a content by path.
    /// </summary>
    /// <param name="path">Path of the <see cref="Content"/> to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(string path, bool permanent, CancellationToken cancel);
    /// <summary>
    /// Deletes multiple contents by path.
    /// </summary>
    /// <param name="paths">List of paths of <see cref="Content"/> items to delete.</param>
    /// <param name="permanent">Delete the contents permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(string[] paths, bool permanent, CancellationToken cancel);
    /// <summary>
    /// Deletes a content by id.
    /// </summary>
    /// <param name="id">Id of the <see cref="Content"/> to delete.</param>
    /// <param name="permanent">Delete the content permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(int id, bool permanent, CancellationToken cancel);
    /// <summary>
    /// Deletes multiple contents by id.
    /// </summary>
    /// <param name="ids">List of ids of <see cref="Content"/> items to delete.</param>
    /// <param name="permanent">Delete the contents permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(int[] ids, bool permanent, CancellationToken cancel);
    /// <summary>
    /// Deletes multiple contents by id or path.
    /// </summary>
    /// <param name="idsOrPaths">List of ids or paths of <see cref="Content"/> items to delete.</param>
    /// <param name="permanent">Delete the contents permanently or into the Trash.</param>
    /// <param name="cancel">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents an asynchronous operation.</returns>
    public Task DeleteContentAsync(object[] idsOrPaths, bool permanent, CancellationToken cancel);

    /* ============================================================================ TOOLS */

    public string GetContentTypeNameByType<T>(); //UNDONE: missing tests GetContentTypeNameByType<T>()
    public string GetContentTypeNameByType(Type contentType); //UNDONE: missing tests GetContentTypeNameByType(Type contentType)
}