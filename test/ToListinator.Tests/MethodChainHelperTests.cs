using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using ToListinator.Analyzers.Utils;
using Xunit;

namespace ToListinator.Tests;

public class MethodChainHelperTests
{
    private static InvocationExpressionSyntax CreateInvocation(string code)
    {
        var tree = CSharpSyntaxTree.ParseText($"class Test {{ void M() {{ var x = {code}; }} }}");
        var root = tree.GetRoot();
        var variable = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        return (InvocationExpressionSyntax)variable.Initializer!.Value;
    }

    [Fact]
    public void CollectMethodChain_SingleWhere_ReturnsOneElement()
    {
        // Arrange
        var countCall = CreateInvocation("items.Where(x => x > 0).Count()");
        var countMemberAccess = (MemberAccessExpressionSyntax)countCall.Expression;

        // Act
        var result = MethodChainHelper.CollectMethodChain(countMemberAccess.Expression, "Where");

        // Assert
        Assert.Single(result);
        Assert.Equal("Where", ((MemberAccessExpressionSyntax)result[0].Expression).Name.Identifier.ValueText);
    }

    [Fact]
    public void CollectMethodChain_ChainedWhere_ReturnsBothElements()
    {
        // Arrange
        var countCall = CreateInvocation("items.Where(x => x > 0).Where(x => x < 10).Count()");
        var countMemberAccess = (MemberAccessExpressionSyntax)countCall.Expression;

        // Act
        var result = MethodChainHelper.CollectMethodChain(countMemberAccess.Expression, "Where");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Where", ((MemberAccessExpressionSyntax)result[0].Expression).Name.Identifier.ValueText);
        Assert.Equal("Where", ((MemberAccessExpressionSyntax)result[1].Expression).Name.Identifier.ValueText);
    }

    [Fact]
    public void CollectMethodChain_NoMatchingMethod_ReturnsEmpty()
    {
        // Arrange
        var countCall = CreateInvocation("items.Select(x => x * 2).Count()");
        var countMemberAccess = (MemberAccessExpressionSyntax)countCall.Expression;

        // Act
        var result = MethodChainHelper.CollectMethodChain(countMemberAccess.Expression, "Where");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CollectMethodChain_MultipleMethodNames_ReturnsMatchingCalls()
    {
        // Arrange
        var toListCall = CreateInvocation("items.Select(x => x * 2).Where(x => x > 0).OrderBy(x => x).ToList()");
        var toListMemberAccess = (MemberAccessExpressionSyntax)toListCall.Expression;

        // Act
        var result = MethodChainHelper.CollectMethodChain(toListMemberAccess.Expression, "Select", "Where", "OrderBy");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Select", ((MemberAccessExpressionSyntax)result[0].Expression).Name.Identifier.ValueText);
        Assert.Equal("Where", ((MemberAccessExpressionSyntax)result[1].Expression).Name.Identifier.ValueText);
        Assert.Equal("OrderBy", ((MemberAccessExpressionSyntax)result[2].Expression).Name.Identifier.ValueText);
    }

    [Fact]
    public void GetChainRoot_SimpleChain_ReturnsOriginalExpression()
    {
        // Arrange
        var whereCall = CreateInvocation("items.Where(x => x > 0).Count()");

        // Act
        var result = MethodChainHelper.GetChainRoot(whereCall);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("items", result.ToString());
    }

    [Fact]
    public void GetChainRoot_ComplexChain_ReturnsOriginalExpression()
    {
        // Arrange
        var countCall = CreateInvocation("collection.Select(x => x * 2).Where(x => x > 0).Count()");

        // Act
        var result = MethodChainHelper.GetChainRoot(countCall);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("collection", result.ToString());
    }

    [Fact]
    public void IsMethodCall_MatchingMethod_ReturnsTrue()
    {
        // Arrange
        var countCall = CreateInvocation("items.Where(x => x > 0).Count()");

        // Act
        var result = MethodChainHelper.IsMethodCall(countCall, "Count");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMethodCall_NonMatchingMethod_ReturnsFalse()
    {
        // Arrange
        var countCall = CreateInvocation("items.Where(x => x > 0).Count()");

        // Act
        var result = MethodChainHelper.IsMethodCall(countCall, "Select");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMethodCall_MultipleMethodNames_ReturnsTrue()
    {
        // Arrange
        var countCall = CreateInvocation("items.Where(x => x > 0).Count()");

        // Act
        var result = MethodChainHelper.IsMethodCall(countCall, "Select", "Where", "Count");

        // Assert
        Assert.True(result); // This is Count(), which is in the list
    }

    [Fact]
    public void GetMethodName_ValidInvocation_ReturnsMethodName()
    {
        // Arrange
        var countCall = CreateInvocation("items.Where(x => x > 0).Count()");

        // Act
        var result = MethodChainHelper.GetMethodName(countCall);

        // Assert
        Assert.Equal("Count", result);
    }

    [Fact]
    public void GetMethodName_InvalidInvocation_ReturnsNull()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText("class Test { void M() { var x = Method(); } }");
        var root = tree.GetRoot();
        var variable = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        var invocation = (InvocationExpressionSyntax)variable.Initializer!.Value;

        // Act
        var result = MethodChainHelper.GetMethodName(invocation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMethodChainNames_ComplexChain_ReturnsAllMethodNames()
    {
        // Arrange
        var toListCall = CreateInvocation("items.Select(x => x * 2).Where(x => x > 0).ToList()");

        // Act
        var result = MethodChainHelper.GetMethodChainNames(toListCall);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Select", result[0]);
        Assert.Equal("Where", result[1]);
        Assert.Equal("ToList", result[2]);
    }

    [Fact]
    public void ChainContainsMethod_ExistingMethod_ReturnsTrue()
    {
        // Arrange
        var toListCall = CreateInvocation("items.Select(x => x * 2).Where(x => x > 0).ToList()");

        // Act
        var result = MethodChainHelper.ChainContainsMethod(toListCall, "Where");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ChainContainsMethod_NonExistingMethod_ReturnsFalse()
    {
        // Arrange
        var toListCall = CreateInvocation("items.Select(x => x * 2).Where(x => x > 0).ToList()");

        // Act
        var result = MethodChainHelper.ChainContainsMethod(toListCall, "OrderBy");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FindMethodInChain_ExistingMethod_ReturnsInvocation()
    {
        // Arrange
        var toListCall = CreateInvocation("items.Select(x => x * 2).Where(x => x > 0).ToList()");

        // Act
        var result = MethodChainHelper.FindMethodInChain(toListCall, "Where");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Where", MethodChainHelper.GetMethodName(result));
    }

    [Fact]
    public void FindMethodInChain_NonExistingMethod_ReturnsNull()
    {
        // Arrange
        var toListCall = CreateInvocation("items.Select(x => x * 2).Where(x => x > 0).ToList()");

        // Act
        var result = MethodChainHelper.FindMethodInChain(toListCall, "OrderBy");

        // Assert
        Assert.Null(result); // OrderBy doesn't exist in this chain
    }
}
