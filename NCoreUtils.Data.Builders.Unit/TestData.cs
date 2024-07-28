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
public class NestedData
{
    public static NestedData Empty { get; } = new(default);

    public string? Str { get; }

    public NestedData(string? str)
        => Str = str;
}

public class ManualNestedData
{
    public string? Str { get; }

    public ManualNestedData(string? str)
        => Str = str;
}

[HasBuilder]
public class TestData
{
    public IReadOnlyList<TestSubdata> Sub { get; }

    public IReadOnlyList<string> Strings { get; }

    [BuilderPropertyName("Integers")]
    public IReadOnlyList<int> Ints { get; }

    [BuilderIgnore]
    public int Sum => Ints.Sum(static x => x);

    public NestedData Nested { get; }

    public ManualNestedData ManualNested { get; }

    public int Count { get; }

    public TestData(IReadOnlyList<TestSubdata> sub, IReadOnlyList<string> strings, IReadOnlyList<int> ints, NestedData nested, ManualNestedData manualNested, int count)
    {
        Sub = sub;
        Strings = strings;
        Ints = ints;
        Nested = nested;
        ManualNested = manualNested;
        Count = count;
    }
}


[HasBuilder]
public record TestRecord(int Number, string? String);