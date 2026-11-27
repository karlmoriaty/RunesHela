using System;
using System.Collections;
using System.Collections.Generic;

namespace RunesHela.Generator;

internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
{
    private readonly T[]? _items;

    public EquatableArray(T[] items)
    {
        _items = items;
    }

    public static EquatableArray<T> Empty => new(Array.Empty<T>());

    public int Count => _items?.Length ?? 0;

    public T this[int index] => _items![index];

    public bool Equals(EquatableArray<T> other)
    {
        var left = _items ?? Array.Empty<T>();
        var right = other._items ?? Array.Empty<T>();

        if (left.Length != right.Length)
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;

        for (var i = 0; i < left.Length; i++)
        {
            if (!comparer.Equals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        var items = _items ?? Array.Empty<T>();
        var hash = 17;

        foreach (var item in items)
        {
            hash = (hash * 31) + (item is null ? 0 : EqualityComparer<T>.Default.GetHashCode(item));
        }

        return hash;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }
}
