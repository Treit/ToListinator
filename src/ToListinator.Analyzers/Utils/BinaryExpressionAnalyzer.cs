using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ToListinator.Analyzers.Utils;

/// <summary>
/// Provides utilities for analyzing binary expressions, particularly those involving
/// count comparisons and pattern matching for code transformations.
/// </summary>
public static class BinaryExpressionAnalyzer
{
    /// <summary>
    /// Determines if a binary expression represents a count comparison pattern that should be negated
    /// when converted from Count() comparisons to Any() calls.
    /// </summary>
    /// <param name="operatorKind">The comparison operator kind</param>
    /// <param name="constantNode">The node containing the constant value being compared against</param>
    /// <param name="isLeftOperand">True if the count expression is on the left side of the comparison</param>
    /// <returns>True if this pattern should result in !Any() (negated), false for Any()</returns>
    public static bool IsNegatedCountPattern(SyntaxKind operatorKind, SyntaxNode constantNode, bool isLeftOperand)
    {
        if (constantNode is not LiteralExpressionSyntax literal)
        {
            return false;
        }

        var value = literal.Token.ValueText;

        // For patterns that check for existence (Any() = true):
        // collection.Count > 0
        // collection.Count >= 1
        // collection.Count != 0
        // 0 < collection.Count
        // 1 <= collection.Count
        // 0 != collection.Count

        // For patterns that check for non-existence (!Any() = true):
        // collection.Count == 0
        // collection.Count <= 0
        // collection.Count < 1
        // 0 == collection.Count
        // 0 >= collection.Count
        // 1 > collection.Count

        if (isLeftOperand) // collection.Count <op> constant
        {
            return operatorKind switch
            {
                SyntaxKind.EqualsEqualsToken when value == "0" => true,
                SyntaxKind.LessThanEqualsToken when value == "0" => true,
                SyntaxKind.LessThanToken when value == "1" => true,
                _ => false
            };
        }
        else // constant <op> collection.Count
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

    /// <summary>
    /// Checks if a binary expression compares against a specific constant value.
    /// </summary>
    /// <param name="binaryExpression">The binary expression to analyze</param>
    /// <param name="constantValue">The constant value to look for</param>
    /// <param name="isLeftOperand">Output: true if the constant is on the left side</param>
    /// <returns>True if the expression compares against the specified constant</returns>
    public static bool IsComparisonWithConstant(
        BinaryExpressionSyntax binaryExpression,
        string constantValue,
        out bool isLeftOperand)
    {
        isLeftOperand = false;

        // Check if left side is the constant
        if (binaryExpression.Left is LiteralExpressionSyntax leftLiteral &&
            leftLiteral.Token.ValueText == constantValue)
        {
            isLeftOperand = true;
            return true;
        }

        // Check if right side is the constant
        if (binaryExpression.Right is LiteralExpressionSyntax rightLiteral &&
            rightLiteral.Token.ValueText == constantValue)
        {
            isLeftOperand = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a binary expression represents a null coalescing pattern
    /// (e.g., "collection ?? EmptyCollection").
    /// </summary>
    /// <param name="binaryExpression">The binary expression to analyze</param>
    /// <returns>True if this is a null coalescing pattern</returns>
    public static bool IsNullCoalescingPattern(BinaryExpressionSyntax binaryExpression)
    {
        return binaryExpression.OperatorToken.IsKind(SyntaxKind.QuestionQuestionToken);
    }

    /// <summary>
    /// Extracts the non-constant operand from a binary expression when one operand is a constant.
    /// </summary>
    /// <param name="binaryExpression">The binary expression to analyze</param>
    /// <param name="constantValue">The constant value to identify</param>
    /// <returns>The non-constant operand, or null if pattern doesn't match</returns>
    public static ExpressionSyntax? GetNonConstantOperand(
        BinaryExpressionSyntax binaryExpression,
        string constantValue)
    {
        if (binaryExpression.Left is LiteralExpressionSyntax leftLiteral &&
            leftLiteral.Token.ValueText == constantValue)
        {
            return binaryExpression.Right;
        }

        if (binaryExpression.Right is LiteralExpressionSyntax rightLiteral &&
            rightLiteral.Token.ValueText == constantValue)
        {
            return binaryExpression.Left;
        }

        return null;
    }

    /// <summary>
    /// Checks if a binary expression represents a valid count comparison pattern
    /// that can be optimized to Any() or !Any() calls.
    /// </summary>
    /// <param name="binaryExpression">The binary expression to analyze</param>
    /// <returns>True if this is a valid count comparison pattern</returns>
    public static bool IsValidCountComparisonPattern(BinaryExpressionSyntax binaryExpression)
    {
        // Must be a comparison operator
        if (!IsComparisonOperator(binaryExpression.OperatorToken.Kind()))
        {
            return false;
        }

        // One side must be a constant 0 or 1
        return IsComparisonWithConstant(binaryExpression, "0", out _) ||
               IsComparisonWithConstant(binaryExpression, "1", out _);
    }

    /// <summary>
    /// Determines if a syntax kind represents a comparison operator.
    /// </summary>
    /// <param name="operatorKind">The syntax kind to check</param>
    /// <returns>True if it's a comparison operator</returns>
    private static bool IsComparisonOperator(SyntaxKind operatorKind)
    {
        return operatorKind switch
        {
            SyntaxKind.EqualsEqualsToken => true,
            SyntaxKind.ExclamationEqualsToken => true,
            SyntaxKind.LessThanToken => true,
            SyntaxKind.LessThanEqualsToken => true,
            SyntaxKind.GreaterThanToken => true,
            SyntaxKind.GreaterThanEqualsToken => true,
            _ => false
        };
    }
}
