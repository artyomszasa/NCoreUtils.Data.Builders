using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal class BuilderCustomFieldDescriptor(
    string fieldName,
    TypeDescriptor fieldType)
    : IEquatable<BuilderCustomFieldDescriptor>
{
    public static bool operator ==(BuilderCustomFieldDescriptor? a, BuilderCustomFieldDescriptor? b)
        => a is null
            ? b is null
            : a.Equals(b);

    public static bool operator !=(BuilderCustomFieldDescriptor? a, BuilderCustomFieldDescriptor? b)
        => a is null
            ? b is not null
            : !a.Equals(b);

    private static ReadOnlySpan<char> InitializePrefix => "Initialize";

    private static string CreateInitializerName(string fieldName)
    {
        var buffer = ArrayPool<char>.Shared.Rent(1024);
        try
        {
            var sourceOffset = 0;
            var sourceSpan = fieldName.AsSpan();
            if (sourceSpan.IsEmpty)
            {
                throw new InvalidOperationException("Cannot generate initializer name for empty string.");
            }
            // skip dashes
            while (sourceSpan[sourceOffset] == '_')
            {
                ++sourceOffset;
            }
            if (sourceSpan.Length <= sourceOffset)
            {
                throw new InvalidOperationException("Cannot generate initializer name for dash-only string.");
            }
            var targetSpan = buffer.AsSpan();
            InitializePrefix.CopyTo(targetSpan);
            targetSpan[10] = char.ToUpperInvariant(sourceSpan[sourceOffset++]);
            var rem = sourceSpan.Slice(sourceOffset);
            if (rem.IsEmpty)
            {
                return new string(buffer, 0, 1);
            }
            rem.CopyTo(targetSpan.Slice(11));
            return new(buffer, 0, 11 + rem.Length);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private IdentifierNameSyntax? _fieldSyntax;

    private IdentifierNameSyntax? _initializerNameSyntax;

    public string FieldName { get; } = fieldName;

    public TypeDescriptor FieldType { get; } = fieldType;

    public IdentifierNameSyntax FieldSyntax => _fieldSyntax ??= IdentifierName(FieldName);

    public IdentifierNameSyntax InitializerNameSyntax => _initializerNameSyntax ??= IdentifierName(CreateInitializerName(FieldName));

    public bool Equals([NotNullWhen(false)] BuilderCustomFieldDescriptor? other)
        => ReferenceEquals(this, other)
            || (other is not null
                && StringComparer.Ordinal.Equals(FieldName, other.FieldName)
                && FieldType == other.FieldType);

    public override bool Equals([NotNullWhen(false)] object? obj)
        => Equals(obj as BuilderCustomFieldDescriptor);

    public override int GetHashCode()
        // FIXME: better hash
        => StringComparer.Ordinal.GetHashCode(FieldName) ^ FieldType.GetHashCode();
}

