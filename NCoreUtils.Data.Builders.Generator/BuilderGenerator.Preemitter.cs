using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

public partial class BuilderGenerator
{
    private static (bool HasDefaultValueMethod, ExpressionSyntax Syntax) GetDefaultValueSyntaxFor(
        HashSet<string> knownBuilderNames,
        ITypeSymbol? targetSymbol,
        IPropertySymbol sourcePropertySymbol,
        string builderPropertyName,
        TypeDescriptor builderPropertyType)
    {
        if (builderPropertyType.IsBuilder || knownBuilderNames.Contains(builderPropertyType.QualifiedName))
        {
            return (false, BuilderEmitter.DefaultLiteral);
        }
        if (builderPropertyType.IsValueType)
        {
            return (false, BuilderEmitter.DefaultLiteral);
        }
        if (builderPropertyType.SafeName == "String")
        {
            return (false, BuilderEmitter.SimpleMemberAccessExpression(BuilderEmitter.Types.String, BuilderEmitter.IdentifierNames.Empty));
        }
        if (builderPropertyType.IsStringList)
        {
            return (false, BuilderEmitter.NewExpression(BuilderEmitter.Types.StringList));
        }
        if (builderPropertyType.IsInt32List)
        {
            return (false, BuilderEmitter.NewExpression(BuilderEmitter.Types.Int32List));
        }
        if (builderPropertyType.IsRefList)
        {
            var expr = InvocationExpression(
                BuilderEmitter.SimpleMemberAccessExpression(
                    BuilderEmitter.Types.RefList,
                    GenericName(
                        BuilderEmitter.Identifiers.Empty,
                        TypeArgumentList(SingletonSeparatedList(builderPropertyType.ElementType.Syntax))
                    )
                )
            );
            return (false, expr);
        }
        var getDefaultValueMethodName = Names.GetDefaultValueMethod(builderPropertyName);
        if (targetSymbol is not null && targetSymbol.GetMembers().OfType<IMethodSymbol>().TryGetFirst(m => m.Name == getDefaultValueMethodName, out var m))
        {
            return (true, InvocationExpression(IdentifierName(getDefaultValueMethodName)));
        }
        throw new BuilderGenerationException(new(
            descriptor: DiagnosticDescriptors.NoDefaultValue,
            location: sourcePropertySymbol.Locations.FirstOrDefault(),
            messageArgs: [sourcePropertySymbol.Name, getDefaultValueMethodName, builderPropertyType.QualifiedName]
        ));
    }
}