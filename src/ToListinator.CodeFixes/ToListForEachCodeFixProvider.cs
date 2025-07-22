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

            // toListInvocation is 'list.ToList()'
            if (toListInvocation.Expression is not MemberAccessExpressionSyntax toListAccess ||
                toListAccess.Name.Identifier.Text != "ToList")
                return document;

            // 'list.ToList().ForEach'
            if (toListInvocation.Parent is not MemberAccessExpressionSyntax forEachAccess ||
                forEachAccess.Name.Identifier.Text != "ForEach")
                return document;

            // 'list.ToList().ForEach(...)'
            if (forEachAccess.Parent is not InvocationExpressionSyntax forEachInvocation)
                return document;

            // Ensure lambda expression exists
            if (forEachInvocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not SimpleLambdaExpressionSyntax lambda)
                return document;

            // ✅ Get 'list' from 'list.ToList()'
            if (toListAccess.Expression is not ExpressionSyntax originalCollection)
                return document;

            // Build: foreach (var x in list) { body }
            var foreachStatement = SyntaxFactory.ForEachStatement(
                attributeLists: default,
                awaitKeyword: default,
                forEachKeyword: SyntaxFactory.Token(SyntaxKind.ForEachKeyword),
                openParenToken: SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                type: SyntaxFactory.IdentifierName("var"),
                identifier: lambda.Parameter.Identifier,
                inKeyword: SyntaxFactory.Token(SyntaxKind.InKeyword),
                expression: originalCollection.WithoutTrivia(),
                closeParenToken: SyntaxFactory.Token(SyntaxKind.CloseParenToken),
                statement: lambda.Body is BlockSyntax block
                    ? block
                    : SyntaxFactory.Block(SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body)))
                .WithLeadingTrivia(forEachInvocation.GetLeadingTrivia())
                .WithTrailingTrivia(forEachInvocation.GetTrailingTrivia());

            // Replace the whole statement that contains the ForEach call
            var statementToReplace = forEachInvocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (statementToReplace == null)
                return document;

            var newRoot = root.ReplaceNode(statementToReplace, foreachStatement);
            return document.WithSyntaxRoot(newRoot);
        }

    }
}
