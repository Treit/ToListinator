using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using ToListinator.Utils;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.Utils.Tests;

public class TriviaHelperTests
{
    [Fact]
    public void ExtractCommentTrivia_WithMixedTrivia_ReturnsOnlyComments()
    {
        // Arrange
        var trivia = TriviaList(
            Whitespace("    "),
            Comment("// Single line comment"),
            EndOfLine("\n"),
            Comment("/* Multi line comment */"),
            Whitespace(" "));

        // Act
        var result = TriviaHelper.ExtractCommentTrivia(trivia);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsKind(SyntaxKind.SingleLineCommentTrivia));
        Assert.True(result[1].IsKind(SyntaxKind.MultiLineCommentTrivia));
    }

    [Fact]
    public void ExtractCommentTrivia_WithNoComments_ReturnsEmpty()
    {
        // Arrange
        var trivia = TriviaList(
            Whitespace("    "),
            EndOfLine("\n"));

        // Act
        var result = TriviaHelper.ExtractCommentTrivia(trivia);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void PreserveTrivia_TransfersLeadingAndTrailingTrivia()
    {
        // Arrange
        var originalNode = ParseExpression("x + y")
            .WithLeadingTrivia(Comment("// Leading"))
            .WithTrailingTrivia(Comment("// Trailing"));

        var newNode = ParseExpression("a * b");

        // Act
        var result = TriviaHelper.PreserveTrivia(newNode, originalNode);

        // Assert
        Assert.Single(result.GetLeadingTrivia());
        Assert.Single(result.GetTrailingTrivia());
        Assert.True(result.GetLeadingTrivia().First().IsKind(SyntaxKind.SingleLineCommentTrivia));
        Assert.True(result.GetTrailingTrivia().First().IsKind(SyntaxKind.SingleLineCommentTrivia));
    }

    [Fact]
    public void PreserveTriviaWithOverrides_UsesCustomLeadingTrivia()
    {
        // Arrange
        var originalNode = ParseExpression("x + y")
            .WithLeadingTrivia(Comment("// Original"))
            .WithTrailingTrivia(Comment("// Original trailing"));

        var newNode = ParseExpression("a * b");
        var customLeading = TriviaList(Comment("// Custom"));

        // Act
        var result = TriviaHelper.PreserveTriviaWithOverrides(
            newNode,
            originalNode,
            leadingTrivia: customLeading);

        // Assert
        Assert.Equal("// Custom", result.GetLeadingTrivia().First().ToString());
        Assert.Equal("// Original trailing", result.GetTrailingTrivia().First().ToString());
    }

    [Fact]
    public void PreserveTrailingComments_AddsCommentsToExistingTrivia()
    {
        // Arrange
        var token = Identifier("test").WithTrailingTrivia(Whitespace(" "));
        var comments = new[] { Comment("// Added comment") };

        // Act
        var result = TriviaHelper.PreserveTrailingComments(token, comments);

        // Assert
        Assert.Equal(2, result.TrailingTrivia.Count);
        Assert.True(result.TrailingTrivia[0].IsKind(SyntaxKind.WhitespaceTrivia));
        Assert.True(result.TrailingTrivia[1].IsKind(SyntaxKind.SingleLineCommentTrivia));
    }

    [Fact]
    public void AddTrailingCommentsWithSpace_AddsSpaceBeforeComments()
    {
        // Arrange
        var token = Identifier("test");
        var comments = new[] { Comment("// Added comment") };

        // Act
        var result = TriviaHelper.AddTrailingCommentsWithSpace(token, comments);

        // Assert
        Assert.True(result.TrailingTrivia.Count >= 2);
        Assert.Contains(result.TrailingTrivia, t => t.IsKind(SyntaxKind.WhitespaceTrivia));
        Assert.Contains(result.TrailingTrivia, t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));
    }

    [Fact]
    public void HasBlankLineBefore_WithBlankLine_ReturnsTrue()
    {
        // Arrange - Create a simple statement with explicit blank line trivia
        var statement = ParseStatement("var x = 1;")
            .WithLeadingTrivia(
                EndOfLine(Environment.NewLine),
                EndOfLine(Environment.NewLine),
                Whitespace("    "));

        // Act
        var result = TriviaHelper.HasBlankLineBefore(statement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasBlankLineBefore_WithoutBlankLine_ReturnsFalse()
    {
        // Arrange - Create a simple statement without blank line trivia
        var statement = ParseStatement("var x = 1;")
            .WithLeadingTrivia(Whitespace("    "));

        // Act
        var result = TriviaHelper.HasBlankLineBefore(statement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EnsureBlankLineBefore_WithoutBlankLine_AddsBlankLine()
    {
        // Arrange
        var statement = ParseStatement("var x = 1;")
            .WithLeadingTrivia(Whitespace("    "));

        // Act
        var result = TriviaHelper.EnsureBlankLineBefore(statement);

        // Assert
        // Check that leading trivia now includes end-of-line characters
        Assert.Contains(result.GetLeadingTrivia(), t => t.IsKind(SyntaxKind.EndOfLineTrivia));
    }

    [Fact]
    public void EnsureBlankLineBefore_WithExistingBlankLine_RemainsUnchanged()
    {
        // Arrange
        var statement = ParseStatement("var x = 1;")
            .WithLeadingTrivia(
                EndOfLine(Environment.NewLine),
                EndOfLine(Environment.NewLine));

        // Act
        var result = TriviaHelper.EnsureBlankLineBefore(statement);

        // Assert
        Assert.Equal(statement.GetLeadingTrivia().Count, result.GetLeadingTrivia().Count);
    }

    [Fact]
    public void CleanExpressionTrivia_RemovesLeadingAndTrailingTrivia()
    {
        // Arrange
        var expression = ParseExpression("  x + y  // comment")
            .WithLeadingTrivia(Whitespace("    "))
            .WithTrailingTrivia(Comment("// trailing"));

        // Act
        var (cleanedExpression, preservedComments) = TriviaHelper.CleanExpressionTrivia(expression);

        // Assert
        Assert.Empty(cleanedExpression.GetLeadingTrivia());
        Assert.NotEmpty(preservedComments); // Comments should be preserved
    }

    [Fact]
    public void TransferTrivia_MovesTriviaFromSourceToDestination()
    {
        // Arrange
        var source = ParseExpression("x")
            .WithLeadingTrivia(Comment("// Source leading"))
            .WithTrailingTrivia(Comment("// Source trailing"));

        var destination = ParseExpression("y");

        // Act
        var result = TriviaHelper.TransferTrivia(source, destination);

        // Assert
        Assert.Equal("// Source leading", result.GetLeadingTrivia().First().ToString());
        Assert.Equal("// Source trailing", result.GetTrailingTrivia().First().ToString());
    }

    [Fact]
    public void CombineTrivia_WithSpaceSeparator_AddsSpace()
    {
        // Arrange
        var existing = TriviaList(Comment("// First"));
        var additional = TriviaList(Comment("// Second"));

        // Act
        var result = TriviaHelper.CombineTrivia(existing, additional, addSpaceSeparator: true);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].IsKind(SyntaxKind.SingleLineCommentTrivia));
        Assert.True(result[1].IsKind(SyntaxKind.WhitespaceTrivia));
        Assert.True(result[2].IsKind(SyntaxKind.SingleLineCommentTrivia));
    }

    [Fact]
    public void CombineTrivia_WithEmptyExisting_ReturnsAdditional()
    {
        // Arrange
        var existing = TriviaList();
        var additional = TriviaList(Comment("// Comment"));

        // Act
        var result = TriviaHelper.CombineTrivia(existing, additional);

        // Assert
        Assert.Equal(additional.Count, result.Count);
        Assert.Equal(additional.First().ToString(), result.First().ToString());
    }
}
