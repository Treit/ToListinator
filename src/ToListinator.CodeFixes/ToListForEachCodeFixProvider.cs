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

namespace ToListinator.CodeFixes
{
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
            var diagnostic = context.Diagnostics.First(diag => diag.Id == ToListForEachAnalyzer.DiagnosticId);
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var invocation = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            if (invocation == null)
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
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Ensure .ToList()
            if (toListInvocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "ToList", Expression: var originalCollection })
                return document;

            // Walk up to .ForEach(...)
            if (toListInvocation.Parent is not MemberAccessExpressionSyntax { Name.Identifier.Text: "ForEach" } forEachAccess ||
                forEachAccess.Parent is not InvocationExpressionSyntax forEachInvocation)
                return document;

            var argumentExpr = forEachInvocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            if (argumentExpr == null)
                return document;

            ParameterSyntax parameter;
            BlockSyntax body;

            // We need to handle lambdas, but ideally also method group and delegate keyword invocations.
            switch (argumentExpr)
            {
                case SimpleLambdaExpressionSyntax simpleLambda:
                    parameter = simpleLambda.Parameter;
                    body = simpleLambda.Body is BlockSyntax b
                        ? b
                        : SyntaxFactory.Block(SyntaxFactory.ExpressionStatement((ExpressionSyntax)simpleLambda.Body));
                    break;

                case AnonymousMethodExpressionSyntax anon when anon.ParameterList?.Parameters.Count == 1 && anon.Block != null:
                    parameter = anon.ParameterList.Parameters[0];
                    body = anon.Block;
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
                statement: body)
                .WithLeadingTrivia(forEachInvocation.GetLeadingTrivia())
                .WithTrailingTrivia(forEachInvocation.GetTrailingTrivia());

            var statementToReplace = forEachInvocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (statementToReplace == null)
                return document;

            var newRoot = root.ReplaceNode(statementToReplace, foreachStatement);
            return document.WithSyntaxRoot(newRoot);
        }


    }
}
