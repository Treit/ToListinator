using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.Utils;

public static class FluentChainAligner
{
    /// <summary>
    /// Aligns fluent method chains to have consistent indentation.
    /// All chained method calls will be aligned to the same column as the first
    /// method call in the chain.
    /// </summary>
    /// <param name="root">The syntax node to process</param>
    /// <returns>A new syntax node with aligned fluent chains</returns>
    /// <example>
    /// <para>Transforms misaligned chains to consistent indentation:</para>
    /// <code>
    /// // Before:
    /// var result = items
    ///             .Where(x => x.IsActive)
    ///         .Select(x => x.Name)
    /// .ToList();
    ///
    /// // After:
    /// var result = items
    ///             .Where(x => x.IsActive)
    ///             .Select(x => x.Name)
    ///             .ToList();
    /// </code>
    /// </example>
    /// <example>
    /// <para>Works with complex nested chains:</para>
    /// <code>
    /// // Before:
    /// var query = dbContext.Users
    ///                      .Where(u => u.IsActive)
    ///                  .Select(u => new { u.Id, u.Name })
    ///              .OrderBy(x => x.Name);
    ///
    /// // After:
    /// var query = dbContext.Users
    ///                      .Where(u => u.IsActive)
    ///                      .Select(u => new { u.Id, u.Name })
    ///                      .OrderBy(x => x.Name);
    /// </code>
    /// </example>
    public static SyntaxNode AlignFluentChains(SyntaxNode root)
    {
        return new FluentChainAlignerRewriter().Visit(root);
    }

    private sealed class FluentChainAlignerRewriter : CSharpSyntaxRewriter
    {
        private string? _targetIndentation;
        private bool _isInChain;

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // Only process the outermost invocation in a chain to avoid double processing
            if (IsOutermostInvocation(node) && IsFluentChain(node))
            {
                var chain = BuildChainFromInvocation(node);
                var firstOnNewLine = chain.FirstOrDefault(IsOnNewLine);

                if (firstOnNewLine != null)
                {
                    _targetIndentation = GetIndentation(firstOnNewLine);
                    _isInChain = true;

                    var result = base.VisitInvocationExpression(node);

                    _targetIndentation = null;
                    _isInChain = false;

                    return result;
                }
            }

            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (_isInChain && _targetIndentation != null && IsOnNewLine(node))
            {
                var currentIndentation = GetIndentation(node);
                if (currentIndentation != _targetIndentation)
                {
                    var alignedNode = SetIndentation(node, _targetIndentation);
                    return base.VisitMemberAccessExpression(alignedNode);
                }
            }

            return base.VisitMemberAccessExpression(node);
        }

        private static bool IsOutermostInvocation(InvocationExpressionSyntax node)
        {
            // Check if this invocation is not part of another invocation's expression
            return !(node.Parent is MemberAccessExpressionSyntax);
        }

        private static bool IsFluentChain(InvocationExpressionSyntax node)
        {
            // A fluent chain has at least one member access that is on a new line
            var chain = BuildChainFromInvocation(node);
            return chain.Any(IsOnNewLine);
        }

        private static List<MemberAccessExpressionSyntax> BuildChainFromInvocation(InvocationExpressionSyntax invocation)
        {
            var chain = new List<MemberAccessExpressionSyntax>();
            var current = invocation.Expression;

            while (current is MemberAccessExpressionSyntax memberAccess)
            {
                chain.Add(memberAccess);

                if (memberAccess.Expression is InvocationExpressionSyntax nestedInvocation)
                {
                    current = nestedInvocation.Expression;
                }
                else
                {
                    break;
                }
            }

            // Reverse to get them in chain order (first to last)
            chain.Reverse();
            return chain;
        }

        private static bool IsOnNewLine(MemberAccessExpressionSyntax memberAccess)
        {
            var dotToken = memberAccess.OperatorToken;
            var previousToken = dotToken.GetPreviousToken();

            // Check if there's a newline in the trailing trivia of the previous token
            // or in the leading trivia of the dot token
            return previousToken.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) ||
                   dotToken.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        }

        private static string GetIndentation(MemberAccessExpressionSyntax memberAccess)
        {
            var dotToken = memberAccess.OperatorToken;
            var previousToken = dotToken.GetPreviousToken();

            // First check the leading trivia of the dot token
            string indentation = GetIndentationFromTrivia(dotToken.LeadingTrivia);
            if (!string.IsNullOrEmpty(indentation))
            {
                return indentation;
            }

            // If no indentation in dot token, check trailing trivia of previous token
            // This handles cases where newline is in previous token's trailing trivia
            indentation = GetIndentationAfterNewlineInTrailingTrivia(previousToken.TrailingTrivia, dotToken.LeadingTrivia);
            return indentation;
        }

        private static string GetIndentationFromTrivia(SyntaxTriviaList triviaList)
        {
            string indentation = "";
            for (int i = 0; i < triviaList.Count; i++)
            {
                var trivia = triviaList[i];
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    indentation = "";
                }
                else if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    indentation = trivia.ToString();
                }
            }
            return indentation;
        }

        private static string GetIndentationAfterNewlineInTrailingTrivia(SyntaxTriviaList trailingTrivia, SyntaxTriviaList leadingTrivia)
        {
            // Check if there's a newline in trailing trivia
            bool foundNewline = trailingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            if (!foundNewline)
            {
                return "";
            }

            // If there's a newline in trailing trivia, the indentation should be in the leading trivia
            return GetIndentationFromTrivia(leadingTrivia);
        }

        private static MemberAccessExpressionSyntax SetIndentation(MemberAccessExpressionSyntax memberAccess, string targetIndentation)
        {
            var dotToken = memberAccess.OperatorToken;
            var previousToken = dotToken.GetPreviousToken();

            // Check if newline is in previous token's trailing trivia
            if (previousToken.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                // Replace the leading trivia of the dot token with target indentation
                var newLeadingTrivia = TriviaList(Whitespace(targetIndentation));
                var newDotToken = dotToken.WithLeadingTrivia(newLeadingTrivia);
                return memberAccess.WithOperatorToken(newDotToken);
            }
            else
            {
                // Newline is in dot token's leading trivia, replace the whitespace after the newline
                var leadingTrivia = dotToken.LeadingTrivia.ToList();

                // Find the last newline and replace all following whitespace
                int lastNewlineIndex = -1;
                for (int i = leadingTrivia.Count - 1; i >= 0; i--)
                {
                    if (leadingTrivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        lastNewlineIndex = i;
                        break;
                    }
                }

                if (lastNewlineIndex >= 0)
                {
                    // Remove all trivia after the newline and add target indentation
                    var newTrivia = leadingTrivia.Take(lastNewlineIndex + 1).ToList();
                    newTrivia.Add(Whitespace(targetIndentation));

                    var newDotToken = dotToken.WithLeadingTrivia(TriviaList(newTrivia));
                    return memberAccess.WithOperatorToken(newDotToken);
                }
            }

            return memberAccess;
        }
    }
}
