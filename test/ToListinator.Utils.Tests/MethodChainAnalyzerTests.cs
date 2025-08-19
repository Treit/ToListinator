using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ToListinator.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.Utils.Tests;

public class MethodChainAnalyzerTests
{
    [Fact]
    public void CollectMethodChain_SingleMethod_ReturnsOneItem()
    {
        // Arrange: items.Where(x => x > 0)
        var whereCall = CreateMethodCall("items", "Where", "x => x > 0");

        // Act
        var result = MethodChainAnalyzer.CollectMethodChain(whereCall, "Where");

        // Assert
        Assert.Single(result);
        Assert.Equal("Where", GetMethodName(result[0]));
    }

    [Fact]
    public void CollectMethodChain_MultipleChainedMethods_ReturnsInForwardOrder()
    {
        // Arrange: items.Where(x => x > 0).Where(x => x < 10)
        var firstWhere = CreateMethodCall("items", "Where", "x => x > 0");
        var secondWhere = CreateChainedMethodCall(firstWhere, "Where", "x => x < 10");

        // Act
        var result = MethodChainAnalyzer.CollectMethodChain(secondWhere, "Where");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Where", GetMethodName(result[0])); // First Where
        Assert.Equal("Where", GetMethodName(result[1])); // Second Where
    }

    [Fact]
    public void CollectMethodChain_MixedChain_OnlyReturnsSpecifiedMethod()
    {
        // Arrange: items.Select(x => x * 2).Where(x => x > 0).Where(x => x < 10).Count()
        var selectCall = CreateMethodCall("items", "Select", "x => x * 2");
        var firstWhere = CreateChainedMethodCall(selectCall, "Where", "x => x > 0");
        var secondWhere = CreateChainedMethodCall(firstWhere, "Where", "x => x < 10");
        var countCall = CreateChainedMethodCall(secondWhere, "Count");

        // Act - Start from the expression inside Count(), which is the second Where() call
        var countExpression = ((MemberAccessExpressionSyntax)countCall.Expression).Expression;
        var result = MethodChainAnalyzer.CollectMethodChain(countExpression, "Where");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, invocation => Assert.Equal("Where", GetMethodName(invocation)));
    }

    [Fact]
    public void CollectMethodChain_MultipleMethodNames_ReturnsMatchingMethods()
    {
        // Arrange: items.Select(x => x * 2).Where(x => x > 0).OrderBy(x => x)
        var selectCall = CreateMethodCall("items", "Select", "x => x * 2");
        var whereCall = CreateChainedMethodCall(selectCall, "Where", "x => x > 0");
        var orderByCall = CreateChainedMethodCall(whereCall, "OrderBy", "x => x");

        // Act
        var result = MethodChainAnalyzer.CollectMethodChain(orderByCall, "Select", "Where", "OrderBy");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Select", GetMethodName(result[0]));
        Assert.Equal("Where", GetMethodName(result[1]));
        Assert.Equal("OrderBy", GetMethodName(result[2]));
    }

    [Fact]
    public void GetChainRoot_SimpleChain_ReturnsRootExpression()
    {
        // Arrange: items.Where(x => x > 0)
        var whereCall = CreateMethodCall("items", "Where", "x => x > 0");

        // Act
        var result = MethodChainAnalyzer.GetChainRoot(whereCall);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IdentifierNameSyntax>(result);
        Assert.Equal("items", ((IdentifierNameSyntax)result).Identifier.ValueText);
    }

    [Fact]
    public void GetChainRoot_ComplexChain_ReturnsRootExpression()
    {
        // Arrange: items.Select(x => x * 2).Where(x => x > 0).Count()
        var selectCall = CreateMethodCall("items", "Select", "x => x * 2");
        var whereCall = CreateChainedMethodCall(selectCall, "Where", "x => x > 0");
        var countCall = CreateChainedMethodCall(whereCall, "Count");

        // Act
        var result = MethodChainAnalyzer.GetChainRoot(countCall);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IdentifierNameSyntax>(result);
        Assert.Equal("items", ((IdentifierNameSyntax)result).Identifier.ValueText);
    }

    [Fact]
    public void IsMethodCall_MatchingMethodName_ReturnsTrue()
    {
        // Arrange
        var whereCall = CreateMethodCall("items", "Where", "x => x > 0");

        // Act
        var result = MethodChainAnalyzer.IsMethodCall(whereCall, "Where");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMethodCall_NonMatchingMethodName_ReturnsFalse()
    {
        // Arrange
        var whereCall = CreateMethodCall("items", "Where", "x => x > 0");

        // Act
        var result = MethodChainAnalyzer.IsMethodCall(whereCall, "Select");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMethodCall_MultipleMethodNames_MatchingOne_ReturnsTrue()
    {
        // Arrange
        var whereCall = CreateMethodCall("items", "Where", "x => x > 0");

        // Act
        var result = MethodChainAnalyzer.IsMethodCall(whereCall, "Select", "Where", "OrderBy");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetMethodName_ValidInvocation_ReturnsMethodName()
    {
        // Arrange
        var whereCall = CreateMethodCall("items", "Where", "x => x > 0");

        // Act
        var result = MethodChainAnalyzer.GetMethodName(whereCall);

        // Assert
        Assert.Equal("Where", result);
    }

    [Fact]
    public void GetMethodName_InvalidInvocation_ReturnsNull()
    {
        // Arrange: Create an invocation that's not a member access
        var invocation = InvocationExpression(IdentifierName("SomeMethod"));

        // Act
        var result = MethodChainAnalyzer.GetMethodName(invocation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMethodChainNames_CompleteChain_ReturnsAllMethodNames()
    {
        // Arrange: items.Select(x => x * 2).Where(x => x > 0).ToList()
        var selectCall = CreateMethodCall("items", "Select", "x => x * 2");
        var whereCall = CreateChainedMethodCall(selectCall, "Where", "x => x > 0");
        var toListCall = CreateChainedMethodCall(whereCall, "ToList");

        // Act
        var result = MethodChainAnalyzer.GetMethodChainNames(toListCall);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Select", result[0]);
        Assert.Equal("Where", result[1]);
        Assert.Equal("ToList", result[2]);
    }

    [Fact]
    public void ChainContainsMethod_MethodPresent_ReturnsTrue()
    {
        // Arrange: items.Select(x => x * 2).Where(x => x > 0).ToList()
        var selectCall = CreateMethodCall("items", "Select", "x => x * 2");
        var whereCall = CreateChainedMethodCall(selectCall, "Where", "x => x > 0");
        var toListCall = CreateChainedMethodCall(whereCall, "ToList");

        // Act
        var result = MethodChainAnalyzer.ChainContainsMethod(toListCall, "Where");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ChainContainsMethod_MethodNotPresent_ReturnsFalse()
    {
        // Arrange: items.Select(x => x * 2).ToList()
        var selectCall = CreateMethodCall("items", "Select", "x => x * 2");
        var toListCall = CreateChainedMethodCall(selectCall, "ToList");

        // Act
        var result = MethodChainAnalyzer.ChainContainsMethod(toListCall, "Where");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FindMethodInChain_MethodPresent_ReturnsInvocation()
    {
        // Arrange: items.Select(x => x * 2).Where(x => x > 0).ToList()
        var selectCall = CreateMethodCall("items", "Select", "x => x * 2");
        var whereCall = CreateChainedMethodCall(selectCall, "Where", "x => x > 0");
        var toListCall = CreateChainedMethodCall(whereCall, "ToList");

        // Act
        var result = MethodChainAnalyzer.FindMethodInChain(toListCall, "Where");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Where", MethodChainAnalyzer.GetMethodName(result));
    }

    [Fact]
    public void FindMethodInChain_MethodNotPresent_ReturnsNull()
    {
        // Arrange: items.Select(x => x * 2).ToList()
        var selectCall = CreateMethodCall("items", "Select", "x => x * 2");
        var toListCall = CreateChainedMethodCall(selectCall, "ToList");

        // Act
        var result = MethodChainAnalyzer.FindMethodInChain(toListCall, "Where");

        // Assert
        Assert.Null(result);
    }

    // Helper methods for creating test syntax nodes

    private static InvocationExpressionSyntax CreateMethodCall(string objectName, string methodName, string? argument = null)
    {
        var memberAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName(objectName),
            IdentifierName(methodName));

        var argumentList = argument != null
            ? ArgumentList(SingletonSeparatedList(Argument(ParseExpression(argument))))
            : ArgumentList();

        return InvocationExpression(memberAccess, argumentList);
    }

    private static InvocationExpressionSyntax CreateChainedMethodCall(
        ExpressionSyntax previousExpression,
        string methodName,
        string? argument = null)
    {
        var memberAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            previousExpression,
            IdentifierName(methodName));

        var argumentList = argument != null
            ? ArgumentList(SingletonSeparatedList(Argument(ParseExpression(argument))))
            : ArgumentList();

        return InvocationExpression(memberAccess, argumentList);
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.Identifier.ValueText
            : null;
    }
}
