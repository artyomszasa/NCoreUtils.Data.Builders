using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal sealed class BuilderPropertyDescriptor(
    TypeDescriptor sourceType,
    TypeDescriptor sourcePropertyType,
    string sourcePropertyName,
    string? constructorParameterName,
    bool hasDefaultValueMethod,
    ExpressionSyntax defaultValueSyntax,
    bool hasInitializerMethod,
    bool hasBuildMethod,
    TypeDescriptor fieldType,
    TypeDescriptor propertyType,
    string fieldName,
    string propertyName
)
    : BuilderMemberDescriptor(sourceType, sourcePropertyType, sourcePropertyName, constructorParameterName, hasDefaultValueMethod, defaultValueSyntax, hasInitializerMethod, hasBuildMethod)
    , IEquatable<BuilderPropertyDescriptor>
{
    private IdentifierNameSyntax? _fieldIdentifier;

    public override TypeDescriptor PublicType => PropertyType;

    /// <summary>
    /// Type of the backing field.
    /// </summary>
    public TypeDescriptor FieldType { get; } = fieldType;

    /// <summary>
    /// Type of the backing field.
    /// </summary>
    public TypeDescriptor PropertyType { get; } = propertyType;

    /// <summary>
    /// Fully qualified name of the type of the backing field (which usually is nullable).
    /// </summary>
    public string QualifiedFieldTypeName => FieldType.QualifiedName;

    /// <summary>
    /// Name of the backing field.
    /// </summary>
    public string FieldName { get; } = fieldName;

    public string QualifiedPropertyTypeName => PropertyType.QualifiedName;

    public string PropertyName { get; } = propertyName;

    public override IdentifierNameSyntax FieldIdentifier => _fieldIdentifier ??= IdentifierName(FieldName);

    protected override ExpressionSyntax CreateInitializerSyntax()
    {
        return InvocationExpression(
            IdentifierName(Names.InitializeMethod(PropertyName)),
            ArgumentList(SeparatedList(new []
            {
                Argument(BuilderEmitter.IdentifierNames.source),
                Argument(default, BuilderEmitter.Keywords.Out, FieldIdentifier)
            }))
        );
    }

    protected override ExpressionSyntax CreateBuilderSyntax()
    {
        return InvocationExpression(
            IdentifierName(Names.BuildMethod(PropertyName)),
            ArgumentList(SingletonSeparatedList(
                Argument(default, BuilderEmitter.Keywords.In, FieldIdentifier)
            ))
        );
    }

    #region equality

    public bool Equals([NotNullWhen(true)] BuilderPropertyDescriptor? other)
        => ReferenceEquals(this, other)
            || (other is not null
                && SourcePropertyType == other.SourcePropertyType
                && StringComparer.Ordinal.Equals(SourcePropertyName, other.SourcePropertyName)
                && StringComparer.Ordinal.Equals(ConstructorParameterName, other.ConstructorParameterName)
                && HasDefaultValueMethod == other.HasDefaultValueMethod
                && HasInitializerMethod == other.HasInitializerMethod
                && HasBuildMethod == other.HasBuildMethod
                && FieldType == other.FieldType
                && StringComparer.Ordinal.Equals(FieldName, other.FieldName)
                && PropertyType == other.PropertyType
                && StringComparer.Ordinal.Equals(PropertyName, other.PropertyName));

    public override bool Equals([NotNullWhen(true)] object? obj)
        => Equals(obj as BuilderPropertyDescriptor);

    public override bool Equals([NotNullWhen(true)] BuilderMemberDescriptor? other)
        => Equals(other as BuilderPropertyDescriptor);

    protected override int DoGetHashCode()
        // FIXME: better hash
        => StringComparer.Ordinal.GetHashCode(SourcePropertyName) ^ PropertyType.GetHashCode();

    public override int GetHashCode()
        => DoGetHashCode();

    #endregion
}
