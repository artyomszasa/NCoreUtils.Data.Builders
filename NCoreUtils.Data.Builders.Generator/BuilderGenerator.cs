using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NCoreUtils.Data;

[Generator]
public partial class BuilderGenerator : IIncrementalGenerator
{
    private readonly struct TargetOrError
    {
        public BuilderTarget? Target { get; }

        public DiagnosticData? Error { get; }

        private TargetOrError(BuilderTarget? target, DiagnosticData? error)
        {
            if (target is null && error is null)
            {
                throw new InvalidOperationException("Either target or error must be not null.");
            }
            Target = target;
            Error = error;
        }

        public TargetOrError(BuilderTarget target) : this(target, default) { }

        public TargetOrError(DiagnosticData error) : this(default, error) { }
    }

    private readonly struct DescriptorOrError
    {
        public static implicit operator DescriptorOrError(BuilderDescriptor descriptor)
            => new(descriptor);

        public static implicit operator DescriptorOrError(DiagnosticData error)
            => new(error);

        public BuilderDescriptor? Descriptor { get; }

        public DiagnosticData? Error { get; }

        private DescriptorOrError(BuilderDescriptor? descriptor, DiagnosticData? error)
        {
            if (descriptor is null && error is null)
            {
                throw new InvalidOperationException("Either target or error must be not null.");
            }
            Descriptor = descriptor;
            Error = error;
        }

        public DescriptorOrError(BuilderDescriptor descriptor)
            : this(descriptor, default)
        { }

        public DescriptorOrError(DiagnosticData error)
            : this(default, error)
        { }
    }


    private const string attributeSource = @"#nullable enable
namespace NCoreUtils.Data
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""NCoreUtils.Data.Builders"", ""8.0.0.0"")]
    internal sealed class HasBuilderAttribute : System.Attribute
    {
        public HasBuilderAttribute() { /* noop */ }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""NCoreUtils.Data.Builders"", ""8.0.0.0"")]
    internal sealed class BuilderFieldAttribute : System.Attribute
    {
        public BuilderFieldAttribute() { /* noop */ }
    }

    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""NCoreUtils.Data.Builders"", ""8.0.0.0"")]
    internal sealed class BuilderIgnoreAttribute : System.Attribute
    {
        public BuilderIgnoreAttribute() { /* noop */ }
    }

    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""NCoreUtils.Data.Builders"", ""8.0.0.0"")]
    internal sealed class BuilderFieldTypeAttribute : System.Attribute
    {
        public System.Type FieldType { get; }

        public BuilderFieldTypeAttribute(System.Type fieldType)
            => FieldType = fieldType;
    }

    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""NCoreUtils.Data.Builders"", ""8.0.0.0"")]
    internal sealed class BuilderPropertyNameAttribute : System.Attribute
    {
        public string PropertyName { get; }

        public BuilderPropertyNameAttribute(string propertyName)
            => PropertyName = propertyName;
    }
}";

    private static UTF8Encoding Utf8 { get; } = new(false);


    private static IEnumerable<BuilderMemberDescriptor> ExtractMembers(
        BuilderTargetContext context,
        ITypeSymbol source,
        ITypeSymbol? builder,
        CancellationToken cancellationToken)
    {
        var relevantProperties = source.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => property.DeclaredAccessibility == Accessibility.Public && !property.IsStatic)
            .Where(property => !property.GetAttributes().Any(static attr => attr.AttributeClass?.Name == "BuilderIgnoreAttribute"))
            .ToImmutableArray();
        var (ctor, constructorParameterNames) = ChooseConstructor(source, relevantProperties);
        foreach (var property in relevantProperties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var builderPropertyName = property.GetAttributes().TryGetFirst(a => a.AttributeClass?.Name == "BuilderPropertyNameAttribute", out var nameAttr)
                ? (string)nameAttr.ConstructorArguments[0].Value!
                : property.Name;
            var sourcePropertyType = context.GetOrCreate(property.Type, isBuilder: false);
            BuilderMemberData builderMemberData;
            if (property.GetAttributes().TryGetFirst(a => a.AttributeClass?.Name == "BuilderFieldTypeAttribute", out var attr))
            {
                // NOTE: postponed evaluation
                builderMemberData = BuilderMemberData.FromType((ITypeSymbol)attr.ConstructorArguments[0].Value!);
            }
            else if (context.ShouldBeMappedToNestedBuilder(property.Type, out var builderData))
            {
                builderMemberData = builderData;
            }
            else
            {
                builderMemberData = BuilderMemberData.FromType(property.Type);
            }
            if (builderMemberData.IsEmpty)
            {
                throw new InvalidOperationException($"{property} => {builderMemberData}");
            }
            var builderPropertyType = context.ResolveDescriptor(builderMemberData, out var builderMemberPropertySymbol);
            var (hasDefaultValueMethod, defaultValueSyntax) = GetDefaultValueSyntaxFor(
                context.KnownBuilderNames,
                targetSymbol: builder,
                sourcePropertySymbol: property,
                builderPropertyName: builderPropertyName,
                builderPropertyType: builderPropertyType
            );
            if (builderPropertyType.IsBuilder)
            {
                yield return new BuilderFieldDescriptor(
                    sourceType: context.GetOrCreate(source, false),
                    sourcePropertyType: sourcePropertyType,
                    sourcePropertyName: property.Name,
                    constructorParameterName: constructorParameterNames[property],
                    hasDefaultValueMethod: hasDefaultValueMethod,
                    defaultValueSyntax: defaultValueSyntax,
                    hasInitializerMethod: HasMethod(builderMemberPropertySymbol, Names.InitializeMethod(builderPropertyName)),
                    hasBuildMethod: HasMethod(builderMemberPropertySymbol, Names.BuildMethod(builderPropertyName)),
                    type: builderPropertyType,
                    fieldName: builderPropertyName
                );
            }
            else
            {
                yield return new BuilderPropertyDescriptor(
                    sourceType: context.GetOrCreate(source, false),
                    sourcePropertyType: sourcePropertyType,
                    sourcePropertyName: property.Name,
                    constructorParameterName: constructorParameterNames[property],
                    hasDefaultValueMethod: hasDefaultValueMethod,
                    defaultValueSyntax: defaultValueSyntax,
                    hasInitializerMethod: HasMethod(builderMemberPropertySymbol, Names.InitializeMethod(builderPropertyName)),
                    hasBuildMethod: HasMethod(builderMemberPropertySymbol, Names.BuildMethod(builderPropertyName)),
                    fieldType: context.CreateBackingFieldType(builderPropertyType),
                    propertyType: builderPropertyType,
                    fieldName: $"_{builderPropertyName.Uncapitalize()}",
                    propertyName: builderPropertyName
                );
            }
        }

        static (IMethodSymbol Constructor, Dictionary<IPropertySymbol, string>? ConstructorParameterNames, string? Reason) ProcessConstructor(
            IMethodSymbol ctor,
            ImmutableArray<IPropertySymbol> properties)
        {
            var parameters = ctor.Parameters;
            if (parameters.Length != properties.Length)
            {
                return (ctor, default, "parameter/property count mismatch");
            }
            var names = new Dictionary<IPropertySymbol, string>(SymbolEqualityComparer.Default);
            foreach (var property in properties)
            {
                if (!parameters.TryGetFirst(parameter => StringComparer.InvariantCultureIgnoreCase.Equals(parameter.Name, property.Name), out var match))
                {
                    return (ctor, default, $"no parameter for property \"{property.Name}\" ({string.Join(", ", parameters.Select(parameter => parameter.Name))})");
                }
                names[property] = match.Name;
            }
            return (ctor, names, default);
        }

        static (IMethodSymbol Constructor, Dictionary<IPropertySymbol, string> ConstructorParameterNames) ChooseConstructor(ITypeSymbol source, ImmutableArray<IPropertySymbol> properties)
        {
            var constructors = source.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
                .Select(ctor => ProcessConstructor(ctor, properties))
                .ToList();
            var matchingConstructors = constructors
                .Where(tup => tup.ConstructorParameterNames is not null)
                .Select(tup => (tup.Constructor, tup.ConstructorParameterNames!))
                .ToList();
            if (matchingConstructors.Count == 0)
            {
                throw new NoMatchingConstructorException(
                    source.Name,
                    constructors
                        .Where(tup => !string.IsNullOrEmpty(tup.Reason))
                        .Select(tup => tup.Reason!)
                        .ToArray()
                );
            }
            if (matchingConstructors.Count > 1)
            {
                throw new InvalidOperationException($"Multiple matching constructors for {source.Name}.");
            }
            return matchingConstructors[0];
        }

        static bool HasMethod(ITypeSymbol? source, string name)
            => source is not null && source.GetMembers().Any(m => m is IMethodSymbol ms && StringComparer.Ordinal.Equals(ms.Name, name));
    }

    private static IEnumerable<DescriptorOrError> EnumerateDescriptors(ImmutableArray<BuilderTarget> targets, CancellationToken cancellationToken)
    {
        if (targets.Length == 0)
        {
            yield break;
        }
        var semanticModel = targets[0].SemanticModel;
        if (targets.TryGetFirst(target => !ReferenceEquals(target.SemanticModel, semanticModel), out var xtarget))
        {
            yield return new DiagnosticData(
                descriptor: DiagnosticDescriptors.InvalidSemanticModel,
                location: xtarget.Node.GetLocation(),
                []
            );
            yield break;
        }
        var context = new BuilderTargetContext(semanticModel, new HashSet<string>(targets.Select(target => target.TargetFullName), StringComparer.Ordinal));
        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<DescriptorOrError> results = new(4);
            try
            {
                var builderPartial = semanticModel.Compilation.GetTypeByMetadataName(target.TargetFullName);
                results.Add(new BuilderDescriptor(
                    builderNamespace: target.TargetNamespace,
                    builderName: target.TargetName,
                    generateDocumentationComment: builderPartial is null,
                    sourceType: context.GetOrCreate(target.Type, false),
                    members: ExtractMembers(context, target.Type, builderPartial, cancellationToken).ToImmutableArray()
                ));
            }
            catch (BuilderGenerationException exn)
            {
                results.Add(exn.DiagnosticData);
            }
            catch (NoMatchingConstructorException exn)
            {
                foreach (var reason in exn.Reasons)
                {
                    results.Add(new DiagnosticData(
                        descriptor: DiagnosticDescriptors.NoMatchingConstructor,
                        location: default,
                        messageArgs: [exn.Target, reason]
                    ));
                }
            }
            catch (Exception exn)
            {
                results.Add(new DiagnosticData(
                    descriptor: DiagnosticDescriptors.UnexpectedError,
                    location: default,
                    messageArgs: [exn.GetType().FullName, exn.Message, exn.StackTrace]
                ));
            }
            foreach (var result in results)
            {
                yield return result;
            }
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context => context.AddSource("HasBuilderAttribute.g.cs", SourceText.From(attributeSource, Utf8)));

        IncrementalValuesProvider<TargetOrError> targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NCoreUtils.Data.HasBuilderAttribute",
            (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
            (ctx, cancellationToken) =>
            {
                if (!ctx.SemanticModel.Compilation.HasLanguageVersionAtLeastEqualTo(LanguageVersion.CSharp10, out _))
                {
                    return new TargetOrError(new DiagnosticData(
                        descriptor: DiagnosticDescriptors.CSharpVersionError,
                        location: default,
                        messageArgs: []
                    ));
                }
                if (ctx.TargetSymbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    return default;
                }
                return new TargetOrError(new BuilderTarget(ctx.SemanticModel, ctx.TargetNode, namedTypeSymbol));
            }
        );

        var realTargets = targets
            .Where(source => source.Target is not null)
            .Select((source, _) => source.Target!);

        var allTargets2 = realTargets.Collect();

        var descriptorsAndErrors = allTargets2.SelectMany(EnumerateDescriptors);

        var descriptors = descriptorsAndErrors
            .Where(tup => tup.Descriptor is not null)
            .Select((tup, _) => tup.Descriptor!);

        context.RegisterSourceOutput(descriptors, (ctx, descriptor) =>
        {
            try
            {
                var unitSyntax = BuilderEmitter.EmitCompilationUnit(descriptor);
                ctx.AddSource($"{descriptor.SourceType.SafeName}Builder.g.cs", unitSyntax.GetText(Utf8));
            }
            catch (BuilderGenerationException exn)
            {
                var err = exn.DiagnosticData;
                ctx.ReportDiagnostic(Diagnostic.Create(
                    descriptor: err.Descriptor,
                    location: err.Location,
                    messageArgs: err.MessageArgs
                ));
            }
            catch (Exception exn)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DiagnosticDescriptors.UnexpectedError,
                    location: default,
                    messageArgs: [exn.GetType().FullName, exn.Message, exn.StackTrace]
                ));
            }
        });

        var errors = descriptorsAndErrors
            .Where(tup => tup.Error is not null)
            .Select((tup, _) => tup.Error!);

        context.RegisterImplementationSourceOutput(errors, (ctx, err) =>
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor: err.Descriptor,
                location: err.Location,
                messageArgs: err.MessageArgs
            ));
        });
    }
}