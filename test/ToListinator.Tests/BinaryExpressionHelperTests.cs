using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using ToListinator.Analyzers.Utils;
using Xunit;

namespace ToListinator.Tests;

public class BinaryExpressionHelperTests
{
    private static BinaryExpressionSyntax CreateBinaryExpression(string code)
    {
        var tree = CSharpSyntaxTree.ParseText($"class Test {{ void M() {{ var x = {code}; }} }}");
        var root = tree.GetRoot();
        var variable = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        return (BinaryExpressionSyntax)variable.Initializer!.Value;
    }

    private static LiteralExpressionSyntax CreateLiteral(string value)
    {
        var tree = CSharpSyntaxTree.ParseText($"class Test {{ void M() {{ var x = {value}; }} }}");
        var root = tree.GetRoot();
        var variable = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        return (LiteralExpressionSyntax)variable.Initializer!.Value;
    }

    [Theory]
    [InlineData(SyntaxKind.EqualsEqualsToken, "0", true, true)]
    [InlineData(SyntaxKind.LessThanEqualsToken, "0", true, true)]
    [InlineData(SyntaxKind.LessThanToken, "1", true, true)]
    [InlineData(SyntaxKind.GreaterThanToken, "0", true, false)]
    [InlineData(SyntaxKind.GreaterThanEqualsToken, "1", true, false)]
    [InlineData(SyntaxKind.ExclamationEqualsToken, "0", true, false)]
    public void IsNegatedCountPattern_LeftOperand_ReturnsExpectedResult(
        SyntaxKind operatorKind, string constantValue, bool isLeftOperand, bool expected)
    {
        // Arrange
        var constantNode = CreateLiteral(constantValue);

        // Act
        var result = BinaryExpressionHelper.IsNegatedCountPattern(operatorKind, constantNode, isLeftOperand);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(SyntaxKind.EqualsEqualsToken, "0", false, true)]
    [InlineData(SyntaxKind.GreaterThanEqualsToken, "0", false, true)]
    [InlineData(SyntaxKind.GreaterThanToken, "1", false, true)]
    [InlineData(SyntaxKind.LessThanToken, "0", false, false)]
    [InlineData(SyntaxKind.LessThanEqualsToken, "1", false, false)]
    [InlineData(SyntaxKind.ExclamationEqualsToken, "0", false, false)]
    public void IsNegatedCountPattern_RightOperand_ReturnsExpectedResult(
        SyntaxKind operatorKind, string constantValue, bool isLeftOperand, bool expected)
    {
        // Arrange
        var constantNode = CreateLiteral(constantValue);

        // Act
        var result = BinaryExpressionHelper.IsNegatedCountPattern(operatorKind, constantNode, isLeftOperand);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsNegatedCountPattern_NonLiteralNode_ReturnsFalse()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText("class Test { void M() { var x = count > y; } }");
        var root = tree.GetRoot();
        var variable = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        var binaryExpr = (BinaryExpressionSyntax)variable.Initializer!.Value;
        var nonLiteralNode = binaryExpr.Right;

        // Act
        var result = BinaryExpressionHelper.IsNegatedCountPattern(SyntaxKind.EqualsEqualsToken, nonLiteralNode, true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComparisonWithConstant_LeftConstant_ReturnsCorrectResult()
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression("0 == count");

        // Act
        var result = BinaryExpressionHelper.IsComparisonWithConstant(binaryExpression, "0", out var isLeftOperand);

        // Assert
        Assert.True(result);
        Assert.True(isLeftOperand);
    }

    [Fact]
    public void IsComparisonWithConstant_RightConstant_ReturnsCorrectResult()
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression("count == 0");

        // Act
        var result = BinaryExpressionHelper.IsComparisonWithConstant(binaryExpression, "0", out var isLeftOperand);

        // Assert
        Assert.True(result);
        Assert.False(isLeftOperand);
    }

    [Fact]
    public void IsComparisonWithConstant_NoMatchingConstant_ReturnsFalse()
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression("count == 5");

        // Act
        var result = BinaryExpressionHelper.IsComparisonWithConstant(binaryExpression, "0", out var isLeftOperand);

        // Assert
        Assert.False(result);
        Assert.False(isLeftOperand);
    }

    [Fact]
    public void IsNullCoalescingPattern_WithNullCoalescing_ReturnsTrue()
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression("collection ?? empty");

        // Act
        var result = BinaryExpressionHelper.IsNullCoalescingPattern(binaryExpression);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNullCoalescingPattern_WithoutNullCoalescing_ReturnsFalse()
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression("collection == empty");

        // Act
        var result = BinaryExpressionHelper.IsNullCoalescingPattern(binaryExpression);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetNonConstantOperand_LeftConstant_ReturnsRightOperand()
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression("0 == count");

        // Act
        var result = BinaryExpressionHelper.GetNonConstantOperand(binaryExpression, "0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("count", result.ToString());
    }

    [Fact]
    public void GetNonConstantOperand_RightConstant_ReturnsLeftOperand()
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression("count == 0");

        // Act
        var result = BinaryExpressionHelper.GetNonConstantOperand(binaryExpression, "0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("count", result.ToString());
    }

    [Fact]
    public void GetNonConstantOperand_NoMatchingConstant_ReturnsNull()
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression("count == 5");

        // Act
        var result = BinaryExpressionHelper.GetNonConstantOperand(binaryExpression, "0");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("count == 0", true)]
    [InlineData("count != 1", true)]
    [InlineData("count > 0", true)]
    [InlineData("count < 1", true)]
    [InlineData("count >= 0", true)]
    [InlineData("count <= 1", true)]
    [InlineData("0 == count", true)]
    [InlineData("1 != count", true)]
    public void IsValidCountComparisonPattern_ValidPatterns_ReturnsTrue(string expression, bool expected)
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression(expression);

        // Act
        var result = BinaryExpressionHelper.IsValidCountComparisonPattern(binaryExpression);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("count + 0", false)]
    [InlineData("count - 1", false)]
    [InlineData("count * 2", false)]
    [InlineData("count && true", false)]
    [InlineData("count || false", false)]
    public void IsValidCountComparisonPattern_InvalidPatterns_ReturnsFalse(string expression, bool expected)
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression(expression);

        // Act
        var result = BinaryExpressionHelper.IsValidCountComparisonPattern(binaryExpression);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("count == 2", false)]
    [InlineData("count > 5", false)]
    [InlineData("count < 10", false)]
    public void IsValidCountComparisonPattern_WrongConstants_ReturnsFalse(string expression, bool expected)
    {
        // Arrange
        var binaryExpression = CreateBinaryExpression(expression);

        // Act
        var result = BinaryExpressionHelper.IsValidCountComparisonPattern(binaryExpression);

        // Assert
        Assert.Equal(expected, result);
    }
}
