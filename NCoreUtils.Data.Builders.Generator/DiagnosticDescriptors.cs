using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal static class DiagnosticDescriptors
{
    public static DiagnosticDescriptor IncompatibleLanguageVersion { get; } = new DiagnosticDescriptor(
        id: "NUB0001",
        title: "Incompatible C# version.",
        messageFormat: "Must target at least C# language version 10 to use builder generator (target version is {0}).",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor NoBuilderFor { get; } = new DiagnosticDescriptor(
        id: "NUB0002",
        title: "No builder found.",
        messageFormat: "No builder found for type {0} (expected {1}) required by property {2}.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor NoDefaultValue { get; } = new DiagnosticDescriptor(
        id: "NUB0003",
        title: "No default value.",
        messageFormat: "Unable to determine default value for {0} ({2}), add method {1} returning default value to the builder.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor NoMatchingConstructor { get; } = new DiagnosticDescriptor(
        id: "NUB0004",
        title: "No matching constructor.",
        messageFormat: "Cannot use {0} ctor: {1}.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor InvalidSemanticModel { get; } = new DiagnosticDescriptor(
        id: "NUB0005",
        title: "Invalid semantic model.",
        messageFormat: "Semantic model must be the same for all builders in assembly",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor CSharpVersionError { get; } = new DiagnosticDescriptor(
        id: "NUB0005",
        title: "C# version errror.",
        messageFormat: "Builder generator only usable with C# 10 and above.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor UnexpectedError { get; } = new DiagnosticDescriptor(
        id: "NUB0000",
        title: "Unexpected error occured.",
        messageFormat: "{0}: {1} | {2}",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}