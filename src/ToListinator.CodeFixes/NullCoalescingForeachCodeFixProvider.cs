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
using ToListinator.Analyzers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCoalescingForeachCodeFixProvider)), Shared]
public class NullCoalescingForeachCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [NullCoalescingForeachAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var diagnostic = context.Diagnostics.First(diag => diag.Id == NullCoalescingForeachAnalyzer.DiagnosticId);
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var binaryExpression = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<BinaryExpressionSyntax>()
            .FirstOrDefault();

        if (binaryExpression is null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: "Replace with null check and foreach",
            createChangedDocument: c => ReplaceWithNullCheck(context.Document, binaryExpression, c),
            equivalenceKey: "ReplaceWithNullCheck");

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> ReplaceWithNullCheck(
        Document document,
        BinaryExpressionSyntax binaryExpression,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var foreachStatement = binaryExpression.FirstAncestorOrSelf<ForEachStatementSyntax>();
        if (foreachStatement is null)
        {
            return document;
        }

        var nullableExpression = binaryExpression.Left;
        
        // Preserve the original leading trivia (indentation and comments)
        var originalLeadingTrivia = foreachStatement.GetLeadingTrivia();
        var originalTrailingTrivia = foreachStatement.GetTrailingTrivia();

        // Create the new foreach statement (without the null coalescing)
        var newForeachStatement = foreachStatement
            .WithExpression(nullableExpression.WithoutTrivia())
            .WithoutTrivia();

        // Create the null check condition
        var nullCheck = BinaryExpression(
            SyntaxKind.NotEqualsExpression,
            nullableExpression.WithoutTrivia(),
            LiteralExpression(SyntaxKind.NullLiteralExpression));

        // Create the if statement, preserving the original indentation
        var ifStatement = IfStatement(
            nullCheck,
            Block(SingletonList<StatementSyntax>(newForeachStatement)))
            .WithLeadingTrivia(originalLeadingTrivia)
            .WithTrailingTrivia(originalTrailingTrivia);

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        var newRoot = root?.ReplaceNode(foreachStatement, ifStatement);

        if (newRoot is null)
        {
            return document;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
