using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IdentitySelectCodeFixProvider)), Shared]
public class IdentitySelectCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [IdentitySelectAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics
            .First(diag => diag.Id == IdentitySelectAnalyzer.DiagnosticId);

        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

        var invocation = root?.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation is null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: "Remove identity Select",
            createChangedDocument: c => RemoveIdentitySelect(context.Document, invocation, c),
            equivalenceKey: "RemoveIdentitySelect");

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> RemoveIdentitySelect(
        Document document,
        InvocationExpressionSyntax selectInvocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = (await document.GetSyntaxRootAsync(cancellationToken))!;

        if (selectInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var sourceExpression = memberAccess.Expression;

            // Replace the entire Select invocation with just the source expression
            var newRoot = root.ReplaceNode(
                selectInvocation,
                sourceExpression.WithTriviaFrom(selectInvocation));

            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }
}
