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
using ToListinator.Utils;

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
        var invocation = await CodeFixHelper.FindTargetNodeBySpan<InvocationExpressionSyntax>(
            context,
            IdentitySelectAnalyzer.DiagnosticId);

        if (invocation == null)
        {
            return;
        }

        var diagnostic = CodeFixHelper.GetDiagnostic(context, IdentitySelectAnalyzer.DiagnosticId);
        if (diagnostic == null)
        {
            return;
        }

        var action = CodeFixHelper.CreateSimpleAction(
            "Remove identity Select",
            "RemoveIdentitySelect",
            RemoveIdentitySelect,
            context,
            invocation);

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> RemoveIdentitySelect(
        Document document,
        InvocationExpressionSyntax selectInvocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (selectInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var sourceExpression = memberAccess.Expression;

            // Replace the entire Select invocation with just the source expression using utility
            return await CodeFixHelper.ReplaceNodeWithTrivia(
                document,
                selectInvocation,
                sourceExpression,
                cancellationToken);
        }

        return document;
    }
}
