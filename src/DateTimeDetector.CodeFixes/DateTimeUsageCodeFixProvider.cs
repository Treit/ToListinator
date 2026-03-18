using DateTimeDetector.Analyzers;
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

namespace DateTimeDetector.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeUsageCodeFixProvider)), Shared]
public class DateTimeUsageCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [DateTimeUsageAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // The diagnostic span may cover just "DateTime" or a qualified "System.DateTime".
        // Find the DateTime identifier within the reported span.
        var node = root.FindNode(diagnosticSpan);
        var identifierNode = node.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .FirstOrDefault(n => n.Identifier.ValueText == "DateTime");

        if (identifierNode is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return;
        }

        var dateTimeOffsetType = semanticModel.Compilation.GetTypeByMetadataName("System.DateTimeOffset");
        if (dateTimeOffsetType is null)
        {
            return;
        }

        // Check if the rewrite would be safe
        if (!IsRewriteSafe(identifierNode, dateTimeOffsetType, semanticModel, context.CancellationToken))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace DateTime with DateTimeOffset",
                createChangedDocument: ct => ReplaceWithDateTimeOffset(context.Document, identifierNode, ct),
                equivalenceKey: "ReplaceDateTimeWithDateTimeOffset"),
            diagnostic);
    }

    private static bool IsRewriteSafe(
        IdentifierNameSyntax identifierNode,
        INamedTypeSymbol dateTimeOffsetType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Check for instance member chains that would break
        // e.g., DateTime.Now.Kind → DateTimeOffset.Now.Kind (Kind doesn't exist on DateTimeOffset)
        if (IsUnsafeChain(identifierNode, dateTimeOffsetType, semanticModel, cancellationToken))
        {
            return false;
        }

        // Check for constructor overloads that don't exist on DateTimeOffset
        // e.g., new DateTime(2024, 1, 2) → new DateTimeOffset(2024, 1, 2) (no matching ctor)
        if (IsUnsafeConstructor(identifierNode, dateTimeOffsetType, semanticModel, cancellationToken))
        {
            return false;
        }

        return true;
    }

    private static bool IsUnsafeChain(
        IdentifierNameSyntax dateTimeIdentifier,
        INamedTypeSymbol dateTimeOffsetType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Find the member access where DateTime is used as the type (e.g., DateTime.Now)
        // Then check if that member access is itself part of a longer chain
        MemberAccessExpressionSyntax? staticAccess = null;

        if (dateTimeIdentifier.Parent is MemberAccessExpressionSyntax directAccess
            && directAccess.Expression == dateTimeIdentifier)
        {
            // DateTime.Now — dateTimeIdentifier is the expression
            staticAccess = directAccess;
        }
        else if (dateTimeIdentifier.Parent is MemberAccessExpressionSyntax qualifiedAccess
            && qualifiedAccess.Name == dateTimeIdentifier
            && qualifiedAccess.Parent is MemberAccessExpressionSyntax outerAccess
            && outerAccess.Expression == qualifiedAccess)
        {
            // System.DateTime.Now — qualifiedAccess is System.DateTime, outerAccess is ...Now
            staticAccess = outerAccess;
        }

        if (staticAccess is null)
        {
            return false;
        }

        // Now check: does the static access resolve to a member that returns DateTime,
        // and is it followed by another member access?
        // e.g., DateTime.Now.Kind — staticAccess is DateTime.Now, we need to check .Kind
        SyntaxNode current = staticAccess.Parent is InvocationExpressionSyntax invocation
            ? invocation  // DateTime.Parse(...) — the invocation is the full expression
            : staticAccess; // DateTime.Now — the member access itself is the expression

        // Walk up: is this expression used as the left side of another member access?
        if (current.Parent is MemberAccessExpressionSyntax chainedAccess
            && chainedAccess.Expression == current)
        {
            var chainedMemberName = chainedAccess.Name.Identifier.ValueText;
            if (!dateTimeOffsetType.GetMembers(chainedMemberName).Any())
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUnsafeConstructor(
        IdentifierNameSyntax dateTimeIdentifier,
        INamedTypeSymbol dateTimeOffsetType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Check if this DateTime is inside a "new DateTime(...)" expression
        if (dateTimeIdentifier.Parent is not ObjectCreationExpressionSyntax objectCreation
            || objectCreation.Type != dateTimeIdentifier)
        {
            return false;
        }

        // Get argument types
        var argList = objectCreation.ArgumentList;
        if (argList is null || argList.Arguments.Count == 0)
        {
            // new DateTime() — parameterless ctor exists on DateTimeOffset
            return false;
        }

        var argTypes = argList.Arguments
            .Select(a => semanticModel.GetTypeInfo(a.Expression, cancellationToken).Type)
            .ToList();

        if (argTypes.Any(t => t is null))
        {
            return true; // Can't determine types — be conservative
        }

        // Check if DateTimeOffset has a constructor with compatible parameter types
        var compilation = semanticModel.Compilation;
        foreach (var ctor in dateTimeOffsetType.InstanceConstructors)
        {
            if (ctor.Parameters.Length != argTypes.Count)
            {
                continue;
            }

            var allMatch = true;
            for (int i = 0; i < ctor.Parameters.Length; i++)
            {
                if (!compilation.HasImplicitConversion(argTypes[i]!, ctor.Parameters[i].Type))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                return false; // Found a matching ctor — safe to rewrite
            }
        }

        return true; // No matching ctor — unsafe
    }

    private static async Task<Document> ReplaceWithDateTimeOffset(
        Document document,
        IdentifierNameSyntax identifierNode,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var leadingTrivia = identifierNode.GetLeadingTrivia();
        var trailingTrivia = identifierNode.GetTrailingTrivia();

        var newIdentifier = SyntaxFactory.IdentifierName("DateTimeOffset")
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(trailingTrivia);

        var newRoot = root.ReplaceNode(identifierNode, newIdentifier);

        return document.WithSyntaxRoot(newRoot);
    }
}
