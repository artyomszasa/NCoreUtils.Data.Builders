using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal class PropertyData
{
    private static SymbolDisplayFormat FullyQualifiedMaybeNullableFormat { get; } = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public string FieldName { get; }

    public string PropertyName { get; }

    public IdentifierNameSyntax FieldIdentifier { get;}

    public IdentifierNameSyntax PropertyIdentifier { get;}

    public ITypeSymbol SourceType { get; }

    public ITypeSymbol? Type { get; }

    public string FullyQualifiedFieldTypeName { get; }

    public string FullyQualifiedPropertyTypeName { get; }

    [MemberNotNullWhen(true, nameof(ElementTypeFullName))]
    [MemberNotNullWhen(true, nameof(SourceElementType))]
    public bool IsRefList { get; }

    public bool IsInt32List { get; }

    public bool IsStringList { get; }

    public string? ElementTypeFullName { get; }

    public ITypeSymbol? SourceElementType { get; }

    public IPropertySymbol PropertySymbol { get; }

    public PropertyData(SemanticModel semanticModel, IReadOnlyList<string> builderFullNames, IPropertySymbol property)
    {
        PropertySymbol = property ?? throw new ArgumentNullException(nameof(property));
        FieldName = $"_{property.Name.Uncapitalize()}";
        PropertyName = property.Name;
        FieldIdentifier = IdentifierName(FieldName);
        PropertyIdentifier = IdentifierName(PropertyName);
        SourceType = property.Type;
        if (property.GetAttributes().TryGetFirst(a => a.AttributeClass?.Name == "BuilderFieldTypeAttribute", out var attr))
        {
            Type = (ITypeSymbol)attr.ConstructorArguments[0].Value!;
            IsRefList = false;
            SourceElementType = default;
            ElementTypeFullName = default;
            FullyQualifiedFieldTypeName = Type.WithNullableAnnotation(NullableAnnotation.Annotated).ToDisplayString(FullyQualifiedMaybeNullableFormat);
            FullyQualifiedPropertyTypeName = Type.ToDisplayString(FullyQualifiedMaybeNullableFormat);
        }
        else if (SourceType is INamedTypeSymbol named && named.ConstructedFrom is not null && named.ConstructedFrom.Name == "IReadOnlyList")
        {
            var argumentType = named.TypeArguments[0];
            if (argumentType.SpecialType == SpecialType.System_String)
            {
                var listDef = semanticModel.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
                    ?? throw new InvalidOperationException("Could not get generic type definition for System.Collections.Generic.List.");
                Type = listDef.Construct(argumentType).WithNullableAnnotation(NullableAnnotation.Annotated);
                FullyQualifiedFieldTypeName = Type.ToDisplayString(FullyQualifiedMaybeNullableFormat);
                FullyQualifiedPropertyTypeName = Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                IsInt32List = false;
                IsStringList = true;
                IsRefList = false;
            }
            else if (argumentType.SpecialType == SpecialType.System_Int32)
            {
                var listDef = semanticModel.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
                    ?? throw new InvalidOperationException("Could not get generic type definition for System.Collections.Generic.List.");
                Type = listDef.Construct(argumentType).WithNullableAnnotation(NullableAnnotation.Annotated);
                FullyQualifiedFieldTypeName = Type.ToDisplayString(FullyQualifiedMaybeNullableFormat);
                FullyQualifiedPropertyTypeName = Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                IsInt32List = true;
                IsStringList = false;
                IsRefList = false;
            }
            else
            {
                var elementNamespace = argumentType.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
                SourceElementType = argumentType;
                ElementTypeFullName = elementNamespace + ".Builders." + argumentType.Name + "Builder";
                var elementType = semanticModel.Compilation.GetTypeByMetadataName(ElementTypeFullName);
                if (elementType is null)
                {
                    if (!builderFullNames.Contains(ElementTypeFullName))
                    {
                        throw new BuilderGenerationException(new(
                            descriptor: DiagnosticDescriptors.NoBuilderFor,
                            location: property.Locations.FirstOrDefault(),
                            new object[] { argumentType, ElementTypeFullName, property }
                        ));
                    }
                    FullyQualifiedFieldTypeName = $"global::NCoreUtils.Data.Builders.RefList<{ElementTypeFullName}>?";
                    FullyQualifiedPropertyTypeName = $"global::NCoreUtils.Data.Builders.RefList<{ElementTypeFullName}>";
                }
                else
                {
                    var refListDef = semanticModel.Compilation.GetTypeByMetadataName("NCoreUtils.Data.Builders.RefList`1")
                        ?? throw new InvalidOperationException("Could not get generic type definition for NCoreUtils.Data.Builders.RefList.");
                    Type = refListDef.Construct(elementType).WithNullableAnnotation(NullableAnnotation.Annotated);
                    FullyQualifiedFieldTypeName = Type.ToDisplayString(FullyQualifiedMaybeNullableFormat);
                    FullyQualifiedPropertyTypeName = Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
                IsRefList = true;
                IsInt32List = false;
                IsStringList = false;
            }
        }
        else
        {
            Type = SourceType;
            IsRefList = false;
            SourceElementType = default;
            ElementTypeFullName = default;
            FullyQualifiedFieldTypeName = Type.WithNullableAnnotation(NullableAnnotation.Annotated).ToDisplayString(FullyQualifiedMaybeNullableFormat);
            FullyQualifiedPropertyTypeName = Type.ToDisplayString(FullyQualifiedMaybeNullableFormat);
        }
    }
}

internal class BuilderEmitter
{
    private static IdentifierNameSyntax ValueIdentifier { get; } = IdentifierName("value");

    private static TypeSyntax StringListTypeSyntax { get; } = ParseTypeName("global::System.Collections.Generic.List<string>");

    private static TypeSyntax Int32ListTypeSyntax { get; } = ParseTypeName("global::System.Collections.Generic.List<int>");

    private SemanticModel SemanticModel { get; }

    private ITypeSymbol RefListFactoryType { get; }

    public BuilderEmitter(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel;
        RefListFactoryType = semanticModel.Compilation.GetTypeByMetadataName("NCoreUtils.Data.Builders.RefList")
            ?? throw new InvalidOperationException("Unable to get type symbol for NCoreUtils.Data.Builders.RefList.");
    }

    private ExpressionSyntax GetDefaultValueSyntax(ITypeSymbol? targetType, PropertyData data)
    {
        if (data.Type?.SpecialType is SpecialType.System_String)
        {
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("string"), IdentifierName("Empty"));
        }
        if (data.IsStringList)
        {
            return ObjectCreationExpression(StringListTypeSyntax, ArgumentList(SeparatedList(Array.Empty<ArgumentSyntax>())), null);
        }
        if (data.IsInt32List)
        {
            return ObjectCreationExpression(Int32ListTypeSyntax, ArgumentList(SeparatedList(Array.Empty<ArgumentSyntax>())), null);
        }
        if (data.IsRefList)
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(RefListFactoryType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                    GenericName(
                        Identifier("Empty"),
                        TypeArgumentList(SeparatedList(new [] { ParseTypeName(data.ElementTypeFullName) }))
                    )
                )
            );
        }
        var getDefaultValueMethodName = $"GetDefault{data.PropertyName}Value";
        if (targetType is not null && targetType.GetMembers().OfType<IMethodSymbol>().TryGetFirst(m => m.Name == getDefaultValueMethodName, out var m))
        {
            return InvocationExpression(IdentifierName(getDefaultValueMethodName));
        }
        throw new BuilderGenerationException(new(
            descriptor: DiagnosticDescriptors.NoDefaultValue,
            location: data.PropertySymbol.Locations.FirstOrDefault(),
            messageArgs: new object[] { data.PropertyName, getDefaultValueMethodName }
        ));
    }

    private static FieldDeclarationSyntax EmitField(PropertyData data)
    {
        return FieldDeclaration(VariableDeclaration(
            IdentifierName(data.FullyQualifiedFieldTypeName),
            SeparatedList(new[] { VariableDeclarator(data.FieldName) })
        ))
        .AddModifiers(Token(SyntaxKind.PrivateKeyword));
    }

    private PropertyDeclarationSyntax EmitProperty(ITypeSymbol? targetType, PropertyData data)
    {
        var getterSyntax = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
        if (!data.IsRefList && !data.IsStringList && !data.IsInt32List && data.Type is not null && (data.Type.IsValueType || data.Type.NullableAnnotation == NullableAnnotation.Annotated))
        {
            getterSyntax = getterSyntax
                .AddModifiers(Token(SyntaxKind.ReadOnlyKeyword))
                .WithExpressionBody(ArrowExpressionClause(data.FieldIdentifier))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }
        else
        {
            getterSyntax = getterSyntax
                .WithExpressionBody(ArrowExpressionClause(
                    AssignmentExpression(
                        SyntaxKind.CoalesceAssignmentExpression,
                        data.FieldIdentifier,
                        GetDefaultValueSyntax(targetType, data)
                    )
                ))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        return PropertyDeclaration(
            type: IdentifierName(data.FullyQualifiedPropertyTypeName),
            identifier: Identifier(data.PropertyName)
        )
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                getterSyntax,
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithExpressionBody(ArrowExpressionClause(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            data.FieldIdentifier,
                            ValueIdentifier
                        )
                    ))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            );
    }

    private static bool Eqi(string? a, string? b)
        => StringComparer.InvariantCultureIgnoreCase.Equals(a, b);

    private static bool Eqs(ITypeSymbol a, ITypeSymbol b)
        => SymbolEqualityComparer.Default.Equals(a, b);

    private ConstructorDeclarationSyntax EmitCtor(BuilderTarget target, ITypeSymbol? targetType, IReadOnlyList<PropertyData> properties)
    {
        var parameterIdentifier = Identifier("source");
        var bodyExpressions = properties.Select(p =>
        {
            var initializerMethodName = $"Initialize{p.PropertyName}";
            if (targetType is not null && targetType.GetMembers().OfType<IMethodSymbol>().Any(m => m.Name == initializerMethodName))
            {
                return ExpressionStatement(
                    InvocationExpression(
                        IdentifierName(initializerMethodName),
                        ArgumentList(SeparatedList(new []
                        {
                            Argument(IdentifierName("source")),
                            Argument(default, Token(SyntaxKind.OutKeyword), p.FieldIdentifier)
                        }))
                    )
                );
            }
            if (p.IsInt32List)
            {
                var memberExpression = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("source"), IdentifierName(p.PropertyName));
                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    p.FieldIdentifier,
                    ConditionalExpression(
                        IsPatternExpression(memberExpression, ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                        LiteralExpression(SyntaxKind.NullLiteralExpression),
                        ObjectCreationExpression(
                            Int32ListTypeSyntax,
                            ArgumentList(SeparatedList(new [] {
                                Argument(memberExpression)
                            })),
                            null
                        )
                    )
                ));
            }
            if (p.IsStringList)
            {
                var memberExpression = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("source"), IdentifierName(p.PropertyName));
                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    p.FieldIdentifier,
                    ConditionalExpression(
                        IsPatternExpression(memberExpression, ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                        LiteralExpression(SyntaxKind.NullLiteralExpression),
                        ObjectCreationExpression(
                            StringListTypeSyntax,
                            ArgumentList(SeparatedList(new [] {
                                Argument(memberExpression)
                            })),
                            null
                        )
                    )
                ));
            }
            if (p.IsRefList)
            {
                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    p.FieldIdentifier,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(RefListFactoryType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                            IdentifierName("CreateOrDefault")
                        ),
                        ArgumentList(SeparatedList(new []
                        {
                            Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("source"), IdentifierName(p.PropertyName))),
                            Argument(SimpleLambdaExpression(
                                TokenList(Token(SyntaxKind.StaticKeyword)),
                                Parameter(Identifier("e")),
                                null,
                                ObjectCreationExpression(
                                    newKeyword: Token(SyntaxKind.NewKeyword),
                                    type: ParseTypeName(p.ElementTypeFullName),
                                    argumentList: ArgumentList(SeparatedList(new[]
                                    {
                                        Argument(IdentifierName("e"))
                                    })),
                                    initializer: null
                                )
                            ))
                        }))
                    )
                ));
            }
            return ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                p.FieldIdentifier,
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("source"), IdentifierName(p.PropertyName))
            ));
        }).ToList();
        if (targetType is not null)
        {
            foreach (var builderField in targetType.GetMembers().OfType<IFieldSymbol>())
            {
                if (builderField.GetAttributes().Any(a => a.AttributeClass?.Name == "BuilderFieldAttribute"))
                {
                    bodyExpressions.Add(ExpressionStatement(
                        InvocationExpression(
                            IdentifierName($"Initialize{builderField.Name.TrimStart('_').Capitalize()}"),
                            ArgumentList(SeparatedList(new []
                            {
                                Argument(IdentifierName("source")),
                                Argument(default, Token(SyntaxKind.OutKeyword), IdentifierName(builderField.Name))
                            }))
                        )
                    ));
                }
            }
        }
        var body = Block(bodyExpressions);
        return ConstructorDeclaration(target.TargetName)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                ParameterList(
                    SeparatedList(new [] { Parameter(parameterIdentifier).WithType(ParseTypeName(target.FullName)) })
                )
            )
            .WithBody(body);
    }

    private MethodDeclarationSyntax EmitBuildMethod(BuilderTarget target, ITypeSymbol? targetType, IReadOnlyList<PropertyData> properties)
    {
        var candidates = target.Type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
            .Where(m => m.Parameters.All(p => properties.Any(prop => Eqi(prop.PropertyName, p.Name) && Eqs(prop.SourceType, p.Type))));
        IMethodSymbol? ctor = null;
        foreach (var candidate in candidates)
        {
            if (ctor is not null)
            {
                throw new InvalidOperationException("Multiple constructor matches.");
            }
            ctor = candidate;
        }
        if (ctor is null)
        {
            throw new InvalidOperationException("No constructor match.");
        }
        var argList = SeparatedList(ctor.Parameters.Select(p =>
        {
            var data = properties.First(prop => Eqi(prop.PropertyName, p.Name) && Eqs(prop.SourceType, p.Type));
            var buildMethod = $"Build{data.PropertyName}";
            var expr = targetType is not null && targetType.GetMembers().Any(m => m is IMethodSymbol && m.Name == buildMethod)
                ? InvocationExpression(
                    IdentifierName(buildMethod),
                    ArgumentList(SeparatedList(new [] { Argument(null, Token(SyntaxKind.InKeyword), IdentifierName(data.FieldName)) }))
                )
                : data.IsInt32List
                    ? ConditionalExpression(
                        IsPatternExpression(IdentifierName(data.FieldName), ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                        CastExpression(
                            ParseTypeName(data.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("System.Array"),
                                    GenericName(Identifier("Empty"), TypeArgumentList(SeparatedList(new [] { ParseTypeName("int") })))
                                )
                            )
                        ),
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(data.FieldName),
                                IdentifierName("ToArray")
                            )
                        )
                    )
                    : data.IsStringList
                        ? ConditionalExpression(
                            IsPatternExpression(IdentifierName(data.FieldName), ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                            CastExpression(
                                ParseTypeName(data.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("System.Array"),
                                        GenericName(Identifier("Empty"), TypeArgumentList(SeparatedList(new [] { ParseTypeName("string") })))
                                    )
                                )
                            ),
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(data.FieldName),
                                    IdentifierName("ToArray")
                                )
                            )
                        )
                        : data.IsRefList
                            ? ConditionalExpression(
                                IsPatternExpression(IdentifierName(data.FieldName), ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                                CastExpression(
                                    ParseTypeName(data.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("System.Array"),
                                            GenericName(Identifier("Empty"), TypeArgumentList(SeparatedList(new [] { ParseTypeName(data.SourceElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) })))
                                        )
                                    )
                                ),
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(data.FieldName),
                                        IdentifierName("Build")
                                    ),
                                    ArgumentList(SeparatedList(new []
                                    {
                                        Argument(ParenthesizedLambdaExpression(
                                            TokenList(Token(SyntaxKind.StaticKeyword)),
                                            ParameterList(SeparatedList(new []
                                            {
                                                Parameter(default, TokenList(Token(SyntaxKind.RefKeyword)), ParseTypeName(data.ElementTypeFullName), Identifier("builder"), default)
                                            })),
                                            null,
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("builder"),
                                                    IdentifierName("Build")
                                                )
                                            )
                                        ))
                                    }))
                                )
                            )
                            : data.Type is not null && (data.Type.IsValueType || data.Type.NullableAnnotation == NullableAnnotation.Annotated)
                                ? IdentifierName(data.FieldName)
                                : (ExpressionSyntax)BinaryExpression(
                                    SyntaxKind.CoalesceExpression,
                                    IdentifierName(data.FieldName),
                                    GetDefaultValueSyntax(targetType, data)
                                );

            return Argument(expr).WithNameColon(NameColon(IdentifierName(p.Name)));
        }));
        var returnType = ParseTypeName(target.FullName);
        return MethodDeclaration(returnType, "Build")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ReadOnlyKeyword)))
            .WithExpressionBody(
                ArrowExpressionClause(
                    ObjectCreationExpression(
                        Token(SyntaxKind.NewKeyword),
                        returnType,
                        ArgumentList(argList),
                        null
                    )
                )
            )
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private StructDeclarationSyntax EmitStruct(BuilderTarget target, IReadOnlyList<string> builderFullNames)
    {
        var builderTypeName = target.Type.Name + "Builder";
        var targetType = SemanticModel.Compilation.GetTypeByMetadataName($"{target.TargetNamespace}.{builderTypeName}");

        var members = new List<MemberDeclarationSyntax>();
        var properties = new List<PropertyData>();
        foreach (var property in target.Type.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.DeclaredAccessibility == Accessibility.Public)
            {
                var data = new PropertyData(SemanticModel, builderFullNames, property);
                members.Add(EmitField(data));
                members.Add(EmitProperty(targetType, data));
                properties.Add(data);
            }
        }
        members.Add(EmitCtor(target, targetType, properties));
        members.Add(EmitBuildMethod(target, targetType, properties));

        return StructDeclaration(builderTypeName)
            .AddModifiers(
                Token(TriviaList(Comment("/// <inheritdoc/>")), SyntaxKind.PublicKeyword, TriviaList()),
                Token(SyntaxKind.PartialKeyword)
            )
            .AddMembers(members.ToArray());
    }

    private ClassDeclarationSyntax EmitExtensions(BuilderTarget target)
    {
        var builderTypeName = target.Type.Name + "Builder";
        var extensionTypeName = builderTypeName + "Extensions";
        var targetTypeSyntax = ParseTypeName(target.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        var builderTypeSyntax = ParseTypeName(builderTypeName);
        var delegateTypeSyntax = GenericName(
            Identifier("NCoreUtils.Data.Builders.UpdateDelegate"),
            TypeArgumentList(SeparatedList(new [] { builderTypeSyntax }))
        );
        return ClassDeclaration(extensionTypeName)
            .AddModifiers(
                Token(TriviaList(Comment("/// <inheritdoc/>")), SyntaxKind.PublicKeyword, TriviaList()),
                Token(SyntaxKind.StaticKeyword)
            )
            .AddMembers(
                MethodDeclaration(targetTypeSyntax, "Update")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(ParameterList(SeparatedList(new []
                    {
                        Parameter(Identifier("source")).WithType(targetTypeSyntax).WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword))),
                        Parameter(Identifier("update")).WithType(delegateTypeSyntax)
                    })))
                    .WithBody(Block(
                        LocalDeclarationStatement(
                            VariableDeclaration(builderTypeSyntax, SeparatedList(new []
                            {
                                VariableDeclarator(Identifier("builder"), default, EqualsValueClause(
                                    ObjectCreationExpression(
                                        builderTypeSyntax,
                                        ArgumentList(SeparatedList(new[] { Argument(IdentifierName("source")) })),
                                        default
                                    )
                                ))
                            }))
                        ),
                        ExpressionStatement(
                            InvocationExpression(
                                IdentifierName("update"),
                                ArgumentList(SeparatedList(new[] { Argument(default, Token(SyntaxKind.RefKeyword), IdentifierName("builder")) }))
                            )
                        ),
                        ReturnStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("builder"),
                                    IdentifierName("Build")
                                )
                            )
                        )
                    ))
            );
    }

    public CompilationUnitSyntax EmitCompilationUnit(BuilderTarget target, IReadOnlyList<string> builderFullNames)
    {
        SyntaxTriviaList syntaxTriviaList = TriviaList(
            Comment("// <auto-generated/>"),
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true))
        );

        return CompilationUnit()
            .AddMembers(
                NamespaceDeclaration(IdentifierName(target.TargetNamespace))
                    .WithLeadingTrivia(syntaxTriviaList)
                    .AddMembers(EmitStruct(target, builderFullNames))
                    .AddMembers(EmitExtensions(target))
            )
            .NormalizeWhitespace();
    }
}