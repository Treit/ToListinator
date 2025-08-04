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

namespace ToListinator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArrayEmptyCodeFixProvider)), Shared]
public class ArrayEmptyCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [ArrayEmptyAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics
            .First(diag => diag.Id == ArrayEmptyAnalyzer.DiagnosticId);

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

        var arrayCreation = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .FirstOrDefault(node => node is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax);

        if (arrayCreation is null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: "Replace with Array.Empty<T>()",
            createChangedDocument: c => ReplaceWithArrayEmpty(context.Document, arrayCreation, c),
            equivalenceKey: "ReplaceWithArrayEmpty");

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> ReplaceWithArrayEmpty(
        Document document,
        SyntaxNode arrayCreation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root is null)
        {
            return document;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel is null)
        {
            return document;
        }

        // Extract trivia from original node
        var originalLeadingTrivia = arrayCreation.GetLeadingTrivia();
        var originalTrailingTrivia = arrayCreation.GetTrailingTrivia();

        // Get the element type
        var elementType = GetElementType(arrayCreation, semanticModel);
        if (elementType is null)
        {
            return document;
        }

        // Create Array.Empty<T>() expression
        var arrayEmptyInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Array"),
                SyntaxFactory.GenericName("Empty")
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.ParseTypeName(elementType))))),
            SyntaxFactory.ArgumentList())
            .WithLeadingTrivia(originalLeadingTrivia)
            .WithTrailingTrivia(originalTrailingTrivia);

        var newRoot = root.ReplaceNode(
            arrayCreation,
            arrayEmptyInvocation.WithAdditionalAnnotations(Formatter.Annotation));

        return document.WithSyntaxRoot(newRoot);
    }

    private static string? GetElementType(SyntaxNode arrayCreation, SemanticModel semanticModel)
    {
        return arrayCreation switch
        {
            ArrayCreationExpressionSyntax explicitArray => GetElementTypeFromExplicit(explicitArray, semanticModel),
            ImplicitArrayCreationExpressionSyntax implicitArray => GetElementTypeFromImplicit(implicitArray, semanticModel),
            _ => null
        };
    }

    private static string? GetElementTypeFromExplicit(ArrayCreationExpressionSyntax arrayCreation, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(arrayCreation.Type.ElementType);
        return typeInfo.Type?.ToDisplayString();
    }

    private static string? GetElementTypeFromImplicit(ImplicitArrayCreationExpressionSyntax arrayCreation, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(arrayCreation);
        if (typeInfo.Type is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType.ToDisplayString();
        }
        return null;
    }
}