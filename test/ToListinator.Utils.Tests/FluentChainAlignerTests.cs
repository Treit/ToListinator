using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ToListinator.Utils;
using Xunit;

namespace ToListinator.Utils.Tests;

public class FluentChainAlignerTests
{
    [Fact]
    public void AlignFluentChains_MisalignedChain_ReturnsAlignedChain()
    {
        var source = """
            var count = list
                        .Where(x => x > 0)
                    .Select(x => x + 1)
            .Count(x => x > 0);
            """;

        var expected = """
            var count = list
                        .Where(x => x > 0)
                        .Select(x => x + 1)
                        .Count(x => x > 0);
            """;

        var result = TransformCode(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AlignFluentChains_AlreadyAligned_RemainsUnchanged()
    {
        var source = """
            var count = list
                        .Where(x => x > 0)
                        .Select(x => x + 1)
                        .Count(x => x > 0);
            """;

        var result = TransformCode(source);

        Assert.Equal(source, result);
    }

    [Fact]
    public void AlignFluentChains_SingleMethodCall_RemainsUnchanged()
    {
        var source = """
            var count = list.Count();
            """;

        var result = TransformCode(source);

        Assert.Equal(source, result);
    }

    [Fact]
    public void AlignFluentChains_NoNewlines_RemainsUnchanged()
    {
        var source = """
            var count = list.Where(x => x > 0).Select(x => x + 1).Count(x => x > 0);
            """;

        var result = TransformCode(source);

        Assert.Equal(source, result);
    }

    [Fact]
    public void AlignFluentChains_ComplexExpression_AlignsCorrectly()
    {
        var source = """
            var result = collection
                         .Where(x => x.IsValid)
                     .SelectMany(x => x.Items)
                .Where(item => item.Value > 10)
                         .ToList();
            """;

        var expected = """
            var result = collection
                         .Where(x => x.IsValid)
                         .SelectMany(x => x.Items)
                         .Where(item => item.Value > 10)
                         .ToList();
            """;

        var result = TransformCode(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AlignFluentChains_MultipleChains_AlignsEachIndependently()
    {
        var source = """
            var first = list1
                        .Where(x => x > 0)
                    .Count();

            var second = list2
                    .Select(x => x * 2)
                        .ToList();
            """;

        var expected = """
            var first = list1
                        .Where(x => x > 0)
                        .Count();

            var second = list2
                    .Select(x => x * 2)
                    .ToList();
            """;

        var result = TransformCode(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AlignFluentChains_NestedCalls_AlignsOuterChain()
    {
        var source = """
            var result = data
                         .Where(x => items.Where(i => i.Id == x.Id).Any())
                     .Select(x => x.Value)
                         .ToList();
            """;

        var expected = """
            var result = data
                         .Where(x => items.Where(i => i.Id == x.Id).Any())
                         .Select(x => x.Value)
                         .ToList();
            """;

        var result = TransformCode(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AlignFluentChains_WithComments_PreservesComments()
    {
        var source = """
            var count = list
                        .Where(x => x > 0) // Filter positive
                    .Select(x => x + 1) // Add one
            .Count(); // Get count
            """;

        var expected = """
            var count = list
                        .Where(x => x > 0) // Filter positive
                        .Select(x => x + 1) // Add one
                        .Count(); // Get count
            """;

        var result = TransformCode(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AlignFluentChains_EmptyInput_ReturnsEmpty()
    {
        var source = "";

        var result = TransformCode(source);

        Assert.Equal(source, result);
    }

    [Fact]
    public void AlignFluentChains_DifferentIndentationStyles_UsesFirstAsReference()
    {
        var source = """
            var result = list
              .Where(x => x > 0)
                  .Select(x => x + 1)
                      .Count();
            """;

        var expected = """
            var result = list
              .Where(x => x > 0)
              .Select(x => x + 1)
              .Count();
            """;

        var result = TransformCode(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AlignFluentChains_TabsAndSpaces_HandlesCorrectly()
    {
        var source = "var count = list\n\t\t\t.Where(x => x > 0)\n    .Select(x => x + 1)\n\t.Count();";
        var expected = "var count = list\n\t\t\t.Where(x => x > 0)\n\t\t\t.Select(x => x + 1)\n\t\t\t.Count();";

        var result = TransformCode(source);

        Assert.Equal(expected, result);
    }

    private static string TransformCode(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var transformed = FluentChainAligner.AlignFluentChains(root);
        return transformed.ToFullString();
    }
}
