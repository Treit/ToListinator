using Microsoft.CodeAnalysis;
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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingleElementAccessCodeFixProvider)), Shared]
public class SingleElementAccessCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [SingleElementAccessAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = CodeFixHelper.GetDiagnostic(context, SingleElementAccessAnalyzer.DiagnosticId);
        if (diagnostic is null)
        {
            return;
        }

        var accessKind = diagnostic.Properties.GetValueOrDefault(SingleElementAccessAnalyzer.AccessKindProperty);

        if (accessKind == SingleElementAccessAnalyzer.AccessKindMethod)
        {
            var invocation = await CodeFixHelper.FindTargetNodeBySpan<InvocationExpressionSyntax>(
                context, SingleElementAccessAnalyzer.DiagnosticId);

            if (invocation is null)
            {
                return;
            }

            var action = CodeFixHelper.CreateSimpleAction(
                "Call LINQ method directly without materializing",
                "RemoveMaterializationBeforeElementAccess",
                RemoveMaterializationFromMethodCall,
                context,
                invocation);

            context.RegisterCodeFix(action, diagnostic);
        }
        else if (accessKind == SingleElementAccessAnalyzer.AccessKindIndexer)
        {
            var elementAccess = await CodeFixHelper.FindTargetNodeBySpan<ElementAccessExpressionSyntax>(
                context, SingleElementAccessAnalyzer.DiagnosticId);

            if (elementAccess is null)
            {
                return;
            }

            var action = CodeFixHelper.CreateSimpleAction(
                "Replace with ElementAt()",
                "ReplaceIndexerWithElementAt",
                ReplaceIndexerWithElementAt,
                context,
                elementAccess);

            context.RegisterCodeFix(action, diagnostic);
        }
    }

    private static async Task<Document> RemoveMaterializationFromMethodCall(
        Document document,
        InvocationExpressionSyntax outerInvocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Pattern: source.ToList().First(...) or source.ToArray().Last(...)
        if (outerInvocation.Expression is not MemberAccessExpressionSyntax
            {
                Name: var methodName,
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name.Identifier.ValueText: "ToList" or "ToArray",
                        Expression: var sourceExpression
                    }
                }
            })
        {
            return document;
        }

        var originalLeadingTrivia = outerInvocation.GetLeadingTrivia();
        var originalTrailingTrivia = outerInvocation.GetTrailingTrivia();

        var newMemberAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            sourceExpression.WithoutTrivia(),
            methodName.WithoutTrivia());

        var newInvocation = InvocationExpression(newMemberAccess, outerInvocation.ArgumentList.WithoutTrivia())
            .WithLeadingTrivia(originalLeadingTrivia)
            .WithTrailingTrivia(originalTrailingTrivia);

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root is null)
        {
            return document;
        }

        var newRoot = root.ReplaceNode(
            outerInvocation,
            newInvocation.WithAdditionalAnnotations(Formatter.Annotation));

        newRoot = FluentChainAligner.AlignFluentChains(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> ReplaceIndexerWithElementAt(
        Document document,
        ElementAccessExpressionSyntax elementAccess,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Pattern: source.ToList()[index] or source.ToArray()[index]
        if (elementAccess.Expression is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "ToList" or "ToArray",
                    Expression: var sourceExpression
                }
            }
            || elementAccess.ArgumentList.Arguments is not [var indexArgument])
        {
            return document;
        }

        var originalLeadingTrivia = elementAccess.GetLeadingTrivia();
        var originalTrailingTrivia = elementAccess.GetTrailingTrivia();

        var elementAtAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            sourceExpression.WithoutTrivia(),
            IdentifierName("ElementAt"));

        var argumentList = ArgumentList(
            SingletonSeparatedList(
                Argument(indexArgument.Expression.WithoutTrivia())));

        var newInvocation = InvocationExpression(elementAtAccess, argumentList)
            .WithLeadingTrivia(originalLeadingTrivia)
            .WithTrailingTrivia(originalTrailingTrivia);

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root is null)
        {
            return document;
        }

        var newRoot = root.ReplaceNode(
            elementAccess,
            newInvocation.WithAdditionalAnnotations(Formatter.Annotation));

        newRoot = FluentChainAligner.AlignFluentChains(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }
}
