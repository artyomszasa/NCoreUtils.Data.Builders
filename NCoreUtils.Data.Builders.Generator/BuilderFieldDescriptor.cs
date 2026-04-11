using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal sealed class BuilderFieldDescriptor(
    TypeDescriptor sourceType,
    TypeDescriptor sourcePropertyType,
    string sourcePropertyName,
    string? constructorParameterName,
    bool hasDefaultValueMethod,
    ExpressionSyntax defaultValueSyntax,
    bool hasInitializerMethod,
    bool hasBuildMethod,
    TypeDescriptor type,
    string fieldName)
    : BuilderMemberDescriptor(sourceType, sourcePropertyType, sourcePropertyName, constructorParameterName, hasDefaultValueMethod, defaultValueSyntax, hasInitializerMethod, hasBuildMethod)
    , IEquatable<BuilderFieldDescriptor>
{
    private IdentifierNameSyntax? _fieldIdentifier;

    public override TypeDescriptor PublicType => Type;

    /// <summary>
    /// Type of the described field.
    /// </summary>
    public TypeDescriptor Type { get; } = type;

    /// <summary>
    /// Fully qualified name of the type of the described field.
    /// </summary>
    public string QualifiedTypeName => Type.QualifiedName;

    /// <summary>
    /// Name of the described field.
    /// </summary>
    public string FieldName { get; } = fieldName;

    public override IdentifierNameSyntax FieldIdentifier => _fieldIdentifier ??= IdentifierName(FieldName);

    protected override ExpressionSyntax CreateInitializerSyntax()
    {
        return InvocationExpression(
            IdentifierName(Names.InitializeMethod(FieldName)),
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
            IdentifierName(Names.BuildMethod(FieldName)),
            ArgumentList(SingletonSeparatedList(
                Argument(default, BuilderEmitter.Keywords.In, FieldIdentifier)
            ))
        );
    }

    #region equality

    public bool Equals([NotNullWhen(true)] BuilderFieldDescriptor? other)
        => ReferenceEquals(this, other)
            || (other is not null
                && SourcePropertyType == other.SourcePropertyType
                && StringComparer.Ordinal.Equals(SourcePropertyName, other.SourcePropertyName)
                && StringComparer.Ordinal.Equals(ConstructorParameterName, other.ConstructorParameterName)
                && HasDefaultValueMethod == other.HasDefaultValueMethod
                && HasInitializerMethod == other.HasInitializerMethod
                && HasBuildMethod == other.HasBuildMethod
                && Type == other.Type
                && StringComparer.Ordinal.Equals(FieldName, other.FieldName));

    public override bool Equals([NotNullWhen(true)] object? obj)
        => Equals(obj as BuilderFieldDescriptor);

    public override bool Equals([NotNullWhen(true)] BuilderMemberDescriptor? other)
        => Equals(other as BuilderFieldDescriptor);

    protected override int DoGetHashCode()
        // FIXME: better hash
        => StringComparer.Ordinal.GetHashCode(SourcePropertyName) ^ Type.GetHashCode();

    public override int GetHashCode()
        => DoGetHashCode();

    #endregion
}
