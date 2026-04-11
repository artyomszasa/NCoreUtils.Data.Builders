using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal static partial class BuilderEmitter
{
    private static readonly string SelfVersion = typeof(BuilderEmitter).Assembly.GetName()?.Version.ToString() ?? string.Empty;

    private static FieldDeclarationSyntax EmitBackingField(BuilderPropertyDescriptor data)
        => FieldDeclaration(VariableDeclaration(
            IdentifierName(data.QualifiedFieldTypeName),
            SingletonSeparatedList(VariableDeclarator(data.FieldName))
        ))
        .AddModifiers(Keywords.Private);

    private static FieldDeclarationSyntax EmitNestedBuilderField2(BuilderFieldDescriptor data)
        => FieldDeclaration(VariableDeclaration(
            IdentifierName(data.QualifiedTypeName),
            SingletonSeparatedList(VariableDeclarator(data.FieldName))
        ))
        .AddModifiers(Keywords.Public)
        .WithLeadingTrivia(TriviaList(
            Tab4,
            Trivia(data.DocumentationComment),
            EndOfLine,
            Tab4
        ));

    private static PropertyDeclarationSyntax EmitProperty2(BuilderPropertyDescriptor data)
    {
        var getterSyntax = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
        if (!data.PropertyType.IsRefList && !data.PropertyType.IsStringList && !data.PropertyType.IsInt32List
            && (data.PropertyType.IsValueType || data.PropertyType.HasNullableAnnotation))
        {
            getterSyntax = getterSyntax
                .AddModifiers(Keywords.ReadOnly)
                .WithExpressionBody(ArrowExpressionClause(data.FieldIdentifier))
                .WithSemicolonToken(Tokens.Semicolon);
        }
        else
        {
            getterSyntax = getterSyntax
                .WithExpressionBody(ArrowExpressionClause(
                    AssignmentExpression(
                        SyntaxKind.CoalesceAssignmentExpression,
                        data.FieldIdentifier,
                        data.DefaultValueSyntax
                        // GetDefaultValueSyntax(targetType, data)
                    )
                ))
                .WithSemicolonToken(Tokens.Semicolon);
        }
        var setterSyntax = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
            .WithExpressionBody(ArrowExpressionClause(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    data.FieldIdentifier,
                    IdentifierNames.value
                )
            ))
            .WithSemicolonToken(Tokens.Semicolon);
        return PropertyDeclaration(
            type: IdentifierName(data.QualifiedPropertyTypeName),
            identifier: Identifier(data.PropertyName)
        )
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                getterSyntax,
                setterSyntax
            )
            .WithLeadingTrivia(TriviaList(
                Tab4,
                Trivia(data.DocumentationComment),
                EndOfLine,
                Tab4
            ));
    }

    private static ConstructorDeclarationSyntax EmitCtor(BuilderDescriptor target)
    {
        var bodyStatements = target.Members.Select(m =>
        {
            if (m is null)
            {
                throw new ArgumentNullException(nameof(m));
            }
            if (m.TryGetInitializer(out var initializerStatement))
            {
                return initializerStatement;
            }
            if (m.IsInt32List)
            {
                var memberExpression = SimpleMemberAccessExpression(IdentifierNames.source, IdentifierName(m.SourcePropertyName));
                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    m.FieldIdentifier,
                    ConditionalExpression(
                        IsNullExpression(memberExpression),
                        NullLiteral,
                        NewExpression(Types.Int32List, memberExpression)
                    )
                ));
            }
            if (m.IsStringList)
            {
                var memberExpression = SimpleMemberAccessExpression(IdentifierNames.source, IdentifierName(m.SourcePropertyName));
                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    m.FieldIdentifier,
                    ConditionalExpression(
                        IsNullExpression(memberExpression),
                        NullLiteral,
                        NewExpression(Types.StringList, memberExpression)
                    )
                ));
            }
            if (m.IsRefList)
            {
                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    m.FieldIdentifier,
                    SimpleInvocationExpression(
                        Methods.RefList.CreateOrDefault,
                        SimpleMemberAccessExpression(IdentifierNames.source, IdentifierName(m.SourcePropertyName)),
                        SimpleLambdaExpression(
                            TokenList(Keywords.Static),
                            Parameter(Identifiers.e),
                            null,
                            NewExpression(m.ElementType.Syntax, IdentifierNames.e)
                        )
                    )
                ));
            }
            if (m is BuilderFieldDescriptor f)
            {
                var memberExpression = SimpleMemberAccessExpression(IdentifierNames.source, IdentifierName(f.SourcePropertyName));
                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    f.FieldIdentifier,
                    ConditionalExpression(
                        IsNullExpression(memberExpression),
                        DefaultLiteral,
                        NewExpression(f.Type.Syntax, memberExpression)
                    )
                ));
            }
            return ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                m.FieldIdentifier,
                SimpleMemberAccessExpression(IdentifierNames.source, IdentifierName(m.SourcePropertyName))
            ));
        });
        var body = Block(bodyStatements);
        var documentationComment = DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            List(new XmlNodeSyntax[]
            {
                XmlText(XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList())),
                XmlElement("summary", List(new XmlNodeSyntax[]
                {
                    XmlText($"Initializes instance of "),
                    XmlEmptyElement(
                        XmlName("see"),
                        List(new XmlAttributeSyntax[]
                        {
                            XmlCrefAttribute(TypeCref(IdentifierName(target.BuilderName)))
                        })
                    ),
                    XmlText($".")
                })),
                XmlText(XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()))
            }),
            Token(SyntaxKind.EndOfDocumentationCommentToken)
        ).WithTrailingTrivia(EndOfLine("\r\n"));

        return ConstructorDeclaration(target.BuilderName)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                ParameterList(
                    SingletonSeparatedList(Parameter(Identifiers.source).WithType(target.SourceType.Syntax))
                )
            )
            .WithBody(body)
            .WithLeadingTrivia(TriviaList(
                Tab4,
                Trivia(documentationComment),
                EndOfLine,
                Tab4
            ));
    }

    private static ExpressionSyntax BuildInt32ListExpression(BuilderPropertyDescriptor data)
        => ConditionalExpression(
            IsNullExpression(data.FieldIdentifier),
            SimpleInvocationExpression(Methods.Array.Empty(Types.Int32)),
            SimpleInvocationExpression(SimpleMemberAccessExpression(data.FieldIdentifier, IdentifierNames.ToArray))
        );

    private static ExpressionSyntax BuildStringListExpression(BuilderPropertyDescriptor data)
        => ConditionalExpression(
            IsNullExpression(data.FieldIdentifier),
            SimpleInvocationExpression(Methods.Array.Empty(Types.String)),
            SimpleInvocationExpression(SimpleMemberAccessExpression(data.FieldIdentifier, IdentifierNames.ToArray))
        );

    private static ExpressionSyntax BuildRefListExpression(BuilderPropertyDescriptor data)
    {
        if (data.SourcePropertyType.ElementType is not TypeDescriptor sourceElementType)
        {
            throw new InvalidOperationException($"{data.SourceType.QualifiedName}.{data.SourcePropertyName} descriptor has no element type.");
        }
        if (data.ElementType is not TypeDescriptor elementType)
        {
            throw new InvalidOperationException($"{data.SourceType} => {data.PropertyName} descriptor has no element type.");
        }
        return ConditionalExpression(
                IsNullExpression(data.FieldIdentifier),
                CastExpression(
                    Types.IReadOnlyList(sourceElementType.Syntax),
                    SimpleInvocationExpression(Methods.Array.Empty(sourceElementType.Syntax))
                ),
                SimpleInvocationExpression(
                    SimpleMemberAccessExpression(data.FieldIdentifier, IdentifierNames.Build),
                    ParenthesizedLambdaExpression(
                        TokenList(Token(SyntaxKind.StaticKeyword)),
                        ParameterList(SingletonSeparatedList(
                            Parameter(default, TokenList(Keywords.Ref), elementType.Syntax, Identifiers.builder, default)
                        )),
                        null,
                        SimpleInvocationExpression(
                            SimpleMemberAccessExpression(IdentifierNames.builder, IdentifierNames.Build)
                        )
                    )
                )
            );
    }

    private static MethodDeclarationSyntax EmitBuildMethod(BuilderDescriptor target)
    {
        var argList = SeparatedList(target.Members.Where(m => m.ConstructorParameterName is not null).Select(m =>
        {
            var parameterName = m.ConstructorParameterName!;
            var expr = m switch
            {
                _ when m.TryGetBuilder(out var builderExpression) => builderExpression,
                BuilderFieldDescriptor f => SimpleInvocationExpression(
                    SimpleMemberAccessExpression(f.FieldIdentifier, IdentifierNames.Build)
                ),
                BuilderPropertyDescriptor p when p.IsInt32List => BuildInt32ListExpression(p),
                BuilderPropertyDescriptor p when p.IsStringList => BuildStringListExpression(p),
                BuilderPropertyDescriptor p when p.IsRefList => BuildRefListExpression(p),
                _ when m.PublicType.IsValueType || m.PublicType.HasNullableAnnotation => m.FieldIdentifier,
                _ => BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    m.FieldIdentifier,
                    m.DefaultValueSyntax
                )
            };
            return NamedArg(parameterName, expr);
        }));
        var documentationComment = DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            List(new XmlNodeSyntax[]
            {
                XmlText(XmlTextLiteral(TriviaList(DocumentationCommentExterior("///")), " ", " ", TriviaList())),
                XmlElement("summary", List(new XmlNodeSyntax[]
                {
                    XmlText("Creates new instance of "),
                    XmlEmptyElement(
                        XmlName("see"),
                        List(new XmlAttributeSyntax[]
                        {
                            XmlCrefAttribute(TypeCref(target.SourceType.Syntax))
                        })
                    ),
                    XmlText(" from the the current instance."),
                })),
                XmlText(XmlTextNewLine(TriviaList(), "\r\n", "\r\n", TriviaList()))
            }),
            Token(SyntaxKind.EndOfDocumentationCommentToken)
        ).WithTrailingTrivia(EndOfLine("\r\n"));
        return MethodDeclaration(target.SourceType.Syntax, Identifiers.Build)
            .WithModifiers(TokenList(Keywords.Public, Keywords.ReadOnly))
            .WithLeadingTrivia(TriviaList(
                Whitespace("    "),
                Trivia(documentationComment),
                EndOfLine("\r\n"),
                Whitespace("    ")
            ))
            .WithExpressionBody(
                ArrowExpressionClause(
                    ObjectCreationExpression(
                        Token(SyntaxKind.NewKeyword),
                        target.SourceType.Syntax,
                        ArgumentList(argList),
                        null
                    )
                )
            )
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static StructDeclarationSyntax EmitStruct(BuilderDescriptor target)
    {
        var members = new List<MemberDeclarationSyntax>();
        foreach (var member in target.Members)
        {
            switch (member)
            {
                case BuilderFieldDescriptor fieldDescriptor:
                    members.Add(EmitNestedBuilderField2(fieldDescriptor));
                    break;
                case BuilderPropertyDescriptor propertyDescriptor:
                    members.Add(EmitBackingField(propertyDescriptor));
                    members.Add(EmitProperty2(propertyDescriptor));
                    break;
            }
        }
        members.Add(EmitCtor(target));
        members.Add(EmitBuildMethod(target));
        return StructDeclaration(target.BuilderName)
            .AddAttributeLists(
                AttributeList(
                    Token(
                        target.DocumentationComment is DocumentationCommentTriviaSyntax doc
                            ? TriviaList(Trivia(doc))
                            : TriviaList(),
                        SyntaxKind.OpenBracketToken,
                        TriviaList()
                    ),
                    default,
                    SingletonSeparatedList(
                        Attribute(ParseName("System.CodeDom.Compiler.GeneratedCodeAttribute"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                        {
                            AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("NCoreUtils.Data.Builders"))),
                            AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(SelfVersion)))
                        })))
                    ),
                    Token(SyntaxKind.CloseBracketToken)
                )
            )
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.PartialKeyword)
            )
            .AddMembers([.. members]);
    }

    private static ClassDeclarationSyntax EmitExtensions(BuilderDescriptor target)
    {
        var extensionTypeName = target.BuilderName + "Extensions";
        var delegateTypeSyntax = Types.UpdateDelegate(target.BuilderTypeSyntax);
        return ClassDeclaration(extensionTypeName)
            .AddAttributeLists(
                AttributeList(SingletonSeparatedList(
                    Attribute(ParseName("System.CodeDom.Compiler.GeneratedCodeAttribute"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                    {
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("NCoreUtils.Data.Builders"))),
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(SelfVersion)))
                    })))
                ))
            )
            .AddModifiers(Keywords.Static)
            .AddMembers(
                MethodDeclaration(target.SourceType.Syntax, "Update")
                    .WithModifiers(TokenList(Keywords.Public, Keywords.Static))
                    .WithParameterList(ParameterList(SeparatedList(new []
                    {
                        Parameter(Identifiers.source).WithType(target.SourceType.Syntax).WithModifiers(TokenList(Keywords.This)),
                        Parameter(Identifiers.update).WithType(delegateTypeSyntax)
                    })))
                    .WithBody(Block(
                        LocalDeclarationStatement(
                            VariableDeclaration(target.BuilderTypeSyntax, SingletonSeparatedList(
                                VariableDeclarator(Identifiers.builder, default, EqualsValueClause(
                                    NewExpression(target.BuilderTypeSyntax, IdentifierNames.source)
                                ))
                            ))
                        ),
                        ExpressionStatement(
                            InvocationExpression(
                                IdentifierNames.update,
                                ArgumentList(SingletonSeparatedList(
                                    Argument(default, Token(SyntaxKind.RefKeyword), IdentifierNames.builder)
                                ))
                            )
                        ),
                        ReturnStatement(
                            SimpleInvocationExpression(
                                SimpleMemberAccessExpression(
                                    IdentifierNames.builder,
                                    IdentifierNames.Build
                                )
                            )
                        )
                    ))
            );
    }

    public static CompilationUnitSyntax EmitCompilationUnit(BuilderDescriptor target)
    {
        SyntaxTriviaList syntaxTriviaList = TriviaList(
            Comment("// <auto-generated/>"),
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true))
        );

        return CompilationUnit()
            .AddMembers(
                NamespaceDeclaration(IdentifierName(target.BuilderNamespace))
                    .WithLeadingTrivia(syntaxTriviaList)
                    .AddMembers(EmitStruct(target))
                    .AddMembers(EmitExtensions(target))
            )
            .NormalizeWhitespace();
    }
}