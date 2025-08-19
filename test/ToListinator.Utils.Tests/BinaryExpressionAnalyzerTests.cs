using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ToListinator.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.Utils.Tests;

public class BinaryExpressionAnalyzerTests
{
    [Theory]
    [InlineData("0", SyntaxKind.EqualsEqualsToken, true, true)]  // count == 0 -> !Any()
    [InlineData("0", SyntaxKind.LessThanEqualsToken, true, true)] // count <= 0 -> !Any()
    [InlineData("1", SyntaxKind.LessThanToken, true, true)]      // count < 1 -> !Any()
    [InlineData("0", SyntaxKind.GreaterThanToken, true, false)]  // count > 0 -> Any()
    [InlineData("1", SyntaxKind.GreaterThanEqualsToken, true, false)] // count >= 1 -> Any()
    [InlineData("0", SyntaxKind.ExclamationEqualsToken, true, false)] // count != 0 -> Any()
    public void IsNegatedCountPattern_LeftOperand_ReturnsExpectedResult(
        string constantValue,
        SyntaxKind operatorKind,
        bool isLeftOperand,
        bool expectedNegated)
    {
        // Arrange
        var constantNode = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(constantValue));

        // Act
        var result = BinaryExpressionAnalyzer.IsNegatedCountPattern(operatorKind, constantNode, isLeftOperand);

        // Assert
        Assert.Equal(expectedNegated, result);
    }

    [Theory]
    [InlineData("0", SyntaxKind.EqualsEqualsToken, false, true)]  // 0 == count -> !Any()
    [InlineData("0", SyntaxKind.GreaterThanEqualsToken, false, true)] // 0 >= count -> !Any()
    [InlineData("1", SyntaxKind.GreaterThanToken, false, true)]   // 1 > count -> !Any()
    [InlineData("0", SyntaxKind.LessThanToken, false, false)]    // 0 < count -> Any()
    [InlineData("1", SyntaxKind.LessThanEqualsToken, false, false)] // 1 <= count -> Any()
    [InlineData("0", SyntaxKind.ExclamationEqualsToken, false, false)] // 0 != count -> Any()
    public void IsNegatedCountPattern_RightOperand_ReturnsExpectedResult(
        string constantValue,
        SyntaxKind operatorKind,
        bool isLeftOperand,
        bool expectedNegated)
    {
        // Arrange
        var constantNode = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(constantValue));

        // Act
        var result = BinaryExpressionAnalyzer.IsNegatedCountPattern(operatorKind, constantNode, isLeftOperand);

        // Assert
        Assert.Equal(expectedNegated, result);
    }

    [Fact]
    public void IsNegatedCountPattern_NonLiteralNode_ReturnsFalse()
    {
        // Arrange
        var nonLiteralNode = IdentifierName("someVariable");

        // Act
        var result = BinaryExpressionAnalyzer.IsNegatedCountPattern(SyntaxKind.EqualsEqualsToken, nonLiteralNode, true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComparisonWithConstant_LeftSideConstant_ReturnsTrue()
    {
        // Arrange
        var binaryExpression = BinaryExpression(
            SyntaxKind.EqualsExpression,
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal("0")),
            IdentifierName("count"));

        // Act
        var result = BinaryExpressionAnalyzer.IsComparisonWithConstant(binaryExpression, "0", out var isLeftOperand);

        // Assert
        Assert.True(result);
        Assert.True(isLeftOperand);
    }

    [Fact]
    public void IsComparisonWithConstant_RightSideConstant_ReturnsTrue()
    {
        // Arrange
        var binaryExpression = BinaryExpression(
            SyntaxKind.EqualsExpression,
            IdentifierName("count"),
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal("0")));

        // Act
        var result = BinaryExpressionAnalyzer.IsComparisonWithConstant(binaryExpression, "0", out var isLeftOperand);

        // Assert
        Assert.True(result);
        Assert.False(isLeftOperand);
    }

    [Fact]
    public void IsComparisonWithConstant_NoMatchingConstant_ReturnsFalse()
    {
        // Arrange
        var binaryExpression = BinaryExpression(
            SyntaxKind.EqualsExpression,
            IdentifierName("count"),
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal("5")));

        // Act
        var result = BinaryExpressionAnalyzer.IsComparisonWithConstant(binaryExpression, "0", out var isLeftOperand);

        // Assert
        Assert.False(result);
        Assert.False(isLeftOperand);
    }

    [Fact]
    public void IsNullCoalescingPattern_NullCoalescingOperator_ReturnsTrue()
    {
        // Arrange
        var binaryExpression = BinaryExpression(
            SyntaxKind.CoalesceExpression,
            IdentifierName("collection"),
            IdentifierName("emptyCollection"));

        // Act
        var result = BinaryExpressionAnalyzer.IsNullCoalescingPattern(binaryExpression);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNullCoalescingPattern_OtherOperator_ReturnsFalse()
    {
        // Arrange
        var binaryExpression = BinaryExpression(
            SyntaxKind.EqualsExpression,
            IdentifierName("collection"),
            LiteralExpression(SyntaxKind.NullLiteralExpression));

        // Act
        var result = BinaryExpressionAnalyzer.IsNullCoalescingPattern(binaryExpression);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetNonConstantOperand_ConstantOnLeft_ReturnsRightOperand()
    {
        // Arrange
        var rightOperand = IdentifierName("count");
        var binaryExpression = BinaryExpression(
            SyntaxKind.EqualsExpression,
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal("0")),
            rightOperand);

        // Act
        var result = BinaryExpressionAnalyzer.GetNonConstantOperand(binaryExpression, "0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(rightOperand.ToString(), result.ToString());
    }

    [Fact]
    public void GetNonConstantOperand_ConstantOnRight_ReturnsLeftOperand()
    {
        // Arrange
        var leftOperand = IdentifierName("count");
        var binaryExpression = BinaryExpression(
            SyntaxKind.EqualsExpression,
            leftOperand,
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal("0")));

        // Act
        var result = BinaryExpressionAnalyzer.GetNonConstantOperand(binaryExpression, "0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(leftOperand.ToString(), result.ToString());
    }

    [Fact]
    public void GetNonConstantOperand_NoMatchingConstant_ReturnsNull()
    {
        // Arrange
        var binaryExpression = BinaryExpression(
            SyntaxKind.EqualsExpression,
            IdentifierName("count"),
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal("5")));

        // Act
        var result = BinaryExpressionAnalyzer.GetNonConstantOperand(binaryExpression, "0");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("1", true)]
    [InlineData("2", false)]
    public void IsValidCountComparisonPattern_WithDifferentConstants_ReturnsExpectedResult(
        string constantValue,
        bool expectedValid)
    {
        // Arrange
        var binaryExpression = BinaryExpression(
            SyntaxKind.EqualsExpression,
            IdentifierName("count"),
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(constantValue)));

        // Act
        var result = BinaryExpressionAnalyzer.IsValidCountComparisonPattern(binaryExpression);

        // Assert
        Assert.Equal(expectedValid, result);
    }

    [Fact]
    public void IsValidCountComparisonPattern_NonComparisonOperator_ReturnsFalse()
    {
        // Arrange
        var binaryExpression = BinaryExpression(
            SyntaxKind.AddExpression,
            IdentifierName("count"),
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal("0")));

        // Act
        var result = BinaryExpressionAnalyzer.IsValidCountComparisonPattern(binaryExpression);

        // Assert
        Assert.False(result);
    }
}
