using System;
using System.Collections.Generic;

namespace NCoreUtils.Data.Builders;

public interface IRefList<T>
    where T : struct
{
    int Count { get; }

    ref T this[int index] { get; }

    void Add(T item);

    ref T AddAndGetRef(T item);

    void Clear();

    void Insert(int index, T item);

#if NET6_0_OR_GREATER
    ref T Find(RefListFindDelegate<T> predicate);

    ref T FindOrAdd(RefListFindDelegate<T> predicate, out bool found);

    ref T FindOrAdd(RefListFindDelegate<T> predicate)
        => ref FindOrAdd(predicate, out _);
#endif

    void RemoveAt(int index);

#if NET6_0_OR_GREATER
    int RemoveAt(IReadOnlySet<int> indices)
    {
        if (indices.Count == 0)
        {
            return 0;
        }
        var originalCount = Count;
        Span<int> ixs = stackalloc int[indices.Count];
        CopyTo(indices, ixs);
        MemoryExtensions.Sort(ixs, static (a, b) => -1 * Comparer<int>.Default.Compare(a, b));
        foreach (var ix in ixs)
        {
            RemoveAt(ix);
        }
        return originalCount - Count;

        static void CopyTo(IReadOnlySet<int> indices, Span<int> buffer)
        {
            if (indices is HashSet<int> ixs)
            {
                var enumerator = ixs.GetEnumerator();
                for (var i = 0; i < buffer.Length; ++i)
                {
                    _ = enumerator.MoveNext();
                    buffer[i] = enumerator.Current;
                }
            }
            else
            {
                using var enumerator = indices.GetEnumerator();
                for (var i = 0; i < buffer.Length; ++i)
                {
                    _ = enumerator.MoveNext();
                    buffer[i] = enumerator.Current;
                }
            }
        }
    }
#endif

    int RemoveAll(RefListFindDelegate<T> predicate);

    void Swap(int index1, int index2);

#if NET6_0_OR_GREATER
    IReadOnlyList<TResult> Build<TResult>(RefList.ItemBuilder<T, TResult> builder)
    {
        var result = new List<TResult>(Count);
        for (var i = 0; i < Count; ++i)
        {
            result.Add(builder(ref this[i]));
        }
        return result;
    }
#else
    IReadOnlyList<TResult> Build<TResult>(RefList.ItemBuilder<T, TResult> builder);
#endif
}