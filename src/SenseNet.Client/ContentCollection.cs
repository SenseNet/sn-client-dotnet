using System;
using System.Collections;
using System.Collections.Generic;

namespace SenseNet.Client;

public interface IContentCollection<out T> : IEnumerable<T> where T : Content
{
    int Count { get; }
    int TotalCount { get; }
}

public class ContentCollection<T> : IContentCollection<T> where T : Content
{
    private readonly IEnumerable<T> _items;
    public int Count { get; }
    public int TotalCount { get; }
    public static ContentCollection<T> Empty { get; } = new ContentCollection<T>(Array.Empty<T>(), 0, 0);

    public ContentCollection(IEnumerable<T> items, int count, int totalCount)
    {
        _items = items;
        Count = count;
        TotalCount = totalCount;
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}