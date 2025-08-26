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
using ToListinator.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StaticExpressionPropertyCodeFixProvider)), Shared]
public class StaticExpressionPropertyCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [StaticExpressionPropertyAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var property = await CodeFixHelper.FindTargetNode<PropertyDeclarationSyntax>(
            context,
            StaticExpressionPropertyAnalyzer.DiagnosticId);

        if (property?.ExpressionBody == null)
        {
            return;
        }

        var diagnostic = CodeFixHelper.GetDiagnostic(context, StaticExpressionPropertyAnalyzer.DiagnosticId);
        if (diagnostic == null)
        {
            return;
        }

        var action = CodeFixHelper.CreateSimpleAction(
            "Convert to getter-only property with initializer",
            "ConvertToGetterOnlyProperty",
            ConvertToGetterOnlyProperty,
            context,
            property);

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> ConvertToGetterOnlyProperty(
        Document document,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Get the expression from the expression body
        var expression = property.ExpressionBody!.Expression;

        // Check if the original expression starts on a new line (multi-line format)
        var originalArrowToken = property.ExpressionBody.ArrowToken;
        var hasNewLineAfterArrow = originalArrowToken.TrailingTrivia.Any(t =>
            t.IsKind(SyntaxKind.EndOfLineTrivia));

        // Determine the appropriate trailing trivia for the equals token
        var equalsTrailingTrivia = hasNewLineAfterArrow
            ? originalArrowToken.TrailingTrivia
            : TriviaList(Space);

        // Create the new property with getter-only accessor and initializer
        var newProperty = property
            .WithExpressionBody(null) // Remove expression body
            .WithSemicolonToken(default) // Remove semicolon
            .WithAccessorList(
                AccessorList(
                    SingletonList(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    )
                )
                .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(Space))
                .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken))
            )
            .WithInitializer(
                EqualsValueClause(expression)
                    .WithEqualsToken(Token(SyntaxKind.EqualsToken)
                        .WithLeadingTrivia(Space)
                        .WithTrailingTrivia(equalsTrailingTrivia))
            )
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        // Use TriviaHelper to preserve trivia from the original property
        var newPropertyWithTrivia = TriviaHelper.PreserveTrivia(newProperty, property);

        var newRoot = root.ReplaceNode(property, newPropertyWithTrivia);
        return document.WithSyntaxRoot(newRoot);
    }
}
