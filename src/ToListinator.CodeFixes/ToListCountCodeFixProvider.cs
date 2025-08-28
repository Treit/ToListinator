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
using ToListinator.Analyzers.Utils;
using ToListinator.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToListCountCodeFixProvider)), Shared]
public class ToListCountCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [ToListCountAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var binaryExpression = await CodeFixHelper.FindTargetNode<BinaryExpressionSyntax>(
            context,
            ToListCountAnalyzer.DiagnosticId);

        if (binaryExpression is null)
        {
            return;
        }

        var diagnostic = CodeFixHelper.GetDiagnostic(context, ToListCountAnalyzer.DiagnosticId);
        if (diagnostic == null)
        {
            return;
        }

        var action = CodeFixHelper.CreateSimpleAction(
            "Replace with Any()",
            "ReplaceToListCountWithAny",
            ReplaceWithAny,
            context,
            binaryExpression);

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> ReplaceWithAny(Document document, BinaryExpressionSyntax binaryExpression, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);

        var (originalCollection, isNegated) = ExtractCollectionAndNegation(binaryExpression);

        if (originalCollection is null)
        {
            return document;
        }

        var anyCall = CreateAnyCall(originalCollection, isNegated);

        var newRoot = root?.ReplaceNode(binaryExpression, anyCall);

        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }

    private static (ExpressionSyntax? originalCollection, bool isNegated) ExtractCollectionAndNegation(BinaryExpressionSyntax binaryExpression)
    {
        // Check left side for ToList().Count or ToArray().Length
        if (IsToListCountOrToArrayLengthExpression(binaryExpression.Left, out var leftCollection))
        {
            var isNegated = BinaryExpressionHelper.IsNegatedCountPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Right, isLeftOperand: true);
            return (leftCollection, isNegated);
        }

        // Check right side for ToList().Count or ToArray().Length
        if (IsToListCountOrToArrayLengthExpression(binaryExpression.Right, out var rightCollection))
        {
            var isNegated = BinaryExpressionHelper.IsNegatedCountPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Left, isLeftOperand: false);
            return (rightCollection, isNegated);
        }

        return (null, false);
    }

    private static bool IsToListCountOrToArrayLengthExpression(SyntaxNode expression, out ExpressionSyntax? originalCollection)
    {
        originalCollection = null;

        // Check for ToList().Count pattern
        if (expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Count",
                Expression: InvocationExpressionSyntax toListCall
            } &&
            toListCall.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "ToList",
                Expression: var toListCollection
            })
        {
            originalCollection = toListCollection;
            return true;
        }

        // Check for ToArray().Length pattern
        if (expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Length",
                Expression: InvocationExpressionSyntax toArrayCall
            } &&
            toArrayCall.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "ToArray",
                Expression: var toArrayCollection
            })
        {
            originalCollection = toArrayCollection;
            return true;
        }

        return false;
    }

    private static bool IsToListCountExpression(SyntaxNode expression, out ExpressionSyntax? originalCollection)
    {
        originalCollection = null;

        if (expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Count",
                Expression: InvocationExpressionSyntax toListCall
            } &&
            toListCall.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "ToList",
                Expression: var collection
            })
        {
            originalCollection = collection;
            return true;
        }

        return false;
    }

    private static ExpressionSyntax CreateAnyCall(ExpressionSyntax originalCollection, bool isNegated)
    {
        // Create collection.Any()
        var anyCall = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                originalCollection,
                IdentifierName("Any")))
            .WithArgumentList(ArgumentList());

        // If negated, wrap in !collection.Any()
        if (isNegated)
        {
            return PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                anyCall);
        }

        return anyCall;
    }
}
