using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;
using ToListinator.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

        // Collect all Where() invocations in the chain
        var whereChain = CollectWhereChain(countMemberAccess.Expression);
        if (whereChain.Count == 0)
        {
            return document;
        }

        // Find the source expression (before the first Where)
        var firstWhere = whereChain[0];
        if (firstWhere.Expression is not MemberAccessExpressionSyntax firstWhereMemberAccess)
        {
            return document;
        }

        var sourceExpression = firstWhereMemberAccess.Expression;

        // Combine all Where predicates with && operators
        var combinedPredicate = CombineWherePredicates(whereChain);
        if (combinedPredicate == null)
        {
            return document;
        }

        // Create the new Count(predicate) invocation
        var newCountMemberAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            sourceExpression,
            IdentifierName("Count"));

        var newCountInvocation = InvocationExpression(
            newCountMemberAccess,
            ArgumentList(
                SingletonSeparatedList(
                    Argument(combinedPredicate))));

        // Preserve the leading trivia from the original count invocation
        // and the trailing trivia as well
        var newInvocationWithTrivia = newCountInvocation
            .WithLeadingTrivia(countInvocation.GetLeadingTrivia())
            .WithTrailingTrivia(countInvocation.GetTrailingTrivia());

        // Replace the entire Where().Count() chain with Count(predicate)
        var newRoot = root.ReplaceNode(countInvocation, newInvocationWithTrivia);

        // Ensure proper alignment formatting
        newRoot = FluentChainAligner.AlignFluentChains(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }

    private static List<InvocationExpressionSyntax> CollectWhereChain(ExpressionSyntax expression)
    {
        var whereChain = new List<InvocationExpressionSyntax>();
        var current = expression;

        // Walk back through the chain collecting Where() calls
        while (current is InvocationExpressionSyntax invocation &&
               invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Name.Identifier.ValueText == "Where")
        {
            whereChain.Add(invocation);
            current = memberAccess.Expression;
        }

        // Reverse to get them in forward order (first to last)
        whereChain.Reverse();
        return whereChain;
    }

    private static ExpressionSyntax? CombineWherePredicates(List<InvocationExpressionSyntax> whereChain)
    {
        if (whereChain.Count == 0)
        {
            return null;
        }

        if (whereChain.Count == 1)
        {
            // Single Where clause - just extract the predicate
            return whereChain[0].ArgumentList.Arguments[0].Expression;
        }

        // Multiple Where clauses - combine with && operators
        var predicates = new List<ExpressionSyntax>();
        foreach (var whereInvocation in whereChain)
        {
            var predicate = whereInvocation.ArgumentList.Arguments[0].Expression;
            if (predicate != null)
            {
                var extracted = ExtractPredicateExpression(predicate);
                if (extracted != null)
                {
                    predicates.Add(extracted);
                }
            }
        }

        if (predicates.Count == 0)
        {
            return null;
        }

        // Get the parameter name from the first lambda
        var parameterName = GetParameterNameFromPredicate(whereChain[0].ArgumentList.Arguments[0].Expression);
        if (string.IsNullOrEmpty(parameterName))
        {
            return null;
        }

        // Combine all predicate expressions with &&
        var combinedExpression = predicates[0];
        for (int i = 1; i < predicates.Count; i++)
        {
            combinedExpression = BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                combinedExpression,
                predicates[i]);
        }

        // Create a new lambda expression with the combined predicate
        return SimpleLambdaExpression(
            Parameter(Identifier(parameterName!)),
            combinedExpression);
    }

    private static ExpressionSyntax? ExtractPredicateExpression(ExpressionSyntax predicate)
    {
        return predicate switch
        {
            SimpleLambdaExpressionSyntax simpleLambda when simpleLambda.Body is ExpressionSyntax expr => expr,
            ParenthesizedLambdaExpressionSyntax parenLambda when parenLambda.Body is ExpressionSyntax expr => expr,
            _ => predicate // For method groups and other cases, use as-is
        };
    }

    private static string? GetParameterNameFromPredicate(ExpressionSyntax? predicate)
    {
        return predicate switch
        {
            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.Identifier.ValueText,
            ParenthesizedLambdaExpressionSyntax parenLambda when parenLambda.ParameterList.Parameters.Count == 1
                => parenLambda.ParameterList.Parameters[0].Identifier.ValueText,
            _ => null
        };
    }
}
