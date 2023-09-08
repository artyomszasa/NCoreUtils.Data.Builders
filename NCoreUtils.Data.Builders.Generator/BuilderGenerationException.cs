using System;

namespace NCoreUtils.Data;

internal class BuilderGenerationException : InvalidOperationException
{
    public DiagnosticData DiagnosticData { get; }

    public BuilderGenerationException(DiagnosticData diagnosticData)
    {
        DiagnosticData = diagnosticData ?? throw new ArgumentNullException(nameof(diagnosticData));
    }
}