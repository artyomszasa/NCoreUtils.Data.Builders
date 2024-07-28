namespace NCoreUtils.Data.Builders;

public struct ManualNestedDataBuilder
{
    private string? _str;

    public string? Str { readonly get => _str; set => _str = value; }

    public ManualNestedDataBuilder(ManualNestedData source)
    {
        _str = source.Str;
    }

    public readonly ManualNestedData Build() => new ManualNestedData(_str);
}