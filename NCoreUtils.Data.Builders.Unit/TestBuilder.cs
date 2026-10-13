namespace NCoreUtils.Data.Builders;

/// <summary>
/// Manually written builder
/// </summary>
public struct ManualNestedDataBuilder
{
    private string? _str;

    /// <summary>
    /// Property
    /// </summary>
    public string? Str { readonly get => _str; set => _str = value; }

    /// <summary>
    /// Initializer
    /// </summary>
    /// <param name="source"></param>
    public ManualNestedDataBuilder(ManualNestedData source)
    {
        _str = source.Str;
    }

    /// <summary>
    /// builder
    /// </summary>
    /// <returns></returns>
    public readonly ManualNestedData Build() => new ManualNestedData(_str);
}

/// <summary>
/// Builder.
/// </summary>
public partial struct TestDataBuilder
{
    private static void InitializeInitializedCount(TestData source, out int initializedCount)
        => initializedCount = source.InitializedCount;

    private static int BuildInitializedCount(in int initializedCount)
        => initializedCount;

    private static List<List<string?>> GetDefaultListOfNullableStringsValue() => [];
}