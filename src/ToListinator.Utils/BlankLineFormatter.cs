using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace ToListinator.Utils;

public static class BlankLineFormatter
{
    /// <summary>
    /// Ensures that all if statements are preceded by at least one blank line (two consecutive EndOfLineTrivia).
    /// </summary>
    public static SyntaxNode EnsureBlankLineBeforeIfStatements(SyntaxNode root)
    {
        return new IfStatementBlankLineRewriter().Visit(root);
    }

    private sealed class IfStatementBlankLineRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            if (HasBlankLineBefore(node))
            {
                return base.VisitIfStatement(node);
            }

            var updated = node.WithLeadingTrivia(
                node.GetLeadingTrivia().Prepend(SyntaxFactory.EndOfLine(Environment.NewLine))
            );

            return base.VisitIfStatement(updated);
        }

        private static bool HasBlankLineBefore(IfStatementSyntax node)
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
    }
}
