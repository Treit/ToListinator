using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.Utils;

/// <summary>
/// Provides utilities for working with syntax trivia to preserve formatting and comments during code transformations.
/// </summary>
public static class TriviaHelper
{
    /// <summary>
    /// Extracts only comment trivia (single-line and multi-line comments) from a trivia list.
    /// </summary>
    /// <param name="trivia">The trivia list to filter</param>
    /// <returns>A trivia list containing only comments</returns>
    public static SyntaxTriviaList ExtractCommentTrivia(SyntaxTriviaList trivia)
    {
        var commentTrivia = trivia.Where(t =>
            t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            t.IsKind(SyntaxKind.MultiLineCommentTrivia));

        return TriviaList(commentTrivia);
    }

    /// <summary>
    /// Preserves the leading and trailing trivia from the original node on the new node.
    /// </summary>
    /// <typeparam name="T">The type of syntax node</typeparam>
    /// <param name="newNode">The new node to apply trivia to</param>
    /// <param name="originalNode">The original node to copy trivia from</param>
    /// <returns>The new node with preserved trivia</returns>
    public static T PreserveTrivia<T>(T newNode, T originalNode) where T : SyntaxNode
    {
        return newNode
            .WithLeadingTrivia(originalNode.GetLeadingTrivia())
            .WithTrailingTrivia(originalNode.GetTrailingTrivia());
    }

    /// <summary>
    /// Preserves trivia from the original node but allows overriding specific trivia.
    /// </summary>
    /// <typeparam name="T">The type of syntax node</typeparam>
    /// <param name="newNode">The new node to apply trivia to</param>
    /// <param name="originalNode">The original node to copy trivia from</param>
    /// <param name="leadingTrivia">Optional custom leading trivia (null to use original)</param>
    /// <param name="trailingTrivia">Optional custom trailing trivia (null to use original)</param>
    /// <returns>The new node with preserved or custom trivia</returns>
    public static T PreserveTriviaWithOverrides<T>(
        T newNode,
        T originalNode,
        SyntaxTriviaList? leadingTrivia = null,
        SyntaxTriviaList? trailingTrivia = null) where T : SyntaxNode
    {
        return newNode
            .WithLeadingTrivia(leadingTrivia ?? originalNode.GetLeadingTrivia())
            .WithTrailingTrivia(trailingTrivia ?? originalNode.GetTrailingTrivia());
    }

    /// <summary>
    /// Adds trailing comments to a syntax token while preserving existing trivia.
    /// </summary>
    /// <param name="token">The token to modify</param>
    /// <param name="additionalComments">The comments to add</param>
    /// <returns>The token with additional trailing comments</returns>
    public static SyntaxToken PreserveTrailingComments(
        SyntaxToken token,
        IEnumerable<SyntaxTrivia> additionalComments)
    {
        var existingTrivia = token.TrailingTrivia;
        var newTrivia = existingTrivia.AddRange(additionalComments);
        return token.WithTrailingTrivia(newTrivia);
    }

    /// <summary>
    /// Adds trailing comments to a syntax token with a space separator.
    /// </summary>
    /// <param name="token">The token to modify</param>
    /// <param name="additionalComments">The comments to add</param>
    /// <returns>The token with additional trailing comments separated by a space</returns>
    public static SyntaxToken AddTrailingCommentsWithSpace(
        SyntaxToken token,
        IEnumerable<SyntaxTrivia> additionalComments)
    {
        var existingTrivia = token.TrailingTrivia;
        var spacedComments = TriviaList(Space).AddRange(additionalComments);
        var newTrivia = existingTrivia.AddRange(spacedComments);
        return token.WithTrailingTrivia(newTrivia);
    }

    /// <summary>
    /// Checks if a syntax node has at least one blank line before it (two consecutive end-of-line trivia).
    /// </summary>
    /// <param name="node">The node to check</param>
    /// <returns>True if there's a blank line before the node</returns>
    public static bool HasBlankLineBefore(SyntaxNode node)
    {
        int eolCount = 0;
        foreach (var trivia in node.GetLeadingTrivia().Reverse())
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                if (++eolCount >= 2)
                {
                    return true;
                }
            }
            else if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                eolCount = 0;
            }
        }

        return false;
    }

    /// <summary>
    /// Ensures that a syntax node has at least one blank line before it.
    /// </summary>
    /// <typeparam name="T">The type of syntax node</typeparam>
    /// <param name="node">The node to modify</param>
    /// <returns>The node with a blank line before it</returns>
    public static T EnsureBlankLineBefore<T>(T node) where T : SyntaxNode
    {
        if (HasBlankLineBefore(node))
        {
            return node;
        }

        return node.WithLeadingTrivia(
            node.GetLeadingTrivia().Prepend(EndOfLine(Environment.NewLine))
        );
    }

    /// <summary>
    /// Removes leading and trailing trivia from tokens within an expression while preserving comment trivia.
    /// This is useful for cleaning up expressions before inserting them into new contexts.
    /// </summary>
    /// <typeparam name="T">The type of syntax node</typeparam>
    /// <param name="expression">The expression to clean</param>
    /// <returns>Tuple containing the cleaned expression and any preserved comment trivia</returns>
    public static (T cleanedExpression, SyntaxTriviaList preservedComments) CleanExpressionTrivia<T>(T expression)
        where T : SyntaxNode
    {
        var tokens = expression.DescendantTokens().ToArray();
        var preservedComments = new List<SyntaxTrivia>();

        if (tokens.Length == 0)
        {
            return (expression, TriviaList());
        }

        var updatedTokens = new SyntaxToken[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (i == 0)
            {
                // First token: remove leading trivia
                updatedTokens[i] = token.WithLeadingTrivia();
            }
            else if (i == tokens.Length - 1)
            {
                // Last token: preserve comment trivia before removing trailing trivia
                var commentTrivia = ExtractCommentTrivia(token.TrailingTrivia);
                preservedComments.AddRange(commentTrivia);
                updatedTokens[i] = token.WithTrailingTrivia();
            }
            else
            {
                // Middle tokens: keep as is
                updatedTokens[i] = token;
            }
        }

        // Replace all tokens
        var tokenIndex = 0;
        var cleanedExpression = expression.ReplaceTokens(tokens, (oldToken, newToken) => updatedTokens[tokenIndex++]);

        return (cleanedExpression, TriviaList(preservedComments));
    }

    /// <summary>
    /// Transfers trivia from one syntax node to another, useful when replacing nodes.
    /// </summary>
    /// <typeparam name="TFrom">The type of the source node</typeparam>
    /// <typeparam name="TTo">The type of the destination node</typeparam>
    /// <param name="from">The node to copy trivia from</param>
    /// <param name="to">The node to copy trivia to</param>
    /// <returns>The destination node with trivia from the source node</returns>
    public static TTo TransferTrivia<TFrom, TTo>(TFrom from, TTo to)
        where TFrom : SyntaxNode
        where TTo : SyntaxNode
    {
        return to
            .WithLeadingTrivia(from.GetLeadingTrivia())
            .WithTrailingTrivia(from.GetTrailingTrivia());
    }

    /// <summary>
    /// Combines trivia lists, filtering out duplicates and maintaining proper spacing.
    /// </summary>
    /// <param name="existingTrivia">The existing trivia list</param>
    /// <param name="additionalTrivia">The trivia to add</param>
    /// <param name="addSpaceSeparator">Whether to add a space between the trivia lists</param>
    /// <returns>A combined trivia list</returns>
    public static SyntaxTriviaList CombineTrivia(
        SyntaxTriviaList existingTrivia,
        SyntaxTriviaList additionalTrivia,
        bool addSpaceSeparator = false)
    {
        if (existingTrivia.Count == 0)
        {
            return additionalTrivia;
        }

        if (additionalTrivia.Count == 0)
        {
            return existingTrivia;
        }

        var combined = existingTrivia;
        if (addSpaceSeparator)
        {
            combined = combined.Add(Space);
        }

        return combined.AddRange(additionalTrivia);
    }
}
