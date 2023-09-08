using NCoreUtils.Data.Builders;

namespace NCoreUtils.Data;

[HasBuilder]
public class TestSubdata
{
    public string A { get; }

    public string? B { get;}

    public TestSubdata(string a, string? b)
    {
        A = a;
        B = b;
    }
}

[HasBuilder]
public class TestData
{
    public IReadOnlyList<TestSubdata> Sub { get; }

    public IReadOnlyList<string> Strings { get; }

    public IReadOnlyList<int> Ints { get; }

    public int Count { get; }

    public TestData(IReadOnlyList<TestSubdata> sub, IReadOnlyList<string> strings, IReadOnlyList<int> ints, int count)
    {
        Sub = sub;
        Strings = strings;
        Ints = ints;
        Count = count;
    }
}


[HasBuilder]
public record TestRecord(int Number, string? String);