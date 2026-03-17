using DateTimeDetector.Analyzers;
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

namespace DateTimeDetector.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeUsageCodeFixProvider)), Shared]
public class DateTimeUsageCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [DateTimeUsageAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var identifierNode = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<IdentifierNameSyntax>()
            .FirstOrDefault(n => n.Identifier.ValueText == "DateTime");

        if (identifierNode is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace DateTime with DateTimeOffset",
                createChangedDocument: ct => ReplaceWithDateTimeOffset(context.Document, identifierNode, ct),
                equivalenceKey: "ReplaceDateTimeWithDateTimeOffset"),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithDateTimeOffset(
        Document document,
        IdentifierNameSyntax identifierNode,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var leadingTrivia = identifierNode.GetLeadingTrivia();
        var trailingTrivia = identifierNode.GetTrailingTrivia();

        var newIdentifier = SyntaxFactory.IdentifierName("DateTimeOffset")
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(trailingTrivia);

        var newRoot = root.ReplaceNode(identifierNode, newIdentifier);

        return document.WithSyntaxRoot(newRoot);
    }
}
