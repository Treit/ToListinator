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
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            // TODO: Implement the logic to replace the invocation with a foreach loop.


            // TEMPORARY: Just append some string to the end to verify the fix runs.
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var lastToken = root!.GetLastToken();

            var commentTrivia = SyntaxFactory.Comment("// CodeFix was here");
            var newTrailingTrivia = lastToken.TrailingTrivia
                .Add(commentTrivia);

            var newLastToken = lastToken.WithTrailingTrivia(newTrailingTrivia);
            var newRoot = root.ReplaceToken(lastToken, newLastToken);

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}
