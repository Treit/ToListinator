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

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WhereCountCodeFixProvider)), Shared]
public class WhereCountCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [WhereCountAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics
            .First(diag => diag.Id == WhereCountAnalyzer.DiagnosticId);

        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

        var countInvocation = root?.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (countInvocation is null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: "Replace with Count(predicate)",
            createChangedDocument: c => ReplaceWithCountPredicate(context.Document, countInvocation, c),
            equivalenceKey: "ReplaceWithCountPredicate");

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> ReplaceWithCountPredicate(
        Document document,
        InvocationExpressionSyntax countInvocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = (await document.GetSyntaxRootAsync(cancellationToken))!;

        // Get the Count() member access
        if (countInvocation.Expression is not MemberAccessExpressionSyntax countMemberAccess)
        {
            return document;
        }

        // Get the Where() invocation
        if (countMemberAccess.Expression is not InvocationExpressionSyntax whereInvocation)
        {
            return document;
        }

        // Get the Where() member access
        if (whereInvocation.Expression is not MemberAccessExpressionSyntax whereMemberAccess)
        {
            return document;
        }

        // Get the source expression (the collection before Where)
        var sourceExpression = whereMemberAccess.Expression;

        // Get the predicate from Where()
        var wherePredicate = whereInvocation.ArgumentList.Arguments[0];

        // Create the new Count(predicate) invocation
        var newCountMemberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            sourceExpression,
            SyntaxFactory.IdentifierName("Count"));

        var newCountInvocation = SyntaxFactory.InvocationExpression(
            newCountMemberAccess,
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(wherePredicate)));

        // Preserve trivia from the original Count() invocation
        var newInvocationWithTrivia = newCountInvocation.WithTriviaFrom(countInvocation);

        // Replace the entire Where().Count() chain with Count(predicate)
        var newRoot = root.ReplaceNode(countInvocation, newInvocationWithTrivia);

        return document.WithSyntaxRoot(newRoot);
    }
}
