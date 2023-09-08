using System;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal class BuilderTarget
{
    public SemanticModel SemanticModel { get; }

    public SyntaxNode Node { get; }

    public INamedTypeSymbol Type { get; }

    public string FullName { get; }

    public string SourceNamespace { get; }

    public string TargetNamespace { get; }

    public string TargetName { get; }

    public string TargetFullName { get; }

    public BuilderTarget(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol type)
    {
        SemanticModel = semanticModel;
        Node = node;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        SourceNamespace = Type.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
        TargetNamespace = $"{SourceNamespace}.Builders";
        TargetName = $"{type.Name}Builder";
        TargetFullName = $"{TargetNamespace}.{type.Name}Builder";
    }
}