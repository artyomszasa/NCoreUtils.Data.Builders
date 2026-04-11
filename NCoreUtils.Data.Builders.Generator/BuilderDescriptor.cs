using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal class BuilderDescriptor(
    string builderNamespace,
    string builderName,
    bool generateDocumentationComment,
    TypeDescriptor sourceType,
    ImmutableArray<BuilderMemberDescriptor> members)
    : IEquatable<BuilderDescriptor>
{
    private TypeSyntax? _builderTypeSyntax;

    private DocumentationCommentTriviaSyntax? _documentationComment;

    public string BuilderNamespace { get; } = builderNamespace;

    /// <summary>
    /// Builder name.
    /// </summary>
    public string BuilderName { get; } = builderName;

    /// <summary>
    ///
    /// </summary>
    public bool GenerateDocumentationComment { get; } = generateDocumentationComment;

    public TypeSyntax BuilderTypeSyntax => _builderTypeSyntax ??= IdentifierName(BuilderName);

    /// <summary>
    /// Source type which is being handled by the enerated builder.
    /// </summary>
    public TypeDescriptor SourceType { get; } = sourceType;

    public ImmutableArray<BuilderMemberDescriptor> Members { get; } = members;

    /// <summary>
    /// If builder has non-generated partial class then documentation should be there (thus this property is null),
    /// otherwise default documentation is retrieved via this property.
    /// </summary>
    public DocumentationCommentTriviaSyntax? DocumentationComment
    {
        get
        {
            if (GenerateDocumentationComment)
            {
                return _documentationComment ??= DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    List(new XmlNodeSyntax[]
                    {
                        XmlText(XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList())),
                        XmlElement("summary", List(new XmlNodeSyntax[]
                        {
                            XmlText("Provides mutable builder for "),
                            XmlEmptyElement(
                                XmlName("see"),
                                List(new XmlAttributeSyntax[]
                                {
                                    XmlCrefAttribute(TypeCref(SourceType.Syntax))
                                })
                            ),
                            XmlText($".")
                        })),
                        XmlText(XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()))
                    }),
                    Token(SyntaxKind.EndOfDocumentationCommentToken)
                ).WithTrailingTrivia(EndOfLine("\r\n"));
            }
            return default;
        }
    }

    public bool Equals(BuilderDescriptor other)
        => GenerateDocumentationComment == other.GenerateDocumentationComment
            && StringComparer.Ordinal.Equals(BuilderNamespace, other.BuilderNamespace)
            && StringComparer.Ordinal.Equals(BuilderName, other.BuilderName)
            && SourceType == other.SourceType
            && Members.SequenceEqual(other.Members);
}