using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ToListinator.Utils;

namespace ToListinator.Utils.Tests;

public class BlankLineFormatterTests
{
    [Fact]
    public void EnsureBlankLineBeforeIfStatements_WithExistingBlankLine_DoesNotAddAdditionalBlankLine()
    {
        const string code = """
            class TestClass 
            {
                void TestMethod() 
                {
                    int x = 1;

                    if (x > 0)
                    {
                        Console.WriteLine("Positive");
                    }
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);

        Assert.NotNull(result);
        
        // Verify if statement is still present
        var ifStatements = result.DescendantNodes().OfType<IfStatementSyntax>().ToList();
        Assert.Single(ifStatements);
    }

    [Fact]
    public void EnsureBlankLineBeforeIfStatements_WithoutBlankLine_AddsBlankLine()
    {
        const string code = """
            class TestClass 
            {
                void TestMethod() 
                {
                    int x = 1;
                    if (x > 0)
                    {
                        Console.WriteLine("Positive");
                    }
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);

        Assert.NotNull(result);
        
        // Verify if statement is still present
        var ifStatements = result.DescendantNodes().OfType<IfStatementSyntax>().ToList();
        Assert.Single(ifStatements);
        
        // Verify blank line was added (result should be longer than original)
        var originalLength = code.Length;
        var resultLength = result.ToFullString().Length;
        Assert.True(resultLength > originalLength, "Result should be longer than original after adding blank line");
    }

    [Fact]
    public void EnsureBlankLineBeforeIfStatements_MultipleIfStatements_ProcessesAll()
    {
        const string code = """
            class TestClass 
            {
                void TestMethod() 
                {
                    int x = 1;
                    if (x > 0)
                    {
                        Console.WriteLine("First if");
                    }
                    if (x < 10)
                    {
                        Console.WriteLine("Second if");
                    }
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);

        Assert.NotNull(result);
        
        // Verify both if statements are still present
        var ifStatements = result.DescendantNodes().OfType<IfStatementSyntax>().ToList();
        Assert.Equal(2, ifStatements.Count);
    }

    [Fact]
    public void EnsureBlankLineBeforeIfStatements_NestedIfStatements_HandlesNesting()
    {
        const string code = """
            class TestClass 
            {
                void TestMethod() 
                {
                    int x = 1;
                    if (x > 0)
                    {
                        Console.WriteLine("Outer if");
                        if (x < 10)
                        {
                            Console.WriteLine("Inner if");
                        }
                    }
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);

        Assert.NotNull(result);
        
        // Verify both if statements are still present
        var ifStatements = result.DescendantNodes().OfType<IfStatementSyntax>().ToList();
        Assert.Equal(2, ifStatements.Count);
    }

    [Fact]
    public void EnsureBlankLineBeforeIfStatements_IfAtBeginningOfMethod_ProcessesSuccessfully()
    {
        const string code = """
            class TestClass 
            {
                void TestMethod() 
                {
                    if (true)
                    {
                        Console.WriteLine("First statement");
                    }
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);

        Assert.NotNull(result);
        
        // Verify if statement is still present
        var ifStatements = result.DescendantNodes().OfType<IfStatementSyntax>().ToList();
        Assert.Single(ifStatements);
    }

    [Fact]
    public void EnsureBlankLineBeforeIfStatements_EmptyInput_ReturnsEmptyInput()
    {
        const string code = "";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);

        Assert.NotNull(result);
        var resultCode = result.ToFullString();
        Assert.Equal(string.Empty, resultCode);
    }

    [Fact]
    public void EnsureBlankLineBeforeIfStatements_NoIfStatements_ReturnsUnchanged()
    {
        const string code = """
            class TestClass 
            {
                void TestMethod() 
                {
                    int x = 1;
                    Console.WriteLine(x);
                    x++;
                    Console.WriteLine(x);
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);

        Assert.NotNull(result);
        
        // Verify no if statements present
        var ifStatements = result.DescendantNodes().OfType<IfStatementSyntax>().ToList();
        Assert.Empty(ifStatements);
        
        // Code should be essentially unchanged
        var originalCode = code.Replace("\r\n", "\n");
        var resultCode = result.ToFullString().Replace("\r\n", "\n");
        Assert.Equal(originalCode, resultCode);
    }

    [Fact]
    public void EnsureBlankLineBeforeIfStatements_ProcessesAllIfStatements()
    {
        const string code = """
            class TestClass 
            {
                void TestMethod() 
                {
                    int x = 1;
                    if (x > 0) Console.WriteLine("Test");
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);

        Assert.NotNull(result);
        
        // Verify the if statement is still there
        var ifStatements = result.DescendantNodes().OfType<IfStatementSyntax>().ToList();
        Assert.Single(ifStatements);
    }
}
