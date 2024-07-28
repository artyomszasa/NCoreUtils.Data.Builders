using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NCoreUtils.Data;

[Generator]
public class BuilderGenerator : IIncrementalGenerator
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

    private const string attributeSource = @"#nullable enable
namespace NCoreUtils.Data
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class HasBuilderAttribute : System.Attribute
    {
        public HasBuilderAttribute() { /* noop */ }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class BuilderFieldAttribute : System.Attribute
    {
        public BuilderFieldAttribute() { /* noop */ }
    }

    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class BuilderIgnoreAttribute : System.Attribute
    {
        public BuilderIgnoreAttribute() { /* noop */ }
    }

    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class BuilderFieldTypeAttribute : System.Attribute
    {
        public System.Type FieldType { get; }

        public BuilderFieldTypeAttribute(System.Type fieldType)
            => FieldType = fieldType;
    }

    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class BuilderPropertyNameAttribute : System.Attribute
    {
        public string PropertyName { get; }

        public BuilderPropertyNameAttribute(string propertyName)
            => PropertyName = propertyName;
    }
}";

    private static UTF8Encoding Utf8 { get; } = new(false);

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
                    return default;
                }
                if (ctx.TargetSymbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    return default;
                }
                return new TargetOrError(new BuilderTarget(ctx.SemanticModel, ctx.TargetNode, namedTypeSymbol));
            }
        );

        var allTargets = targets.Collect();

        context.RegisterSourceOutput(allTargets, (ctx, items) =>
        {
            var builderNames = new List<string>(items.Length);
            foreach (var item in items)
            {
                if (item.Target is not null)
                {
                    builderNames.Add(item.Target.TargetFullName);
                }
            }
            foreach (var targetOrError in items)
            {
                var target = targetOrError.Target;
                if (target is null)
                {
                    var err = targetOrError.Error!;
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        descriptor: err.Descriptor,
                        location: err.Location,
                        messageArgs: err.MessageArgs
                    ));
                    return;
                }
                try
                {
                    var emitter = new BuilderEmitter(target.SemanticModel);
                    var unitSyntax = emitter.EmitCompilationUnit(target, builderNames);
                    ctx.AddSource($"{target.Type.Name}Builder.g.cs", unitSyntax.GetText(Utf8));
                }
                catch (BuilderGenerationException exn)
                {
                    var err = exn.DiagnosticData;
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        descriptor: err.Descriptor,
                        location: err.Location ?? target.Node.GetLocation(),
                        messageArgs: err.MessageArgs
                    ));
                }
                catch (Exception exn)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DiagnosticDescriptors.UnexpectedError,
                        location: default,
                        messageArgs: new object[] { exn.GetType().FullName, exn.Message, exn.StackTrace }
                    ));
                }
            }
        });
    }
}