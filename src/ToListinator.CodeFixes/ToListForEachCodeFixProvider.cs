using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToListForEachCodeFixProvider)), Shared]
public class ToListForEachCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [ToListForEachAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First(diag => diag.Id == ToListForEachAnalyzer.DiagnosticId);
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var invocation = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation is null)
        {
            return;
        }

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

        InvocationExpressionSyntax? forEachInvocation = null;
        ExpressionSyntax? originalCollection = null;

        if (toListInvocation is not
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
                        ArgumentList.Arguments: [{ Expression: { } argumentExpr }],
                    } matchedForEachInvocation,
                },
            }
        )
        {
            // It's a more complex expression, possibly a chained set of calls, so try to
            // find the actual ToList().ForEach() expression by walking the chain.
            forEachInvocation = TryFindToListForEachChain(toListInvocation);
        }
        else
        {
            forEachInvocation = matchedForEachInvocation;
            originalCollection = matchedOriginalCollection;
        }

        if (forEachInvocation is null)
        {
            return document;
        }

        if (forEachInvocation.Expression is not MemberAccessExpressionSyntax forEachAccess ||
            forEachAccess.Name.Identifier.Text != "ForEach")
        {
            return document;
        }

        if (forEachInvocation.ArgumentList.Arguments is not [{ Expression: var forEachArg }])
        {
            return document;
        }

        BlockSyntax? body = null;
        ParameterSyntax? parameter = null;

        switch (forEachArg)
        {
            case SimpleLambdaExpressionSyntax simpleLambda:
                parameter = simpleLambda.Parameter;
                body = simpleLambda.Body switch
                {
                    BlockSyntax b => b,
                    ExpressionSyntax e => SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(e)),
                    _ => null
                };
                break;

            case ParenthesizedLambdaExpressionSyntax parenLambda when parenLambda.ParameterList.Parameters.Count == 1:
                parameter = parenLambda.ParameterList.Parameters[0];
                body = parenLambda.Body switch
                {
                    BlockSyntax b => b,
                    ExpressionSyntax e => SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(e)),
                    _ => null
                };
                break;

            case IdentifierNameSyntax methodGroup:
                // Could generate: foreach (var x in ...) methodGroup(x);
                parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("x"));
                body = SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(methodGroup)
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("x")))))));
                break;
        }

        if (forEachAccess.Expression is not InvocationExpressionSyntax toListInvocation2 ||
            toListInvocation2.Expression is not MemberAccessExpressionSyntax toListAccess ||
            toListAccess.Name.Identifier.Text != "ToList" ||
            parameter is null)
        {
            return document;
        }

        originalCollection ??= toListAccess.Expression;

        var foreachStatement = SyntaxFactory.ForEachStatement(
            attributeLists: default,
            awaitKeyword: default,
            forEachKeyword: SyntaxFactory.Token(SyntaxKind.ForEachKeyword),
            openParenToken: SyntaxFactory.Token(SyntaxKind.OpenParenToken),
            type: SyntaxFactory.IdentifierName("var"),
            identifier: parameter.Identifier,
            inKeyword: SyntaxFactory.Token(SyntaxKind.InKeyword),
            expression: originalCollection.WithoutTrailingTrivia(),
            closeParenToken: SyntaxFactory.Token(SyntaxKind.CloseParenToken),
            statement: body!);

        var expressionStatement = forEachInvocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (expressionStatement is null)
        {
            return document;
        }

        var root = (await document.GetSyntaxRootAsync(cancellationToken))!;
        var newRoot = root.ReplaceNode(expressionStatement, foreachStatement.WithTriviaFrom(forEachInvocation));
        return document.WithSyntaxRoot(newRoot);
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
            if (memberAccess.Name.Identifier.Text == "ToList")
            {
                if (invocation.Parent is MemberAccessExpressionSyntax forEachAccess &&
                    forEachAccess.Name.Identifier.Text == "ForEach" &&
                    forEachAccess.Parent is InvocationExpressionSyntax forEachInvocation)
                {
                    return forEachInvocation;
                }
            }

            current = invocation;
        }

        return null;
    }
}
