using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.Builders;

public static class RefList
{
    public delegate TResult ItemBuilder<TSource, TResult>(ref TSource source)
        where TSource : struct;

    private static readonly int[] _sizes =
    [
        4,
        8,
        16,
        32,
        48,
        64,
        80,
        96,
        128,
        192,
        256,
        1024,
        4096,
        16 * 1024
    ];

    /// <summary>
    /// Creates new instance of <see cref="RefList{T}" /> from source using selector. Source is guaranteed to be not
    /// <see langref="null" />.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TData">Item type.</typeparam>
    /// <param name="list">Source.</param>
    /// <param name="selector">Selector</param>
    private static RefList<TData> CreateInternal<TSource, TData>(IReadOnlyList<TSource> list, Func<TSource, TData> selector)
        where TData : struct
    {
        var count = list.Count;
        var data = new TData[NextCapacity(count)];
        for (var i = 0; i < list.Count; ++i)
        {
            data[i] = selector(list[i]);
        }
        return new(data, count);
    }

    /// <summary>
    /// Creates new instance of <see cref="RefList{T}" /> from source using selector. Source is guaranteed to be not
    /// <see langref="null" />.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TData">Item type.</typeparam>
    /// <param name="list">Source.</param>
    /// <param name="selector">Selector</param>
    private static RefList<TData> CreateInternal<TSource, TData>(IEnumerable<TSource> source, Func<TSource, TData> selector)
        where TData : struct
    {
        if (source is IReadOnlyList<TSource> list)
        {
            return CreateInternal(list, selector);
        }
        if (source is IReadOnlyCollection<TSource> collection)
        {
            var count = collection.Count;
            var data = new TData[NextCapacity(count)];
            var i = 0;
            foreach (var item in collection)
            {
                data[i++] = selector(item);
            }
            return new(data, count);
        }
        return CreateInternal(source.ToList(), selector);
    }

    internal static int NextCapacity(int value)
    {
        foreach (var candidate in _sizes)
        {
            if (value <= candidate)
            {
                return candidate;
            }
        }
        return value;
    }

    public static RefList<TData> Create<TSource, TData>(IReadOnlyCollection<TSource> source, Func<TSource, TData> selector)
        where TData : struct
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        var data = new TData[NextCapacity(source.Count)];
        var count = 0;
        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            data[count] = selector(enumerator.Current);
            ++count;
        }
        return new RefList<TData>(data, count);
    }

    public static RefList<TData> Create<TSource, TData>(IEnumerable<TSource> source, Func<TSource, TData> selector)
        where TData : struct
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        return CreateInternal(source, selector);
    }

    public static RefList<TData> CreateOrEmpty<TSource, TData>(IEnumerable<TSource>? source, Func<TSource, TData> selector)
        where TData : struct
    {
        if (source is null)
        {
            return Empty<TData>();
        }
        return CreateInternal(source, selector);
    }

    [return: NotNullIfNotNull(nameof(source))]
    public static RefList<TData>? CreateOrDefault<TSource, TData>(IEnumerable<TSource>? source, Func<TSource, TData> selector)
        where TData : struct
    {
        if (source is null)
        {
            return default;
        }
        return CreateInternal(source, selector);
    }

    public static RefList<TData> Empty<TData>()
        where TData : struct
        => new(4);
}

public class RefList<T> : IEnumerable<T>
    where T : struct
{
#if NET6_0_OR_GREATER
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref struct Enumerator(RefList<T> source)
    {
        private RefList<T> Source { get; } = source;

        private int Index { get; set; } = -1;

        public readonly ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (Index < 0 || Index >= Source.Count)
                {
                    return ref Unsafe.NullRef<T>();
                }
                return ref Source[Index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var nextIndex = Index + 1;
            if (nextIndex == Source.Count)
            {
                return false;
            }
            Index = nextIndex;
            return true;
        }
    }
#endif

    private T[] _data;

    public int Capacity => _data.Length;

    public int Count { get; private set; }

    public ref T this[int index]
    {
        get
        {
            if (index < Count)
            {
                return ref _data[index];
            }
            throw new IndexOutOfRangeException();
        }
    }

    internal RefList(T[] data, int count)
    {
        _data = data;
        Count = count;
    }

    public RefList(int capacity)
    {
        _data = new T[RefList.NextCapacity(capacity)];
        Count = 0;
    }

    public RefList(IReadOnlyCollection<T> items)
        : this(items.Count)
    {
        using var enumerator = items.GetEnumerator();
        while (enumerator.MoveNext())
        {
            _data[Count] = enumerator.Current;
            ++Count;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable<T>)this).GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
        {
            yield return _data[i];
        }
    }

    private void EnsureSize(int desired)
    {
        if (desired <= Capacity)
        {
            return;
        }
        var newSize = RefList.NextCapacity(desired);
        var newData = new T[newSize];
        for (var i = 0; i < Count; ++i)
        {
            newData[i] = _data[i];
        }
        _data = newData;
    }

    private ref T AddUninitialized()
    {
        EnsureSize(Count + 1);
        ref T item = ref _data[Count];
        ++Count;
        return ref item;
    }

    public void Add(T item)
        => AddUninitialized() = item;

    public ref T AddAndGetRef(T item)
    {
        ref T newItem = ref AddUninitialized();
        newItem = item;
        return ref newItem;
    }

    public void Clear()
    {
        Count = 0;
    }

#if NET6_0_OR_GREATER
    public Enumerator GetEnumerator()
        => new(this);
#endif

    public void Insert(int index, T item)
    {
        if (0 > index)
        {
            throw new IndexOutOfRangeException();
        }
        if (index >= Count)
        {
            Add(item);
        }
        else
        {
            EnsureSize(Count + 1);
            for (var i = Count - 1; i >= index; --i)
            {
                _data[i + 1] = _data[i];
            }
            _data[index] = item;
            ++Count;
        }
    }

    public int IndexOf(T item)
        => Array.IndexOf(_data, item);

    public int FindIndex(Predicate<T> predicate)
        => Array.FindIndex(_data, predicate);

#if NET6_0_OR_GREATER
    public ref T Find(RefListFindDelegate<T> predicate)
    {
        foreach (ref T item in this)
        {
            if (predicate(in item))
            {
                return ref item;
            }
        }
        return ref Unsafe.NullRef<T>();
    }

    public ref T FindOrAdd(RefListFindDelegate<T> predicate, out bool found)
    {
        foreach (ref T item in this)
        {
            if (predicate(in item))
            {
                found = true;
                return ref item;
            }
        }
        found = false;
        return ref AddUninitialized();
    }

    public ref T FindOrAdd(RefListFindDelegate<T> predicate)
        => ref FindOrAdd(predicate, out _);
#endif

    public void RemoveAt(int index)
    {
        if (0 > index || index >= Count)
        {
            throw new IndexOutOfRangeException();
        }
        for (var i = index + 1; i < Count; ++i)
        {
            _data[i - 1] = _data[i];
        }
        --Count;
    }

#if NET6_0_OR_GREATER
    public int RemoveAt(IReadOnlySet<int> indices)
    {
        var removed = 0;
        for (var i = 0; i < Count; ++i)
        {
            if (indices.Contains(i))
            {
                ++removed;
                var k = i;
                for (var j = i + 1; j < Count; ++j)
                {
                    if (indices.Contains(j))
                    {
                        ++removed;
                    }
                    else
                    {
                        _data[k++] = _data[j];
                    }
                }
                break;
            }
        }
        Count -= removed;
        return removed;
    }
#endif

    public int RemoveAll(RefListFindDelegate<T> predicate)
    {
        var removed = 0;
        for (var i = 0; i < Count; ++i)
        {
            if (predicate(in _data[i]))
            {
                ++removed;
                var k = i;
                for (var j = i + 1; j < Count; ++j)
                {
                    if (predicate(in _data[j]))
                    {
                        ++removed;
                    }
                    else
                    {
                        _data[k++] = _data[j];
                    }
                }
                break;
            }
        }
        Count -= removed;
        return removed;
    }

    public void Swap(int index1, int index2)
    {
        if (index1 < 0 || index1 >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index1));
        }
        if (index2 < 0 || index2 >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index2));
        }
        if (index1 == index2)
        {
            return;
        }
        (_data[index2], _data[index1]) = (_data[index1], _data[index2]);
    }

    public IReadOnlyList<TResult> Build<TResult>(RefList.ItemBuilder<T, TResult> builder)
    {
        var result = new List<TResult>(Count);
        for (var i = 0; i < Count; ++i)
        {
            result.Add(builder(ref _data[i]));
        }
        return result;
    }

    public IReadOnlyList<TResult> BuildOptional<TResult>(RefList.ItemBuilder<T, TResult?> builder)
        where TResult : class
    {
        var result = new List<TResult>(Count);
        for (var i = 0; i < Count; ++i)
        {
            var item = builder(ref _data[i]);
            if (item is not null)
            {
                result.Add(item);
            }
        }
        return result;
    }
}