using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal partial class BuilderEmitter
{
    private static SyntaxTrivia Tab4 { get; } = Whitespace("    ");

    private static SyntaxTrivia EndOfLine { get; } = EndOfLine("\r\n");

    private static LiteralExpressionSyntax NullLiteral { get; } = LiteralExpression(SyntaxKind.NullLiteralExpression);

    public static LiteralExpressionSyntax DefaultLiteral { get; } = LiteralExpression(SyntaxKind.DefaultLiteralExpression);

    public static class Keywords
    {
        public static SyntaxToken In { get; } = Token(SyntaxKind.InKeyword);

        public static SyntaxToken Out { get; } = Token(SyntaxKind.OutKeyword);

        public static SyntaxToken Private { get; } = Token(SyntaxKind.PrivateKeyword);

        public static SyntaxToken Public { get; } = Token(SyntaxKind.PublicKeyword);

        public static SyntaxToken ReadOnly { get; } = Token(SyntaxKind.ReadOnlyKeyword);

        public static SyntaxToken Ref { get; } = Token(SyntaxKind.RefKeyword);

        public static SyntaxToken Static { get; } = Token(SyntaxKind.StaticKeyword);

        public static SyntaxToken This { get; } = Token(SyntaxKind.ThisKeyword);

    }

    private static class Tokens
    {
        public static SyntaxToken Semicolon { get; } = Token(SyntaxKind.SemicolonToken);
    }

    public static class Identifiers
    {
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static SyntaxToken builder { get; } = Identifier(nameof(builder));

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static SyntaxToken e { get; } = Identifier(nameof(e));

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static SyntaxToken global { get; } = Identifier(nameof(global));

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static SyntaxToken source { get; } = Identifier(nameof(source));

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static SyntaxToken update { get; } = Identifier(nameof(update));

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static SyntaxToken value { get; } = Identifier(nameof(value));

        public static SyntaxToken Build { get; } = Identifier(nameof(Build));

        public static SyntaxToken Empty { get; } = Identifier(nameof(Empty));

        public static SyntaxToken ToArray { get; } = Identifier(nameof(ToArray));
    }

    public static class IdentifierNames
    {
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static IdentifierNameSyntax builder { get; } = IdentifierName(Identifiers.builder);

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static IdentifierNameSyntax e { get; } = IdentifierName(Identifiers.e);

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static IdentifierNameSyntax global { get; } = IdentifierName(Identifiers.global);

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static IdentifierNameSyntax source { get; } = IdentifierName(Identifiers.source);

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static IdentifierNameSyntax update { get; } = IdentifierName(Identifiers.update);

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Property name must match the contained string.")]
        public static IdentifierNameSyntax value { get; } = IdentifierName(Identifiers.value);

        public static IdentifierNameSyntax Build { get; } = IdentifierName(Identifiers.Build);

        public static IdentifierNameSyntax Empty { get; } = IdentifierName(Identifiers.Empty);

        public static IdentifierNameSyntax ToArray { get; } = IdentifierName(Identifiers.ToArray);
    }

    public static class Types
    {
        public static PredefinedTypeSyntax Int32 { get; } = PredefinedType(Token(SyntaxKind.IntKeyword));

        public static PredefinedTypeSyntax String { get; } = PredefinedType(Token(SyntaxKind.StringKeyword));

        public static TypeSyntax StringList { get; } = ParseTypeName("global::System.Collections.Generic.List<string>");

        public static TypeSyntax Int32List { get; } = ParseTypeName("global::System.Collections.Generic.List<int>");

        public static TypeSyntax Array { get; } = ParseTypeName("global::System.Array");

        public static TypeSyntax RefList { get; } = ParseTypeName("global::NCoreUtils.Data.Builders.RefList");

        public static NameSyntax UpdateDelegate(TypeSyntax builderType)
            => QualifiedName(
                QualifiedName(
                    QualifiedName(
                        AliasQualifiedName(
                            IdentifierNames.global,
                            IdentifierName("NCoreUtils")
                        ),
                        IdentifierName("Data")
                    ),
                    IdentifierName("Builders")
                ),
                GenericName(
                    Identifier("UpdateDelegate"),
                    TypeArgumentList(SingletonSeparatedList(builderType))
                )
            );

        public static NameSyntax IReadOnlyList(TypeSyntax elementType)
            => QualifiedName(
                QualifiedName(
                    QualifiedName(
                        AliasQualifiedName(
                            IdentifierNames.global,
                            IdentifierName("System")
                        ),
                        IdentifierName("Collections")
                    ),
                    IdentifierName("Generic")
                ),
                GenericName(
                    Identifier("IReadOnlyList"),
                    TypeArgumentList(SingletonSeparatedList(elementType))
                )
            );
    }

    private static class Methods
    {
        public static class RefList
        {
            public static ExpressionSyntax CreateOrDefault { get; } = SimpleMemberAccessExpression(
                Types.RefList,
                IdentifierName("CreateOrDefault")
            );
        }

        public static class Array
        {
            public static ExpressionSyntax Empty(TypeSyntax elementType) => SimpleMemberAccessExpression(
                Types.Array,
                GenericName(Identifiers.Empty, TypeArgumentList(SingletonSeparatedList(elementType)))
            );
        }
    }
}