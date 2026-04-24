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
        ArgumentNullException.ThrowIfNull(source);
        _str = source.Str;
    }

    /// <summary>
    /// builder
    /// </summary>
    /// <returns></returns>
    public readonly ManualNestedData Build() => new ManualNestedData(_str);
}