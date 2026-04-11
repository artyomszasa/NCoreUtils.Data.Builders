using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal abstract class BuilderMemberDescriptor(
    TypeDescriptor sourceType,
    TypeDescriptor sourcePropertyType,
    string sourcePropertyName,
    string? constructorParameterName,
    bool hasDefaultValueMethod,
    ExpressionSyntax defaultValueSyntax,
    bool hasInitializerMethod,
    bool hasBuildMethod)
    : IEquatable<BuilderMemberDescriptor>
{
    private DocumentationCommentTriviaSyntax? _documentationComment;

    private ExpressionStatementSyntax? _initializer;

    private ExpressionSyntax? _builder;

    protected bool HasDefaultValueMethod { get; } = hasDefaultValueMethod;

    protected bool HasInitializerMethod { get; } = hasInitializerMethod;

    protected bool HasBuildMethod { get; } = hasBuildMethod;

    public abstract TypeDescriptor PublicType { get; }

    /// <summary>
    /// Identifier of either field itself (nested builder) or backing field (property).
    /// </summary>
    public abstract IdentifierNameSyntax FieldIdentifier { get; }

    public TypeDescriptor SourceType { get; } = sourceType;

    public TypeDescriptor SourcePropertyType { get; } = sourcePropertyType;

    public string SourcePropertyName { get; } = sourcePropertyName;

    public string? ConstructorParameterName { get; } = constructorParameterName;

    [MemberNotNullWhen(true, nameof(ElementType))]
    public bool IsRefList => PublicType.IsRefList;

    public bool IsStringList => PublicType.IsStringList;

    public bool IsInt32List => PublicType.IsInt32List;

    public TypeDescriptor? ElementType => PublicType.ElementType;

    #region computed

    public ExpressionSyntax DefaultValueSyntax { get; } = defaultValueSyntax;

    public DocumentationCommentTriviaSyntax DocumentationComment
        => _documentationComment ??= DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            List(new XmlNodeSyntax[]
            {
                XmlText(XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList())),
                XmlEmptyElement(
                    XmlName("inheritdoc"),
                    List(new XmlAttributeSyntax[]
                    {
                        XmlCrefAttribute(
                            QualifiedCref(
                                SourceType.Syntax,
                                NameMemberCref(ParseName(SourcePropertyName))
                            )
                        )
                    })
                ),
                XmlText(XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()))
            }),
            Token(SyntaxKind.EndOfDocumentationCommentToken)
        ).WithTrailingTrivia(EndOfLine("\r\n"));

    protected abstract ExpressionSyntax CreateInitializerSyntax();

    protected abstract ExpressionSyntax CreateBuilderSyntax();

    public bool TryGetInitializer([MaybeNullWhen(false)] out ExpressionStatementSyntax statement)
    {
        if (HasInitializerMethod)
        {
            statement = _initializer ??= ExpressionStatement(CreateInitializerSyntax());
            return true;
        }
        statement = default;
        return false;
    }

    public bool TryGetBuilder([MaybeNullWhen(false)] out ExpressionSyntax expression)
    {
        if (HasBuildMethod)
        {
            expression = _builder ??= CreateBuilderSyntax();
            return true;
        }
        expression = default;
        return false;
    }

    #endregion

    #region equality

    public static bool operator==(BuilderMemberDescriptor? a, BuilderMemberDescriptor? b)
        => a is null
            ? b is null
            : a.Equals(b);

    public static bool operator!=(BuilderMemberDescriptor? a, BuilderMemberDescriptor? b)
        => a is null
            ? b is not null
            : !a.Equals(b);

    protected abstract int DoGetHashCode();

    public abstract bool Equals([NotNullWhen(true)] BuilderMemberDescriptor? other);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => Equals(obj as BuilderMemberDescriptor);

    public override int GetHashCode()
        => DoGetHashCode();

    #endregion
}
