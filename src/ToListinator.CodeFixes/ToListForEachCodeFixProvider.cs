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

        // Ensure .ToList()
        if (toListInvocation is not
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ToList",
                    Expression: var originalCollection,
                },
                Parent: MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ForEach",
                    Parent: InvocationExpressionSyntax
                    {
                        ArgumentList.Arguments: [{ Expression: { } argumentExpr }],
                    } forEachInvocation,
                },
            }
        )
        {
            return document;
        }

        var statementToReplace = forEachInvocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (statementToReplace == null)
            return document;

        ParameterSyntax parameter;
        BlockSyntax body;

        // We need to handle lambdas, but ideally also method group and delegate keyword invocations.
        switch (argumentExpr)
        {
            case SimpleLambdaExpressionSyntax simpleLambda:
                parameter = simpleLambda.Parameter;
                body = simpleLambda.Body switch
                {
                    BlockSyntax b => b,
                    _ => SyntaxFactory.Block(SyntaxFactory.ExpressionStatement((ExpressionSyntax)simpleLambda.Body)),
                };

                break;

            case AnonymousMethodExpressionSyntax { ParameterList.Parameters: [{ } param], Block: { } block }:
                parameter = param;
                body = block;
                break;

            case IdentifierNameSyntax or MemberAccessExpressionSyntax:
                // Method group like: list.ToList().ForEach(LogItem)
                parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("x"));
                body = SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(argumentExpr, SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("x"))
                            )
                        ))
                    )
                );
                break;

            default:
                return document;
        }

        var foreachStatement = SyntaxFactory.ForEachStatement(
            attributeLists: default,
            awaitKeyword: default,
            forEachKeyword: SyntaxFactory.Token(SyntaxKind.ForEachKeyword),
            openParenToken: SyntaxFactory.Token(SyntaxKind.OpenParenToken),
            type: SyntaxFactory.IdentifierName("var"),
            identifier: parameter.Identifier,
            inKeyword: SyntaxFactory.Token(SyntaxKind.InKeyword),
            expression: originalCollection.WithoutTrivia(),
            closeParenToken: SyntaxFactory.Token(SyntaxKind.CloseParenToken),
            statement: body
        )
            .WithLeadingTrivia(forEachInvocation.GetLeadingTrivia())
            .WithTrailingTrivia(forEachInvocation.GetTrailingTrivia());

        var root = (await document.GetSyntaxRootAsync(cancellationToken))!;
        var newRoot = root.ReplaceNode(statementToReplace, foreachStatement);
        return document.WithSyntaxRoot(newRoot);
    }
}
