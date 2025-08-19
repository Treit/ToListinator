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
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == StaticExpressionPropertyAnalyzer.DiagnosticId);
        if (diagnostic == null)
        {
            return;
        }

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var property = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault();

        if (property?.ExpressionBody == null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: "Convert to getter-only property with initializer",
            createChangedDocument: c => ConvertToGetterOnlyProperty(context.Document, property, c),
            equivalenceKey: "ConvertToGetterOnlyProperty");

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
                    .WithEqualsToken(Token(SyntaxKind.EqualsToken).WithLeadingTrivia(Space).WithTrailingTrivia(Space))
            )
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        // Preserve trivia from the original property
        newProperty = newProperty
            .WithLeadingTrivia(property.GetLeadingTrivia())
            .WithTrailingTrivia(property.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(property, newProperty);
        return document.WithSyntaxRoot(newRoot);
    }
}
