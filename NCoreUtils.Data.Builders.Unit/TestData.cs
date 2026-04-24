namespace NCoreUtils.Data;

/// <summary>
/// Subdata (item) for testing.
/// </summary>
[HasBuilder]
public class TestSubdata
{
    /// <summary>
    /// <c>A</c> property.
    /// </summary>
    public string A { get; }

    /// <summary>
    /// <c>B</c> property.
    /// </summary>
    public string? B { get; }

    /// <summary>
    /// Initializes new instance of subdata.
    /// </summary>
    /// <param name="a">value</param>
    /// <param name="b">value</param>
    public TestSubdata(string a, string? b)
    {
        A = a;
        B = b;
    }
}

/// <summary>
/// Nested class for testing.
/// </summary>
[HasBuilder]
public class NestedData
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static NestedData Empty { get; } = new(default);

    /// <summary>
    /// string property
    /// </summary>
    public string? Str { get; }

    /// <summary>
    /// Initializes new instance of nested class
    /// </summary>
    /// <param name="str">property value</param>
    public NestedData(string? str)
        => Str = str;
}

/// <summary>
/// Nested class with manual (not generated) builder
/// </summary>
public class ManualNestedData
{
    /// <summary>
    /// string property
    /// </summary>
    public string? Str { get; }

    /// <summary>
    /// Initializes new instance of nested class
    /// </summary>
    /// <param name="str">property value</param>
    public ManualNestedData(string? str)
        => Str = str;
}

/// <summary>
/// Root class for testing
/// </summary>
[HasBuilder]
public class TestData
{
    /// <summary>
    /// list of subdata items
    /// </summary>
    public IReadOnlyList<TestSubdata> Sub { get; }

    /// <summary>
    /// list of strings
    /// </summary>
    public IReadOnlyList<string> Strings { get; }

    /// <summary>
    /// list of int32s
    /// </summary>
    [BuilderPropertyName("Integers")]
    public IReadOnlyList<int> Ints { get; }

    /// <summary>
    /// Ignored property
    /// </summary>
    [BuilderIgnore]
    public int Sum => Ints.Sum(static x => x);

    /// <summary>
    /// Nested property --> converts to field
    /// </summary>
    public NestedData Nested { get; }

    /// <summary>
    /// Nested property with manual builder --> converts to field
    /// </summary>
    public ManualNestedData ManualNested { get; }

    /// <summary>
    /// Simple property
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Initializes new instace of test class
    /// </summary>
    /// <param name="sub"></param>
    /// <param name="strings"></param>
    /// <param name="ints"></param>
    /// <param name="nested"></param>
    /// <param name="manualNested"></param>
    /// <param name="count"></param>
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

/// <summary>
/// Record for testing
/// </summary>
/// <param name="Number">Value</param>
/// <param name="String">Value</param>
[HasBuilder]
public record TestRecord(int Number, string? String);