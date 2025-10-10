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
        messageFormat: "Unable to determine default value for {0}, add method {1} returning default value to the builder.",
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