using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;


internal readonly struct BuilderMemberData(bool? isNestedBuilder, string? nestedBuilderQualifiedName, ITypeSymbol? type)
{
    public static BuilderMemberData FromType(ITypeSymbol type)
        => new(default, default, type);

    public static BuilderMemberData FromBuilderType(ITypeSymbol type)
        => new(true, default, type);

    public static BuilderMemberData FromBuilderType(string qualifiedName)
        => new(true, qualifiedName, default);

    public bool? IsNestedBuilder { get; } = isNestedBuilder;

    public string? NestedBuilderQualifiedName { get; } = nestedBuilderQualifiedName;

    public ITypeSymbol? Type { get; } = type;

    public bool IsEmpty => string.IsNullOrEmpty(NestedBuilderQualifiedName) && Type is null;

    public override string ToString()
    {
        if (IsNestedBuilder == true)
        {
            if (!string.IsNullOrEmpty(NestedBuilderQualifiedName))
            {
                return $"Builder (gen): {NestedBuilderQualifiedName}";
            }
            if (Type is not null)
            {
                return $"Builder: {Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";
            }
            return "Builder: ???";
        }
        if (Type is not null)
        {
            return Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        return "???";
    }
}