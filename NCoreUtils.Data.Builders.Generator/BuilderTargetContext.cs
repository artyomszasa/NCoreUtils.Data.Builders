using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal class BuilderTargetContext
{
    private ConcurrentDictionary<ITypeSymbol, TypeDescriptor> DescriptorCache { get; } = new(SymbolEqualityComparer.Default);

    // private ConcurrentDictionary<ITypeSymbol, TypeDescriptor> ListDescriptors { get; } = new(SymbolEqualityComparer.Default);

    private TypeDescriptor? _listOfInt32;

    private TypeDescriptor? _listOfString;

    private Func<ITypeSymbol, TypeDescriptor> FactoryNonBuilder { get; }

    private Func<ITypeSymbol, TypeDescriptor> FactoryBuilder { get; }

    private Compilation Compilation => SemanticModel.Compilation;

    public SemanticModel SemanticModel { get; }

    public HashSet<string> KnownBuilderNames { get; }

    public INamedTypeSymbol RefListOfT { get; }

    public INamedTypeSymbol IReadOnlyListOfT { get; }

    public INamedTypeSymbol ListOfT { get; }

    public INamedTypeSymbol String { get; }

    public INamedTypeSymbol Int32 { get; }

    public TypeDescriptor ListOfInt32
        => _listOfInt32 ??= GetOrCreate(ListOfT.Construct(Compilation.GetSpecialType(SpecialType.System_Int32)), false);

    public TypeDescriptor ListOfString
        => _listOfString ??= GetOrCreate(ListOfT.Construct(Compilation.GetSpecialType(SpecialType.System_String)), false);

    public BuilderTargetContext(SemanticModel semanticModel, HashSet<string> knownBuilderNames)
    {
        RefListOfT = semanticModel.Compilation.GetTypeByMetadataName("NCoreUtils.Data.Builders.RefList`1")
            ?? throw new InvalidOperationException("Could not get RefList<T> symbol.");
        IReadOnlyListOfT = semanticModel.Compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1")
            ?? throw new InvalidOperationException("Could not get IReadOnlyList<T> symbol.");
        ListOfT = semanticModel.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
            ?? throw new InvalidOperationException("Could not get List<T> symbol.");
        String = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
        Int32 = semanticModel.Compilation.GetSpecialType(SpecialType.System_Int32);
        FactoryNonBuilder = type => TypeDescriptor.Create(type, this, false);
        FactoryBuilder = type => TypeDescriptor.Create(type, this, true);
        SemanticModel = semanticModel;
        KnownBuilderNames = knownBuilderNames;
    }

    public ITypeSymbol RefList(ITypeSymbol elementType)
        => RefListOfT.Construct(elementType);

    public bool IsRefList(ITypeSymbol type, [MaybeNullWhen(false)] out ITypeSymbol elementType)
    {
        if (type is INamedTypeSymbol named
            && named.ConstructedFrom is not null
            && SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, RefListOfT))
        {
            elementType = named.TypeArguments[0];
            return true;
        }
        elementType = default;
        return false;
    }

    public bool IsReadOnlyList(ITypeSymbol type, [MaybeNullWhen(false)] out ITypeSymbol elementType)
    {
        if (type is INamedTypeSymbol named
            && named.ConstructedFrom is not null
            && SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, IReadOnlyListOfT))
        {
            elementType = named.TypeArguments[0];
            return true;
        }
        elementType = default;
        return false;
    }

    public bool IsList(ITypeSymbol type, [MaybeNullWhen(false)] out ITypeSymbol elementType)
    {
        if (type is INamedTypeSymbol named
            && named.ConstructedFrom is not null
            && SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, ListOfT))
        {
            elementType = named.TypeArguments[0];
            return true;
        }
        elementType = default;
        return false;
    }

    public bool IsStringList(ITypeSymbol type)
        => IsList(type, out var elementType) && elementType.SpecialType == SpecialType.System_String;

    public bool IsInt32List(ITypeSymbol type)
        => IsList(type, out var elementType) && elementType.SpecialType == SpecialType.System_Int32;

    public TypeDescriptor GetOrCreate(ITypeSymbol type, bool isBuilder)
        => DescriptorCache.GetOrAdd(type, isBuilder ? FactoryBuilder : FactoryNonBuilder);

    public bool ShouldBeMappedToNestedBuilder(ITypeSymbol sourceType, out BuilderMemberData data)
    {
        if (sourceType is INamedTypeSymbol named)
        {
            var elementNamespace = named.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
            var candidateBuilderQualifiedName = elementNamespace + ".Builders." + named.Name + "Builder";
            var existingBuilderType = SemanticModel.Compilation.GetTypeByMetadataName(candidateBuilderQualifiedName);
            // FIXME: debug
            // if (candidateBuilderQualifiedName != "System.Collections.Generic.Builders.IReadOnlyListBuilder" && candidateBuilderQualifiedName != "System.Builders.StringBuilder")
            // {
            //     throw new InvalidOperationException(candidateBuilderQualifiedName + " => " + string.Join(", ", KnownBuilderNames));
            // }
            if (existingBuilderType is not null)
            {
                data = BuilderMemberData.FromBuilderType(existingBuilderType);
                return true;
            }
            if (KnownBuilderNames.Contains(candidateBuilderQualifiedName))
            {
                data = BuilderMemberData.FromBuilderType(candidateBuilderQualifiedName);
                return true;
            }
        }
        data = default;
        return false;
    }

    private static string GetGeneratedSafeName(string qualifiedName) => qualifiedName.LastIndexOf('.') switch
    {
        -1 => qualifiedName,
        var i => qualifiedName.Substring(i + 1)
    };

    public TypeDescriptor CreateGeneratedBuilder(string qualifiedName) => new(
        isValueType: true,
        hasNullableAnnotation: default,
        qualifiedName: qualifiedName,
        safeName: GetGeneratedSafeName(qualifiedName), // TODO: do not recalculate
        isBuilder: true,
        isRefList: false,
        isStringList: false,
        isInt32List: false,
        elementType: default
    );

    public TypeDescriptor CreateBackingFieldType(TypeDescriptor propertyType)
    {
        if (propertyType.IsValueType || propertyType.HasNullableAnnotation)
        {
            return propertyType;
        }
        string nullableName;
        if (propertyType.QualifiedName.EndsWith("?", StringComparison.Ordinal))
        {
            // FIXME: emit warning
            nullableName = propertyType.QualifiedName;
        }
        else
        {
            nullableName = $"{propertyType.QualifiedName}?";
        }
        return new TypeDescriptor(
            isValueType: false,
            hasNullableAnnotation: true,
            qualifiedName: nullableName,
            safeName: propertyType.SafeName,
            isBuilder: false,
            isRefList: propertyType.IsRefList,
            isStringList: propertyType.IsStringList,
            isInt32List: propertyType.IsInt32List,
            elementType: propertyType.ElementType
        );
    }

    public TypeDescriptor ResolveDescriptor(BuilderMemberData data, out ITypeSymbol? typeSymbol)
    {
        switch (data)
        {
            case { IsNestedBuilder: true, Type: ITypeSymbol builderTypeSymbol }:
                typeSymbol = builderTypeSymbol;
                return GetOrCreate(builderTypeSymbol, true);
            // NOTE: this branch is only met when builder is to be generated
            case { IsNestedBuilder: true, NestedBuilderQualifiedName: string builderName }:
                typeSymbol = default;
                return CreateGeneratedBuilder(builderName);
            case { Type: ITypeSymbol mappedType } when IsReadOnlyList(mappedType, out var elementType):
                typeSymbol = mappedType;
                return elementType switch
                {
                    { SpecialType: SpecialType.System_String } => ListOfString,
                    { SpecialType: SpecialType.System_Int32 } => ListOfInt32,
                    _ when ShouldBeMappedToNestedBuilder(elementType, out var elementData) => CreateRefList(elementData),
                    _ => GetOrCreate(mappedType, isBuilder: false)
                };
            case { Type: ITypeSymbol mappedType }:
                typeSymbol = mappedType;
                return GetOrCreate(mappedType, isBuilder: false);
            default:
                throw new InvalidOperationException($"Should never happen: {data}");
        }
    }

    public TypeDescriptor CreateRefList(BuilderMemberData elementData) => elementData switch
    {
        { IsNestedBuilder: true, Type: ITypeSymbol builderTypeSymbol } => GetOrCreate(RefList(builderTypeSymbol), false),
        // NOTE: this branch is only met when builder is to be generated
        { IsNestedBuilder: true, NestedBuilderQualifiedName: string builderName } => new TypeDescriptor(
            isValueType: false,
            hasNullableAnnotation: false,
            qualifiedName: $"NCoreUtils.Data.Builders.RefList<{builderName}>",
            safeName: $"RefListOf{builderName}",
            isBuilder: false,
            isRefList: true,
            isStringList: false,
            isInt32List: false,
            elementType: ResolveDescriptor(elementData, out _)
        ),
        _ => throw new InvalidOperationException("Should never happen")
    };
}