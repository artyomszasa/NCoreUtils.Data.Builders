using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NCoreUtils.Data.Builders.Internal;

namespace NCoreUtils.Data.Builders;

/// <summary>
/// Contains factory methods for creating instances of <see cref="RefList{T}"/>.
/// </summary>
public static class RefList
{
    /// <summary>
    /// Defines a delegate for building an item of type <typeparamref name="TResult"/> from a source item of type <typeparamref name="TSource"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the source item, which must be a struct.</typeparam>
    /// <typeparam name="TResult">The type of the result item.</typeparam>
    /// <param name="source">A reference to the source item.</param>
    /// <returns>The built result item.</returns>
    public delegate TResult ItemBuilder<TSource, TResult>(ref TSource source)
        where TSource : struct;

#if NET6_0_OR_GREATER
    private static ReadOnlySpan<int> NextSizes =>
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

#else

    private static readonly int[] NextSizes =
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

#endif

#if NET6_0_OR_GREATER

#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Unsafe_AreSame<T>(in T ref1, in T ref2)
        => Unsafe.AreSame(in ref1, in ref2);
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Unsafe_AreSame<T>(in T ref1, in T ref2)
        => Unsafe.AreSame(ref Unsafe.AsRef(in ref1), ref Unsafe.AsRef(in ref2));
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref readonly T Unsafe_Add<T>(in T @ref, int n)
        => ref Unsafe.Add(ref Unsafe.AsRef(in @ref), n);

    private static void Initialize_Ref<TSource, TData>(
        ref TData destinationBegin,
        in TSource sourceBegin,
        in TSource sourceEnd,
        Func<TSource, TData> selector)
        where TData : struct
    {
        ref TData destinationIt = ref destinationBegin;
        ref readonly TSource sourceIt = ref sourceBegin;
        while (!Unsafe_AreSame(in sourceIt, in sourceEnd))
        {
            destinationIt = selector(sourceIt);
            destinationIt = ref Unsafe.Add(ref destinationIt, 1);
            sourceIt = ref Unsafe_Add(in sourceIt, 1);
        }
    }

    private static void Initialize_Array<TSource, TData>(
        ref TData destinationBegin,
        TSource[] source,
        Func<TSource, TData> selector)
        where TData : struct
    {
        ref readonly TSource sourceBegin = ref MemoryMarshal.GetArrayDataReference(source);
        ref readonly TSource sourceEnd = ref Unsafe_Add(sourceBegin, source.Length);
        Initialize_Ref(ref destinationBegin, in sourceBegin, in sourceEnd, selector);
    }

    private static void Initialize_List<TSource, TData>(
        ref TData destinationBegin,
        List<TSource> source,
        Func<TSource, TData> selector)
        where TData : struct
    {
        ref readonly TSource sourceBegin = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(source));
        ref readonly TSource sourceEnd = ref Unsafe_Add(sourceBegin, source.Count);
        Initialize_Ref(ref destinationBegin, in sourceBegin, in sourceEnd, selector);
    }


    private static void Initialize<TSource, TData>(TData[] destination, IReadOnlyList<TSource> source, Func<TSource, TData> selector)
        where TData : struct
    {
        switch (source)
        {
            case TSource[] arraySource:
                Initialize_Array(
                    ref MemoryMarshal.GetArrayDataReference(destination),
                    arraySource,
                    selector
                );
                break;
            case List<TSource> listSource:
                Initialize_List(
                    ref MemoryMarshal.GetArrayDataReference(destination),
                    listSource,
                    selector
                );
                break;
            default:
                for (var i = 0; i < source.Count; ++i)
                {
                    destination[i] = selector(source[i]);
                }
                break;
        }
    }

#endif

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
#if NET6_0_OR_GREATER
        Initialize(data, list, selector);
#else
        for (var i = 0; i < count; ++i)
        {
            data[i] = selector(list[i]);
        }
#endif
        return new(data, count);
    }

    /// <summary>
    /// Creates new instance of <see cref="RefList{T}" /> from source using selector. Source is guaranteed to be not
    /// <see langref="null" />. Used when other optimizations are not possible but the element cooountis known.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TData">Item type.</typeparam>
    /// <param name="source">Source.</param>
    /// <param name="count">Element count.</param>
    /// <param name="selector">Selector</param>
    private static RefList<TData> CreateInternal<TSource, TData>(IEnumerable<TSource> source, int count, Func<TSource, TData> selector)
        where TData : struct
    {
        var data = new TData[NextCapacity(count)];
        var i = 0;
        foreach (var item in source)
        {
            data[i++] = selector(item);
        }
        return new(data, count);
    }

    /// <summary>
    /// Creates new instance of <see cref="RefList{T}" /> from source using selector. Source is guaranteed to be not
    /// <see langref="null" />.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TData">Item type.</typeparam>
    /// <param name="source">Source.</param>
    /// <param name="selector">Selector</param>
    private static RefList<TData> CreateInternal<TSource, TData>(IEnumerable<TSource> source, Func<TSource, TData> selector)
        where TData : struct
    {
        if (source is IReadOnlyList<TSource> list)
        {
            return CreateInternal(list, selector);
        }
#if NET6_0_OR_GREATER
        if (Enumerable.TryGetNonEnumeratedCount(source, out var count))
        {
            return CreateInternal(source, count, selector);
        }
#else
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
#endif
        return CreateInternal(source.ToArray(), selector);
    }

    internal static int NextCapacity(int value)
    {
        foreach (var candidate in NextSizes)
        {
            if (value <= candidate)
            {
                return candidate;
            }
        }
        return value;
    }

    /// <summary>
    /// Creates a new <see cref="RefList{TData}"/> from a collection of source items.
    /// </summary>
    /// <typeparam name="TSource">The type of the source items.</typeparam>
    /// <typeparam name="TData">The type of the items in the resulting <see cref="RefList{T}"/>, which must be a struct.</typeparam>
    /// <param name="source">The source collection. Must not be null.</param>
    /// <param name="selector">A function to transform each source item into a <typeparamref name="TData"/> item.</param>
    /// <returns>A new <see cref="RefList{TData}"/> containing the transformed items.</returns>
    public static RefList<TData> Create<TSource, TData>(IReadOnlyCollection<TSource> source, Func<TSource, TData> selector)
        where TData : struct
    {
        Check.NotNull(source);
        if (source is IReadOnlyList<TSource> list)
        {
            return CreateInternal(list, selector);
        }
        return CreateInternal(source, source.Count, selector);
    }

    /// <summary>
    /// Creates a new <see cref="RefList{TData}"/> from an enumerable of source items.
    /// </summary>
    /// <typeparam name="TSource">The type of the source items.</typeparam>
    /// <typeparam name="TData">The type of the items in the resulting <see cref="RefList{T}"/>, which must be a struct.</typeparam>
    /// <param name="source">The source enumerable. Must not be null.</param>
    /// <param name="selector">A function to transform each source item into a <typeparamref name="TData"/> item.</param>
    /// <returns>A new <see cref="RefList{TData}"/> containing the transformed items.</returns>
    public static RefList<TData> Create<TSource, TData>(IEnumerable<TSource> source, Func<TSource, TData> selector)
        where TData : struct
    {
        Check.NotNull(source);
        return CreateInternal(source, selector);
    }

    /// <summary>
    /// Creates a new <see cref="RefList{TData}"/> from a nullable enumerable of source items, returning an empty list if the source is null.
    /// </summary>
    /// <typeparam name="TSource">The type of the source items.</typeparam>
    /// <typeparam name="TData">The type of the items in the resulting <see cref="RefList{T}"/>, which must be a struct.</typeparam>
    /// <param name="source">The source enumerable, which can be null.</param>
    /// <param name="selector">A function to transform each source item into a <typeparamref name="TData"/> item.</param>
    /// <returns>A new <see cref="RefList{TData}"/> containing the transformed items, or an empty list if the source is null.</returns>
    public static RefList<TData> CreateOrEmpty<TSource, TData>(IEnumerable<TSource>? source, Func<TSource, TData> selector)
        where TData : struct
        => source is null
            ? Empty<TData>()
            : CreateInternal(source, selector);

    /// <summary>
    /// Creates a new <see cref="RefList{TData}"/> from a nullable enumerable of source items, returning null if the source is null.
    /// </summary>
    /// <typeparam name="TSource">The type of the source items.</typeparam>
    /// <typeparam name="TData">The type of the items in the resulting <see cref="RefList{T}"/>, which must be a struct.</typeparam>
    /// <param name="source">The source enumerable, which can be null.</param>
    /// <param name="selector">A function to transform each source item into a <typeparamref name="TData"/> item.</param>
    /// <returns>A new <see cref="RefList{TData}"/> containing the transformed items, or null if the source is null.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public static RefList<TData>? CreateOrDefault<TSource, TData>(IEnumerable<TSource>? source, Func<TSource, TData> selector)
        where TData : struct
        => source is null
            ? default
            : CreateInternal(source, selector);

    /// <summary>
    /// Creates an empty <see cref="RefList{TData}"/> with a default initial capacity.
    /// </summary>
    /// <typeparam name="TData">The type of items in the list, which must be a struct.</typeparam>
    /// <returns>An empty <see cref="RefList{TData}"/>.</returns>
    public static RefList<TData> Empty<TData>()
        where TData : struct
        => new(4);
}

/// <summary>
/// Represents a list of value types that can be accessed by reference, similar to <see cref="List{T}"/> but for structs and with by-ref access.
/// </summary>
/// <typeparam name="T">The type of elements in the list. Must be a value type.</typeparam>
public class RefList<T> : IEnumerable<T>
    where T : struct
{
#if NET6_0_OR_GREATER
    /// <summary>
    /// Enumerates the elements of a <see cref="RefList{T}"/>.
    /// </summary>
    /// <param name="source">The <see cref="RefList{T}"/> to enumerate.</param>
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref struct Enumerator(RefList<T> source)
    {
        private RefList<T> Source { get; } = source;

        private int Index { get; set; } = -1;

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
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

        /// <summary>
        /// Advances the enumerator to the next element of the <see cref="RefList{T}"/>.
        /// </summary>
        /// <returns><c>true</c> if the enumerator was successfully advanced to the next element; <c>false</c> if the enumerator has passed the end of the collection.</returns>
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

    /// <summary>
    /// Gets the total number of elements the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity => _data.Length;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="RefList{T}"/>.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets a reference to the element at the specified index.
    /// </summary>
    public ref T this[int index]
    {
        get
        {
            Check.LessThan(index, Count);
            return ref _data[index];
        }
    }

    internal RefList(T[] data, int count)
    {
        _data = data;
        Count = count;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefList{T}"/> class that is empty and has the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The number of elements that the new list can initially store.</param>
    public RefList(int capacity)
    {
        _data = new T[RefList.NextCapacity(capacity)];
        Count = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefList{T}"/> class that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="items">The collection whose elements are copied to the new list.</param>
    [Obsolete("Use factory methods instead")]
    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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

    /// <summary>
    /// Adds an object to the end of the <see cref="RefList{T}"/>.
    /// </summary>
    /// <param name="item">The object to be added to the end of the <see cref="RefList{T}"/>.</param>
    public void Add(T item)
        => AddUninitialized() = item;

    /// <summary>
    /// Adds an object to the end of the <see cref="RefList{T}"/> and returns a reference to the added item.
    /// </summary>
    /// <param name="item">The object to be added.</param>
    /// <returns>A reference to the added item.</returns>
    public ref T AddAndGetRef(T item)
    {
        ref T newItem = ref AddUninitialized();
        newItem = item;
        return ref newItem;
    }

    /// <summary>
    /// Removes all elements from the <see cref="RefList{T}"/>.
    /// </summary>
    public void Clear()
    {
        Count = 0;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="RefList{T}"/>.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> for the <see cref="RefList{T}"/>.</returns>
    public Enumerator GetEnumerator()
        => new(this);
#endif

    /// <summary>
    /// Inserts an element into the <see cref="RefList{T}"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The object to insert.</param>
    public void Insert(int index, T item)
    {
        Check.GreaterThanOrEqual(index, 0);
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

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="RefList{T}"/>.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="RefList{T}"/>. The value can be <c>null</c> for reference types.</param>
    /// <returns>The zero-based index of the first occurrence of <paramref name="item"/> within the entire <see cref="RefList{T}"/>, if found; otherwise, –1.</returns>
    public int IndexOf(T item)
        => Array.IndexOf(_data, item);

    /// <summary>
    /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the entire <see cref="RefList{T}"/>.
    /// </summary>
    /// <param name="predicate">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
    /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="predicate"/>, if found; otherwise, –1.</returns>
    public int FindIndex(Predicate<T> predicate)
        => Array.FindIndex(_data, predicate);

#if NET6_0_OR_GREATER
    /// <summary>
    /// Searches for an element that matches the conditions defined by the specified predicate, and returns a reference to the first occurrence within the entire <see cref="RefList{T}"/>.
    /// </summary>
    /// <param name="predicate">The predicate that defines the conditions of the element to search for.</param>
    /// <returns>A reference to the first element that matches the conditions defined by the specified predicate, if found; otherwise, a null reference.</returns>
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

    /// <summary>
    /// Searches for an element that matches the conditions defined by the specified predicate, and returns a reference to it. If no such element is found, a new element is added and a reference to it is returned.
    /// </summary>
    /// <param name="predicate">The predicate that defines the conditions of the element to search for.</param>
    /// <param name="found">When this method returns, contains <c>true</c> if the element was found; otherwise, <c>false</c>.</param>
    /// <returns>A reference to the found or added element.</returns>
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

    /// <summary>
    /// Searches for an element that matches the conditions defined by the specified predicate, and returns a reference to it. If no such element is found, a new element is added and a reference to it is returned.
    /// </summary>
    /// <param name="predicate">The predicate that defines the conditions of the element to search for.</param>
    /// <returns>A reference to the found or added element.</returns>
    public ref T FindOrAdd(RefListFindDelegate<T> predicate)
        => ref FindOrAdd(predicate, out _);
#endif

    /// <summary>
    /// Removes the element at the specified index of the <see cref="RefList{T}"/>.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        Check.GreaterThanOrEqual(index, 0);
        Check.LessThan(index, Count);
        for (var i = index + 1; i < Count; ++i)
        {
            _data[i - 1] = _data[i];
        }
        --Count;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Removes the elements at the specified indices from the <see cref="RefList{T}"/>.
    /// </summary>
    /// <param name="indices">The set of zero-based indices of the elements to remove.</param>
    /// <returns>The number of elements removed from the <see cref="RefList{T}"/>.</returns>
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

    /// <summary>
    /// Removes all the elements that match the conditions defined by the specified predicate.
    /// </summary>
    /// <param name="predicate">The delegate that defines the conditions of the elements to remove.</param>
    /// <returns>The number of elements removed from the <see cref="RefList{T}"/>.</returns>
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

    /// <summary>
    /// Swaps the elements at the specified indices in the <see cref="RefList{T}"/>.
    /// </summary>
    /// <param name="index1">The index of the first element to swap.</param>
    /// <param name="index2">The index of the second element to swap.</param>
    public void Swap(int index1, int index2)
    {
        Check.GreaterThanOrEqual(index1, 0);
        Check.LessThan(index1, Count);
        Check.GreaterThanOrEqual(index2, 0);
        Check.LessThan(index2, Count);
        if (index1 == index2)
        {
            return;
        }
        (_data[index2], _data[index1]) = (_data[index1], _data[index2]);
    }

    /// <summary>
    /// Creates a read-only list of a specified type from the <see cref="RefList{T}"/>, transforming each element using the provided builder.
    /// </summary>
    /// <typeparam name="TResult">The type of elements in the resulting list.</typeparam>
    /// <param name="builder">The delegate that transforms each element of the <see cref="RefList{T}"/>.</param>
    /// <returns>A read-only list containing the transformed elements.</returns>
    public IReadOnlyList<TResult> Build<TResult>(RefList.ItemBuilder<T, TResult> builder)
    {
        var result = new List<TResult>(Count);
        for (var i = 0; i < Count; ++i)
        {
            result.Add(builder(ref _data[i]));
        }
        return result;
    }

    /// <summary>
    /// Creates a read-only list of a specified reference type from the <see cref="RefList{T}"/>, transforming each element using the provided builder and excluding null results.
    /// </summary>
    /// <typeparam name="TResult">The type of elements in the resulting list, which must be a class.</typeparam>
    /// <param name="builder">The delegate that transforms each element of the <see cref="RefList{T}"/> into a nullable result.</param>
    /// <returns>A read-only list containing the non-null transformed elements.</returns>
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