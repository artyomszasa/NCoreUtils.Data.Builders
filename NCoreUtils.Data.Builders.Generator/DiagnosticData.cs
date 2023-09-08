using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal sealed class DiagnosticData
{
    public DiagnosticDescriptor Descriptor { get; }

    public Location? Location { get; }

    public object?[]? MessageArgs { get; }

    public DiagnosticData(DiagnosticDescriptor descriptor, Location? location, object?[]? messageArgs)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArgs = messageArgs;
    }
}