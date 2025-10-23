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
using ToListinator.Analyzers.Utils;
using ToListinator.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToArrayLengthCodeFixProvider)), Shared]
public class ToArrayLengthCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [ToArrayLengthAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var binaryExpression = await CodeFixHelper.FindTargetNode<BinaryExpressionSyntax>(
            context,
            ToArrayLengthAnalyzer.DiagnosticId);

        if (binaryExpression is null)
        {
            return;
        }

        var diagnostic = CodeFixHelper.GetDiagnostic(context, ToArrayLengthAnalyzer.DiagnosticId);
        if (diagnostic is null)
        {
            return;
        }

        var action = CodeFixHelper.CreateSimpleAction(
            "Replace with Any()",
            "ReplaceToArrayLengthWithAny",
            ReplaceWithAny,
            context,
            binaryExpression);

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> ReplaceWithAny(Document document, BinaryExpressionSyntax binaryExpression, CancellationToken cancellationToken)
    {
        var (originalCollection, isNegated) = ExtractCollectionAndNegation(binaryExpression);
        if (originalCollection is null)
        {
            return document;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root is null)
        {
            return document;
        }

        var replacementExpression = CreateAnyExpression(originalCollection, isNegated);
        var replacementWithTrivia = TriviaHelper.PreserveTrivia(replacementExpression, binaryExpression)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(binaryExpression, replacementWithTrivia);
        newRoot = FluentChainAligner.AlignFluentChains(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }

    private static (ExpressionSyntax? originalCollection, bool isNegated) ExtractCollectionAndNegation(BinaryExpressionSyntax binaryExpression)
    {
        if (TryGetCollection(binaryExpression.Left, out var leftCollection))
        {
            var isNegated = BinaryExpressionHelper.IsNegatedCountPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Right, isLeftOperand: true);
            return (leftCollection, isNegated);
        }

        if (TryGetCollection(binaryExpression.Right, out var rightCollection))
        {
            var isNegated = BinaryExpressionHelper.IsNegatedCountPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Left, isLeftOperand: false);
            return (rightCollection, isNegated);
        }

        return (null, false);
    }

    private static bool TryGetCollection(SyntaxNode expression, out ExpressionSyntax? originalCollection)
    {
        if (expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Length",
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name.Identifier.ValueText: "ToArray",
                        Expression: ExpressionSyntax collection
                    }
                }
            })
        {
            originalCollection = collection;
            return true;
        }

        originalCollection = null;
        return false;
    }

    private static ExpressionSyntax CreateAnyExpression(ExpressionSyntax originalCollection, bool isNegated)
    {
        var (cleanedCollection, preservedComments) = TriviaHelper.CleanExpressionTrivia(originalCollection);

        var anyInvocation = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                cleanedCollection,
                IdentifierName("Any")),
            ArgumentList());

        if (preservedComments.Count > 0)
        {
            anyInvocation = anyInvocation.WithTrailingTrivia(
                TriviaHelper.CombineTrivia(anyInvocation.GetTrailingTrivia(), preservedComments, addSpaceSeparator: true));
        }

        var resultExpression = isNegated
            ? (ExpressionSyntax)PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, anyInvocation)
            : anyInvocation;

        return resultExpression;
    }
}
