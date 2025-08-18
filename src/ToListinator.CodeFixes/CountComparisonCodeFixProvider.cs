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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CountComparisonCodeFixProvider)), Shared]
public class CountComparisonCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [CountComparisonAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First(diag => diag.Id == CountComparisonAnalyzer.DiagnosticId);
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var binaryExpression = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<BinaryExpressionSyntax>()
            .FirstOrDefault();

        if (binaryExpression is null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: "Replace with Any()",
            createChangedDocument: c => ReplaceWithAny(context.Document, binaryExpression, c),
            equivalenceKey: "ReplaceCountComparisonWithAny");

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

        // Extract trivia from the original binary expression
        var originalLeadingTrivia = binaryExpression.GetLeadingTrivia();
        var originalTrailingTrivia = binaryExpression.GetTrailingTrivia();

        var anyCall = CreateAnyCall(originalCollection.WithoutTrivia(), isNegated)
            .WithLeadingTrivia(originalLeadingTrivia)
            .WithTrailingTrivia(originalTrailingTrivia);

        var newRoot = root?.ReplaceNode(binaryExpression, anyCall.WithAdditionalAnnotations(Formatter.Annotation));

        if (newRoot is null)
        {
            return document;
        }

        // Use FluentChainAligner to properly align any fluent chains
        newRoot = FluentChainAligner.AlignFluentChains(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }

    private static (ExpressionSyntax? originalCollection, bool isNegated) ExtractCollectionAndNegation(BinaryExpressionSyntax binaryExpression)
    {
        // Check left side for Count()
        if (IsCountInvocation(binaryExpression.Left, out var leftCollection))
        {
            var isNegated = IsNegatedPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Right, isLeftOperand: true);
            return (leftCollection, isNegated);
        }

        // Check right side for Count()
        if (IsCountInvocation(binaryExpression.Right, out var rightCollection))
        {
            var isNegated = IsNegatedPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Left, isLeftOperand: false);
            return (rightCollection, isNegated);
        }

        return (null, false);
    }

    private static bool IsCountInvocation(SyntaxNode expression, out ExpressionSyntax? originalCollection)
    {
        originalCollection = null;

        if (expression is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccess,
                ArgumentList.Arguments.Count: 0 // Only parameterless Count()
            } &&
            memberAccess.Name.Identifier.ValueText == "Count")
        {
            originalCollection = memberAccess.Expression;
            return true;
        }

        return false;
    }

    private static bool IsNegatedPattern(SyntaxKind operatorKind, SyntaxNode constantNode, bool isLeftOperand)
    {
        if (constantNode is not LiteralExpressionSyntax literal)
            return false;

        var value = literal.Token.ValueText;

        // For patterns that check for existence (Any() = true):
        // collection.Count() > 0
        // collection.Count() >= 1
        // collection.Count() != 0
        // 0 < collection.Count()
        // 1 <= collection.Count()
        // 0 != collection.Count()

        // For patterns that check for non-existence (!Any() = true):
        // collection.Count() == 0
        // collection.Count() <= 0
        // collection.Count() < 1
        // 0 == collection.Count()
        // 0 >= collection.Count()
        // 1 > collection.Count()

        if (isLeftOperand) // collection.Count() <op> constant
        {
            return operatorKind switch
            {
                SyntaxKind.EqualsEqualsToken when value == "0" => true,
                SyntaxKind.LessThanEqualsToken when value == "0" => true,
                SyntaxKind.LessThanToken when value == "1" => true,
                _ => false
            };
        }
        else // constant <op> collection.Count()
        {
            return operatorKind switch
            {
                SyntaxKind.EqualsEqualsToken when value == "0" => true,
                SyntaxKind.GreaterThanEqualsToken when value == "0" => true,
                SyntaxKind.GreaterThanToken when value == "1" => true,
                _ => false
            };
        }
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
