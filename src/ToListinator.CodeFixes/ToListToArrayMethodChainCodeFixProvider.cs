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
using ToListinator.Utils;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToListToArrayMethodChainCodeFixProvider)), Shared]
public class ToListToArrayMethodChainCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [ToListinator.Analyzers.ToListToArrayMethodChainAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the invocation expression
            var token = root?.FindToken(diagnosticSpan.Start);
            var invocationExpression = token?.Parent?
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocationExpression != null)
            {
                var methodName = GetMethodName(invocationExpression);
                var action = CodeAction.Create(
                    title: $"Remove unnecessary {methodName}() call",
                    createChangedDocument: c => RemoveUnnecessaryCall(context.Document, invocationExpression, c),
                    equivalenceKey: $"Remove{methodName}");

                context.RegisterCodeFix(action, diagnostic);
            }
        }
    }

    private static string GetMethodName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.ValueText;
        }
        return "ToList";
    }

    private static async Task<Document> RemoveUnnecessaryCall(
        Document document,
        InvocationExpressionSyntax invocationExpression,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
            return document;

        // Get the member access expression (e.g., "collection.ToList")
        if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        // The underlying expression is the part before .ToList() or .ToArray()
        var underlyingExpression = memberAccess.Expression;

        // For trivia handling, we want to preserve the structure but remove the ToList/ToArray line
        // We keep the leading trivia from the invocation but don't move trailing trivia
        // (which might include comments that belong to the ToList/ToArray line)
        var leadingTrivia = invocationExpression.GetLeadingTrivia();

        // Create the replacement expression with preserved leading trivia
        var newExpression = underlyingExpression
            .WithLeadingTrivia(leadingTrivia)
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Replace the entire invocation with just the underlying expression
        var newRoot = root.ReplaceNode(invocationExpression, newExpression);

        // Apply fluent chain alignment to ensure proper formatting
        newRoot = FluentChainAligner.AlignFluentChains(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }
}
