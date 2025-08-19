using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;
using ToListinator.Utils;
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
        var binaryExpression = await CodeFixHelper.FindTargetNode<BinaryExpressionSyntax>(
            context,
            NullCoalescingForeachAnalyzer.DiagnosticId);

        if (binaryExpression == null)
        {
            return;
        }

        var diagnostic = CodeFixHelper.GetDiagnostic(context, NullCoalescingForeachAnalyzer.DiagnosticId);
        if (diagnostic == null)
        {
            return;
        }

        var action = CodeFixHelper.CreateSimpleAction(
            "Replace with null check and foreach",
            "ReplaceWithNullCheck",
            ReplaceWithNullCheck,
            context,
            binaryExpression);

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

        // Create the new foreach statement (without the null coalescing)
        var newForeachStatement = foreachStatement
            .WithExpression(nullableExpression.WithoutTrivia())
            .WithoutTrivia();

        // Create the null check condition
        var nullCheck = BinaryExpression(
            SyntaxKind.NotEqualsExpression,
            nullableExpression.WithoutTrivia(),
            LiteralExpression(SyntaxKind.NullLiteralExpression));

        // Create the if statement using trivia utilities
        var ifStatement = IfStatement(
            nullCheck,
            Block(SingletonList<StatementSyntax>(newForeachStatement)))
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Use TriviaHelper to transfer trivia from the original foreach statement
        var ifStatementWithTrivia = TriviaHelper.TransferTrivia(foreachStatement, ifStatement);

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        var newRoot = root?.ReplaceNode(foreachStatement, ifStatementWithTrivia);

        if (newRoot is null)
        {
            return document;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
