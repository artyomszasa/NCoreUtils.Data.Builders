using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal partial class TypeDescriptor(
    bool isValueType,
    bool hasNullableAnnotation,
    string qualifiedName,
    string safeName,
    bool isBuilder,
    bool isRefList,
    bool isStringList,
    bool isInt32List,
    TypeDescriptor? elementType)
    : IEquatable<TypeDescriptor>
{
    public static bool operator==(TypeDescriptor? a, TypeDescriptor? b)
        => a is null
            ? b is null
            : a.Equals(b);

    public static bool operator!=(TypeDescriptor? a, TypeDescriptor? b)
        => a is null
            ? b is not null
            : !a.Equals(b);

    private TypeSyntax? _syntax;

    public bool IsValueType { get; } = isValueType;

    public bool HasNullableAnnotation { get; } = hasNullableAnnotation;

    public string QualifiedName { get; } = qualifiedName;

    public string SafeName { get; } = safeName;

    public bool IsBuilder { get; } = isBuilder;

    public TypeDescriptor? ElementType { get; } = elementType switch
    {
        null when isRefList => throw new ArgumentNullException(nameof(elementType)),
        var v => v
    };

    // FIXME: enum?

    [MemberNotNullWhen(true, nameof(ElementType))]
    public bool IsRefList { get; } = isRefList;

    public bool IsStringList { get; } = isStringList;

    public bool IsInt32List { get; } = isInt32List;

    public TypeSyntax Syntax => _syntax ??= ParseTypeName(QualifiedName);

    public bool Equals([NotNullWhen(true)] TypeDescriptor? other)
        => ReferenceEquals(this, other)
            || (other is not null
                && IsValueType == other.IsValueType
                && HasNullableAnnotation == other.HasNullableAnnotation
                && StringComparer.Ordinal.Equals(QualifiedName, other.QualifiedName)
                && IsRefList == other.IsRefList
                && IsStringList == other.IsStringList
                && IsInt32List == other.IsInt32List);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => Equals(obj as TypeDescriptor);

    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(QualifiedName);
}
