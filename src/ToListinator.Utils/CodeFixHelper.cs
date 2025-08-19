using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ToListinator.Utils;

/// <summary>
/// Provides common utility methods for code fix providers to reduce boilerplate and standardize patterns.
/// </summary>
public static class CodeFixHelper
{
    /// <summary>
    /// Finds the target syntax node for a code fix by locating the diagnostic and traversing to the desired node type.
    /// </summary>
    /// <typeparam name="T">The type of syntax node to find</typeparam>
    /// <param name="context">The code fix context containing the diagnostic information</param>
    /// <param name="diagnosticId">The diagnostic ID to search for</param>
    /// <returns>The target node of type T, or null if not found</returns>
    public static async Task<T?> FindTargetNode<T>(CodeFixContext context, string diagnosticId)
        where T : SyntaxNode
    {
        var diagnostic = context.Diagnostics.FirstOrDefault(diag => diag.Id == diagnosticId);
        if (diagnostic == null)
        {
            return null;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null)
        {
            return null;
        }

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        return root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<T>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Finds the target syntax node using the diagnostic span location directly.
    /// </summary>
    /// <typeparam name="T">The type of syntax node to find</typeparam>
    /// <param name="context">The code fix context containing the diagnostic information</param>
    /// <param name="diagnosticId">The diagnostic ID to search for</param>
    /// <returns>The target node of type T, or null if not found</returns>
    public static async Task<T?> FindTargetNodeBySpan<T>(CodeFixContext context, string diagnosticId)
        where T : SyntaxNode
    {
        var diagnostic = context.Diagnostics.FirstOrDefault(diag => diag.Id == diagnosticId);
        if (diagnostic == null)
        {
            return null;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null)
        {
            return null;
        }

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        return root.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<T>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Creates a simple code action with standardized naming and behavior.
    /// </summary>
    /// <param name="title">The title to display for the code action</param>
    /// <param name="equivalenceKey">The equivalence key for the code action</param>
    /// <param name="createChangedDocument">Function that creates the modified document</param>
    /// <param name="context">The code fix context</param>
    /// <param name="targetNode">The target syntax node to transform</param>
    /// <returns>A CodeAction that can be registered with the context</returns>
    public static CodeAction CreateSimpleAction<T>(
        string title,
        string equivalenceKey,
        Func<Document, T, CancellationToken, Task<Document>> createChangedDocument,
        CodeFixContext context,
        T targetNode) where T : SyntaxNode
    {
        return CodeAction.Create(
            title: title,
            createChangedDocument: c => createChangedDocument(context.Document, targetNode, c),
            equivalenceKey: equivalenceKey);
    }

    /// <summary>
    /// Replaces a syntax node while preserving trivia from the original node.
    /// </summary>
    /// <typeparam name="T">The type of syntax node</typeparam>
    /// <param name="document">The document to modify</param>
    /// <param name="oldNode">The node to replace</param>
    /// <param name="newNode">The replacement node</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The modified document</returns>
    public static async Task<Document> ReplaceNodeWithTrivia<T>(
        Document document,
        T oldNode,
        T newNode,
        CancellationToken cancellationToken) where T : SyntaxNode
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
        {
            return document;
        }

        // Preserve trivia from the original node
        var newNodeWithTrivia = TriviaHelper.PreserveTrivia(newNode, oldNode);
        var newRoot = root.ReplaceNode(oldNode, newNodeWithTrivia);

        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Gets the first diagnostic with the specified ID from the context.
    /// </summary>
    /// <param name="context">The code fix context</param>
    /// <param name="diagnosticId">The diagnostic ID to find</param>
    /// <returns>The diagnostic, or null if not found</returns>
    public static Diagnostic? GetDiagnostic(CodeFixContext context, string diagnosticId)
    {
        return context.Diagnostics.FirstOrDefault(diag => diag.Id == diagnosticId);
    }

    /// <summary>
    /// Gets the syntax root for the document in the context.
    /// </summary>
    /// <param name="context">The code fix context</param>
    /// <returns>The syntax root, or null if not available</returns>
    public static async Task<SyntaxNode?> GetSyntaxRoot(CodeFixContext context)
    {
        return await context.Document.GetSyntaxRootAsync(context.CancellationToken);
    }

    /// <summary>
    /// Standard pattern for registering a simple code fix that transforms one node to another.
    /// </summary>
    /// <typeparam name="T">The type of syntax node to transform</typeparam>
    /// <param name="context">The code fix context</param>
    /// <param name="diagnosticId">The diagnostic ID to fix</param>
    /// <param name="title">The title for the code action</param>
    /// <param name="equivalenceKey">The equivalence key for the code action</param>
    /// <param name="transform">Function that transforms the target node</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task RegisterSimpleCodeFix<T>(
        CodeFixContext context,
        string diagnosticId,
        string title,
        string equivalenceKey,
        Func<Document, T, CancellationToken, Task<Document>> transform) where T : SyntaxNode
    {
        var targetNode = await FindTargetNode<T>(context, diagnosticId);
        if (targetNode == null)
        {
            return;
        }

        var diagnostic = GetDiagnostic(context, diagnosticId);
        if (diagnostic == null)
        {
            return;
        }

        var action = CreateSimpleAction(title, equivalenceKey, transform, context, targetNode);
        context.RegisterCodeFix(action, diagnostic);
    }
}
