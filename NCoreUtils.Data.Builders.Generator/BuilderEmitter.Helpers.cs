using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal partial class BuilderEmitter
{
    public static MemberAccessExpressionSyntax SimpleMemberAccessExpression(
        ExpressionSyntax host,
        SimpleNameSyntax name)
        => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, host, name);

    private static IsPatternExpressionSyntax IsNullExpression(ExpressionSyntax expression)
        => IsPatternExpression(expression, ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression)));

    private static ArgumentListSyntax Args(ExpressionSyntax singleArg)
        => ArgumentList(SingletonSeparatedList(
            Argument(singleArg)
        ));

    private static ArgumentListSyntax Args(params ExpressionSyntax[] args)
        => ArgumentList(SeparatedList(args.Select(Argument)));

    public static ObjectCreationExpressionSyntax NewExpression(TypeSyntax type, ExpressionSyntax singleArg)
        => ObjectCreationExpression(type, Args(singleArg), null);

    public static ObjectCreationExpressionSyntax NewExpression(TypeSyntax type, params ExpressionSyntax[] args)
        => ObjectCreationExpression(type, Args(args), null);

    public static InvocationExpressionSyntax SimpleInvocationExpression(
        ExpressionSyntax expression,
        ExpressionSyntax singleArg)
        => InvocationExpression(
            expression,
            Args(singleArg)
        );

    public static InvocationExpressionSyntax SimpleInvocationExpression(
        ExpressionSyntax expression,
        params ExpressionSyntax[] args)
        => InvocationExpression(
            expression,
            Args(args)
        );


    private static ArgumentSyntax NamedArg(IdentifierNameSyntax name, ExpressionSyntax expression) => Argument(
        nameColon: NameColon(name),
        refKindKeyword: default,
        expression: expression
    );

    private static ArgumentSyntax NamedArg(string name, ExpressionSyntax expression)
        => NamedArg(IdentifierName(name), expression);
}