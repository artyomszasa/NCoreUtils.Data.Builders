using System.Linq;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal partial class TypeDescriptor
{
    private static string GetSafeName(ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol named && named.IsGenericType)
        {
            return $"{symbol.Name}Of{string.Join(string.Empty, named.TypeArguments.Select(GetSafeName))}";
        }
        if (symbol is IArrayTypeSymbol array)
        {
            return $"ArrayOf{GetSafeName(array.ElementType)}";
        }
        return symbol.Name;
    }

    public static TypeDescriptor Create(ITypeSymbol type, BuilderTargetContext context, bool isBuilder)
    {
        var (isRefList, isStringList, isInt32List, elementType) = isBuilder
            ? (false, false, false, default)
            : context.IsRefList(type, out var etype)
                ? (true, false, false, context.GetOrCreate(etype, false))
                : context.IsStringList(type)
                    ? (false, true, false, context.GetOrCreate(context.String, false))
                    : context.IsInt32List(type)
                        ? (false, false, true, context.GetOrCreate(context.Int32, false))
                        : context.IsReadOnlyList(type, out etype)
                            ? (false, false, false, context.GetOrCreate(etype, false))
                            : (false, false, false, default);

        return new TypeDescriptor(
            type.IsValueType,
            !type.IsValueType && type.NullableAnnotation == NullableAnnotation.Annotated,
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            GetSafeName(type),
            isBuilder,
            isRefList,
            isStringList,
            isInt32List,
            elementType
        );
    }
}