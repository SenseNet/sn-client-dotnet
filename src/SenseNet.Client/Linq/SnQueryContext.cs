using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.Client.Linq;
/// <summary>
/// Defines metadata for indexing a field.
/// </summary>
public interface IPerFieldIndexingInfo
{
    /// <summary>
    /// Gets or sets the System.Type of the field's native value.
    /// </summary>
    Type FieldDataType { get; set; }
}

/// <summary>
/// Defines a context for query execution.
/// </summary>
public interface IQueryContext
{
    /// <summary>
    /// Gets the current query extension values (top, skip, sort etc.).
    /// </summary>
    QuerySettings Settings { get; }

    /// <summary>
    /// Gets the logged in user's id.
    /// </summary>
    int UserId { get; }

    /// <summary>
    /// Returns a field indexing metadata by given fieldName.
    /// </summary>
    IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
}

/// <summary>
/// Implements a context for query execution.
/// </summary>
public class SnQueryContext : IQueryContext
{
    /// <inheritdoc />
    public QuerySettings Settings { get; }
    /// <inheritdoc />
    public int UserId { get; }

    /// <summary>
    /// Initializes a new instance of the SnQueryContext.
    /// </summary>
    public SnQueryContext(QuerySettings settings, int userId)
    {
        Settings = settings ?? QuerySettings.Default;
        UserId = userId;
    }

    /// <inheritdoc />
    public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates a default context for the content query with the currently logged-in user.
    /// </summary>
    /// <returns></returns>
    public static IQueryContext CreateDefault()
    {
        //return new SnQueryContext(QuerySettings.Default, AccessProvider.Current.GetCurrentUser().Id);
        throw new NotImplementedException();
    }
}