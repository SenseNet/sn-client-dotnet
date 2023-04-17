using System;
using System.Collections;
using System.Collections.Generic;

namespace SenseNet.Client;

/// <summary>
/// Represents a set of <see cref="Content"/> for <c>LoadCollection</c> and <c>Query</c>... methods.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IContentCollection<out T> : IEnumerable<T> where T : Content
{
    /// <summary>
    /// Returns count of the current set without executing enumeration.
    /// </summary>
    int Count { get; }
    /// <summary>
    /// Gets all items in the defined collection if the request property InlineCount is InlineCountOptions.AllPages,
    /// otherwise equals to Count.
    /// </summary>
    int TotalCount { get; }
}

public class ContentCollection<T> : IContentCollection<T> where T : Content
{
    private readonly IEnumerable<T> _items;
    public int Count { get; }
    public int TotalCount { get; }

    /// <summary>
    /// Represents the empty set of <see cref="Content"/>.
    /// </summary>
    public static IContentCollection<T> Empty { get; } = new ContentCollection<T>(Array.Empty<T>(), 0, 0);

    /// <summary>
    /// Initializes a new <see cref="ContentCollection&lt;T&gt;"/> instance.
    /// </summary>
    /// <param name="items">Wrapped items.</param>
    /// <param name="count">Count of the <paramref name="items"/>.</param>
    /// <param name="totalCount">Total count of the collection.</param>
    public ContentCollection(IEnumerable<T> items, int count, int totalCount)
    {
        _items = items;
        Count = count;
        TotalCount = totalCount;
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}