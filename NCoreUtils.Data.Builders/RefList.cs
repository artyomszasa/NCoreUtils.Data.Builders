using System;
using System.Collections;
using System.Collections.Generic;

namespace NCoreUtils.Data.Builders
{
    public static class RefList
    {
        public delegate TResult ItemBuilder<TSource, TResult>(ref TSource source)
            where TSource : struct;

        private static readonly int[] _sizes =
        {
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
        };

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

        public static RefList<TData> Empty<TData>()
            where TData : struct
            => new RefList<TData>(4);
    }

    public class RefList<T> : IEnumerable<T>
        where T : struct
    {


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
            => GetEnumerator();

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

        public void Add(T item)
        {
            EnsureSize(Count + 1);
            _data[Count] = item;
            ++Count;
        }

        public void Clear()
        {
            Count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; ++i)
            {
                yield return _data[i];
            }
        }

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
            var tmp = _data[index1];
            _data[index1] = _data[index2];
            _data[index2] = tmp;
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
                if (!(item is null))
                {
                    result.Add(item);
                }
            }
            return result;
        }
    }
}