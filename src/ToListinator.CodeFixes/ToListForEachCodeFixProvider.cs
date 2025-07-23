using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.CodeFixes;

internal record ForEachInvocationData(
    InvocationExpressionSyntax? ForEachInvocation,
    ExpressionSyntax? OriginalCollection);

internal record ForEachArgumentData(
    ParameterSyntax? Parameter,
    BlockSyntax? Body);

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToListForEachCodeFixProvider)), Shared]
public class ToListForEachCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [ToListForEachAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First(diag => diag.Id == ToListForEachAnalyzer.DiagnosticId);
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root?.FindNode(diagnosticSpan) is not InvocationExpressionSyntax invocation)
            return;

        var action = CodeAction.Create(
            title: "Replace with foreach loop",
            createChangedDocument: c => ReplaceWithForeachLoop(context.Document, invocation, c),
            equivalenceKey: "ReplaceWithForeachLoop");

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> ReplaceWithForeachLoop(
        Document document,
        InvocationExpressionSyntax toListInvocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var invocationData = FindForEachInvocation(toListInvocation);

        if (!IsValidForEachTransformation(
            invocationData,
            out var forEachAccess,
            out var toListAccess))
        {
            return document;
        }

        if (invocationData.ForEachInvocation?.ArgumentList.Arguments is not [{ Expression: var forEachArg }])
            return document;

        if (ParseForEachArgument(forEachArg) is not { } argumentData)
            return document;

        var originalCollection = invocationData.OriginalCollection ?? toListAccess.Expression;

        var foreachStatement = CreateForEachStatement(argumentData.Parameter!, originalCollection, argumentData.Body!);
        var expressionStatement = invocationData.ForEachInvocation!.FirstAncestorOrSelf<ExpressionStatementSyntax>();

        if (expressionStatement is null)
            return document;

        var formattedForeach = ApplyTrivia(foreachStatement, expressionStatement);
        var root = (await document.GetSyntaxRootAsync(cancellationToken))!;
        var newRoot = root.ReplaceNode(expressionStatement, formattedForeach);

        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsValidForEachTransformation(
        ForEachInvocationData invocationData,
        [NotNullWhen(true)] out MemberAccessExpressionSyntax? forEachAccess,
        [NotNullWhen(true)] out MemberAccessExpressionSyntax? toListAccess)
    {
        if (invocationData is
            {
                ForEachInvocation: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name.Identifier.Text: "ForEach",
                        Expression: InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name.Identifier.Text: "ToList"
                            } toList
                        }
                    } forEach
                }
            })
        {
            forEachAccess = forEach;
            toListAccess = toList;
            return true;
        }

        forEachAccess = null;
        toListAccess = null;
        return false;
    }


    private static ForEachStatementSyntax CreateForEachStatement(
        ParameterSyntax parameter,
        ExpressionSyntax originalCollection,
        BlockSyntax body)
    {
        // Remove leading trivia. We will add it back to the final code later.
        originalCollection = originalCollection.WithoutLeadingTrivia();

        return ForEachStatement(
            attributeLists: default,
            awaitKeyword: default,
            forEachKeyword: Token(SyntaxKind.ForEachKeyword),
            openParenToken: Token(SyntaxKind.OpenParenToken),
            type: IdentifierName("var"),
            identifier: parameter.Identifier,
            inKeyword: Token(SyntaxKind.InKeyword),
            expression: originalCollection,
            closeParenToken: Token(SyntaxKind.CloseParenToken),
            statement: body);
    }

    private static ForEachStatementSyntax ApplyTrivia(
        ForEachStatementSyntax foreachStatement,
        ExpressionStatementSyntax expressionStatement)
    {
        var trailingTrivia = expressionStatement.SemicolonToken.TrailingTrivia;

        return foreachStatement
            .WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
            .WithCloseParenToken(foreachStatement.CloseParenToken.WithTrailingTrivia(trailingTrivia));
    }

    private static ForEachArgumentData? ParseForEachArgument(ExpressionSyntax forEachArg)
    {
        return forEachArg switch
        {
            // Matches list.ToList().ForEach(x => Console.WriteLine(x));
            SimpleLambdaExpressionSyntax simpleLambda => new(
                simpleLambda.Parameter,
                simpleLambda.Body switch
                {
                    BlockSyntax b => b,
                    ExpressionSyntax e => Block(ExpressionStatement(e)),
                    _ => null
                }
            ),

            // Matches list.ToList().ForEach((x) => Console.WriteLine(x));
            ParenthesizedLambdaExpressionSyntax parenLambda when parenLambda.ParameterList.Parameters.Count == 1 => new(
                parenLambda.ParameterList.Parameters[0],
                parenLambda.Body switch
                {
                    BlockSyntax b => b,
                    ExpressionSyntax e => Block(ExpressionStatement(e)),
                    _ => null
                }
            ),

            // Matches list.OrderBy(x => x).ToList().ForEach(Print);
            IdentifierNameSyntax methodGroup => new(
                Parameter(Identifier("x")),
                Block(
                    ExpressionStatement(
                        InvocationExpression(methodGroup)
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(IdentifierName("x")))))))
            ),

            // Matches list.Where(x => x > 0).ToList().ForEach(delegate(int item) { Console.WriteLine(item); });
            AnonymousMethodExpressionSyntax anonymousMethod
                when anonymousMethod.ParameterList?.Parameters.Count == 1
                => new(
                    anonymousMethod.ParameterList.Parameters[0],
                    anonymousMethod.Body is BlockSyntax originalBody
                        ? Block(originalBody.Statements)
                        : null
            ),

            _ => null,
        };
    }

    private static ForEachInvocationData FindForEachInvocation(
        InvocationExpressionSyntax toListInvocation)
    {
        if (toListInvocation is
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ToList",
                    Expression: var matchedOriginalCollection,
                },
                Parent: MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ForEach",
                    Parent: InvocationExpressionSyntax
                    {
                        ArgumentList.Arguments: [{ Expression: { } }],
                    } matchedForEachInvocation,
                },
            }
        )
        {
            // It's a direct ToList().ForEach() already.
            return new(matchedForEachInvocation, matchedOriginalCollection);
        }
        else
        {
            // It's a more complex expression, possibly a chained set of calls, so try to
            // find the actual ToList().ForEach() expression by walking the chain.
            var forEachInvocation = TryFindToListForEachChain(toListInvocation);
            return new(forEachInvocation, null);
        }
    }

    // We could have a chain of calls, like Select().Where().OrderBy().Take().ToList().ForEach().
    // This code needs to handle this as well as the simple case of just a plain ToList().ForEach()
    // directly on a collection. This method walks the starting node's parents until it gets to the
    // actual ToList().ForEach() call that we want to replace.
    private static InvocationExpressionSyntax? TryFindToListForEachChain(SyntaxNode node)
    {
        var current = node;

        while (current?.Parent is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Parent is InvocationExpressionSyntax invocation)
        {
            if (memberAccess.Name.Identifier.Text is "ToList" &&
                invocation.Parent is MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ForEach",
                    Parent: InvocationExpressionSyntax forEachInvocation,
                })
            {
                return forEachInvocation;
            }

            current = invocation;
        }

        return null;
    }
}
