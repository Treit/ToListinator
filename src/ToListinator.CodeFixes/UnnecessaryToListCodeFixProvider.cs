using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;
using ToListinator.Utils;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnnecessaryToListCodeFixProvider)), Shared]
public class UnnecessaryToListCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [UnnecessaryToListAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var invocation = await CodeFixHelper.FindTargetNode<InvocationExpressionSyntax>(
            context,
            UnnecessaryToListAnalyzer.DiagnosticId);

        if (invocation == null)
        {
            return;
        }

        var diagnostic = CodeFixHelper.GetDiagnostic(context, UnnecessaryToListAnalyzer.DiagnosticId);
        if (diagnostic == null)
        {
            return;
        }

        var action = CodeFixHelper.CreateSimpleAction(
            "Remove unnecessary ToList() call",
            "RemoveUnnecessaryToList",
            RemoveUnnecessaryToList,
            context,
            invocation);

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> RemoveUnnecessaryToList(
        Document document,
        InvocationExpressionSyntax toListInvocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (toListInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return document;
        }

        var sourceExpression = memberAccess.Expression;

        // Extract trivia from the original ToList() invocation
        var originalLeadingTrivia = toListInvocation.GetLeadingTrivia();
        var originalTrailingTrivia = toListInvocation.GetTrailingTrivia();

        // Create the replacement expression (just the source without ToList())
        var replacementExpression = sourceExpression.WithoutTrivia()
            .WithLeadingTrivia(originalLeadingTrivia)
            .WithTrailingTrivia(originalTrailingTrivia);

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
        {
            return document;
        }

        // Replace the ToList() call with just the source expression
        var newRoot = root.ReplaceNode(
            toListInvocation,
            replacementExpression.WithAdditionalAnnotations(Formatter.Annotation));

        // Apply fluent chain alignment if this is part of a method chain
        newRoot = FluentChainAligner.AlignFluentChains(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }
}
