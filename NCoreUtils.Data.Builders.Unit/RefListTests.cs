using System.Collections;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.Builders.Unit;

/// <summary>
/// RefList related tests
/// </summary>
public class RefListTests
{
    /// <summary>
    /// Some class used for testing.
    /// </summary>
    public class SomeData(long i64, double f64)
    {
        /// <summary>
        /// Some integer value.
        /// </summary>
        public long I64 { get; } = i64;

        /// <summary>
        /// Some float value.
        /// </summary>
        public double F64 { get; } = f64;
    }

    /// <summary>
    /// Some struct used for testing.
    /// </summary>
    public struct SomeBuilder(long i64, double f64)
    {
        /// <summary>
        /// Some integer value.
        /// </summary>
        public long I64 { get; set; } = i64;

        /// <summary>
        /// Some float value.
        /// </summary>
        public double F64 { get; set; } = f64;
    }

    private sealed class ArrayAsListWrapper<T>(T[] source) : IReadOnlyList<T>
    {
        public T this[int index] => source[index];

        public int Count => source.Length;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)source).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => source.GetEnumerator();
    }

    private sealed class ArrayAsCollectionWrapper<T>(T[] source) : IReadOnlyCollection<T>
    {
        public int Count => source.Length;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)source).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => source.GetEnumerator();
    }

    private sealed class ArrayAsEnumerableWrapper<T>(T[] source) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)source).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => source.GetEnumerator();
    }

    /// <summary>
    /// Tests initialization.
    /// </summary>
    [Fact]
    public void Initialization()
    {
        SomeData[] arraySource = [ new (12, 12.0), new(5, 5.0)  ];
        List<SomeData> listSource = [ new (12, 12.0), new(5, 5.0)  ];
        var mappedSource = new int[] { 12, 5 }.Select(i => new SomeData(i, i));
        var wrappedListSource = new ArrayAsListWrapper<SomeData>(arraySource);
        var wrappedCollectionSource = new ArrayAsCollectionWrapper<SomeData>(arraySource);
        var wrappedEnumerableSource = new ArrayAsEnumerableWrapper<SomeData>(arraySource);
        var rl0 = RefList.Create((IEnumerable<SomeData>)arraySource, Create);
        var rl1 = RefList.Create((IEnumerable<SomeData>)listSource, Create);
        var rl2 = RefList.Create(mappedSource, Create);
        var rl3 = RefList.Create((IEnumerable<SomeData>)wrappedListSource, Create);
        var rl4 = RefList.Create(wrappedEnumerableSource, Create);
        var rl5 = RefList.Create(listSource, Create);
        var rl6 = RefList.Create(wrappedCollectionSource, Create);
        DoCheck(rl0);
        DoCheck(rl1);
        DoCheck(rl2);
        DoCheck(rl3);
        DoCheck(rl4);
        DoCheck(rl5);
        DoCheck(rl6);
        Assert.Equal(4, RefList.Empty<SomeBuilder>().Capacity);
        var rl7 = RefList.CreateOrEmpty(listSource, Create);
        DoCheck(rl7);
        Assert.Equal(0, RefList.CreateOrEmpty<SomeData, SomeBuilder>(null, Create).Count);
        var rl8 = RefList.CreateOrDefault(listSource, Create);
        DoCheck(rl8);
        Assert.Null(RefList.CreateOrDefault<SomeData, SomeBuilder>(null, Create));

        static void DoCheck(RefList<SomeBuilder> rl)
        {
            Assert.Equal(2, rl.Count);
            Assert.Equal(12, rl[0].I64);
            Assert.Equal(12, rl[0].F64);
            Assert.Equal(5, rl[1].I64);
            Assert.Equal(5, rl[1].F64);
        }

        static SomeBuilder Create(SomeData data) => new(data.I64, data.F64);
    }

    /// <summary>
    /// Tests removal
    /// </summary>
    [Fact]
    public void RemoveAll()
    {
        var list = new RefList<int>(24);
        for (var i = 0; i < 24; ++i)
        {
            list.Add(i);
        }
        Assert.Equal(12, list.RemoveAll((in int value) => value % 2 == 0));
        for (var i = 0; i < 12; ++i)
        {
            Assert.Equal(i * 2 + 1, list[i]);
        }
    }

    /// <summary>
    /// Tests removal
    /// </summary>
    [Fact]
    public void RemoveAt()
    {
        {
            var list = new RefList<int>(24);
            for (var i = 0; i < 24; ++i)
            {
                list.Add(i);
            }
            Assert.Equal(12, list.RemoveAt(new HashSet<int>
            {
                0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26
            }));
            for (var i = 0; i < 12; ++i)
            {
                Assert.Equal(i * 2 + 1, list[i]);
            }
        }
        {
            var list = new RefList<int>(24);
            for (var i = 0; i < 24; ++i)
            {
                list.Add(i);
            }
            list.RemoveAt(2);
            for (var i = 0; i < 23; ++i)
            {
                Assert.Equal(i < 2 ? i : i + 1, list[i]);
            }
        }
    }

    /// <summary>
    /// Tests enumeration
    /// </summary>
    [Fact]
    public void Enumerate()
    {
        var list = new RefList<int>(24);
        for (var i = 0; i < 24; ++i)
        {
            list.Add(i);
        }
        Assert.Equal(12, list.IndexOf(12));
        Assert.Equal(12, list.FindIndex(i => i == 12));
        var j = 0;
        foreach (ref var item in list)
        {
            Assert.Equal(item, j);
            ++j;
        }
        Assert.True(Unsafe.IsNullRef(ref list.GetEnumerator().Current));
        j = 0;
        foreach (var item in (IEnumerable<int>)list)
        {
            Assert.Equal(item, j);
            ++j;
        }
    }

    /// <summary>
    /// Tests resizing
    /// </summary>
    [Fact]
    public void Resize()
    {
        var list = new RefList<int>(2);
        for (var i = 0; i < 24; ++i)
        {
            list.Add(i);
        }
        var j = 0;
        foreach (ref var item in list)
        {
            Assert.Equal(item, j);
            ++j;
        }
        ref var x = ref list.AddAndGetRef(88);
        Assert.Equal(88, list[^1]);
        x = 24;
        j = 0;
        foreach (ref var item in list)
        {
            Assert.Equal(item, j);
            ++j;
        }
        list.Clear();
        Assert.Empty(list);
    }

    /// <summary>
    /// Tests insertion
    /// </summary>
    [Fact]
    public void Insert()
    {
        var list = new RefList<int>(24);
        for (var i = 0; i < 22; ++i)
        {
            list.Add(i);
        }
        list.Insert(3, 45);
        list.Insert(10, 50);
        list.Insert(100, 22);
        for (var i = 0; i < 24; ++i)
        {
            if (i < 3)
            {
                Assert.Equal(i, list[i]);
            }
            else if (i == 3)
            {
                Assert.Equal(45, list[i]);
            }
            else if (i < 10)
            {
                Assert.Equal(i - 1, list[i]);
            }
            else if (i == 10)
            {
                Assert.Equal(50, list[i]);
            }
            else
            {
                Assert.Equal(i - 2, list[i]);
            }
        }
    }

    /// <summary>
    /// Tests negative branches
    /// </summary>
    [Fact]
    public void Failures()
    {
        var list = new RefList<int>(4);
        for (var i = 0; i < 24; ++i)
        {
            list.Add(i);
        }
        var rangeExn = Assert.Throws<ArgumentOutOfRangeException>(() => list[48]);
        Assert.Equal("index", rangeExn.ParamName);
        rangeExn = Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-2, default));
        Assert.Equal("index", rangeExn.ParamName);
        rangeExn = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-2));
        Assert.Equal("index", rangeExn.ParamName);
        rangeExn = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(200));
        Assert.Equal("index", rangeExn.ParamName);
        rangeExn = Assert.Throws<ArgumentOutOfRangeException>(() => list.Swap(-1, 200));
        Assert.Equal("index1", rangeExn.ParamName);
        rangeExn = Assert.Throws<ArgumentOutOfRangeException>(() => list.Swap(200, -1));
        Assert.Equal("index1", rangeExn.ParamName);
        rangeExn = Assert.Throws<ArgumentOutOfRangeException>(() => list.Swap(0, 200));
        Assert.Equal("index2", rangeExn.ParamName);
        rangeExn = Assert.Throws<ArgumentOutOfRangeException>(() => list.Swap(0, -1));
        Assert.Equal("index2", rangeExn.ParamName);
    }

    /// <summary>
    /// Tests searching elements.
    /// </summary>
    [Fact]
    public void Search()
    {
        var list = new RefList<int>(4);
        for (var i = 0; i < 24; ++i)
        {
            list.Add(i);
        }
        Assert.Equal(1, list.Find((in i) => i % 2 != 0));
        Assert.True(Unsafe.IsNullRef(ref list.Find((in i) => i > 200)));
        Assert.Equal(1, list.FindOrAdd((in i) => i % 2 != 0));
        list.FindOrAdd((in i) => i >= 24) = 24;
        for (var i = 0; i < 25; ++i)
        {
            Assert.Equal(i, list[i]);
        }
    }

    /// <summary>
    /// Tests item swapping.
    /// </summary>
    [Fact]
    public void Swap()
    {
        var list = new RefList<int>(4);
        for (var i = 0; i < 4; ++i)
        {
            list.Add(i);
        }
        list.Swap(1, 2);
        Assert.Equal(0, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(1, list[2]);
        Assert.Equal(3, list[3]);
        Assert.Equal(4, list.Count);
        list.Swap(1, 1);
        Assert.Equal(0, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(1, list[2]);
        Assert.Equal(3, list[3]);
    }

    /// <summary>
    /// Used for testing.
    /// </summary>
    /// <param name="Value">Value to compare</param>
    public record Int32Box(int Value);

    private static Int32Box I(int value) => new(value);

    /// <summary>
    /// Tests building.
    /// </summary>
    [Fact]
    public void Build()
    {
        var list = new RefList<int>(4);
        for (var i = 0; i < 4; ++i)
        {
            list.Add(i);
        }
        Assert.Equal(list.Build((ref item) => item), [0, 1, 2, 3]);
        Assert.Equal(list.BuildOptional((ref item) => new Int32Box(item)), [I(0), I(1), I(2), I(3)]);
        Assert.Equal(list.BuildOptional((ref item) => item % 2 == 0 ? new Int32Box(item) : default), [I(0), I(2)]);
    }
}