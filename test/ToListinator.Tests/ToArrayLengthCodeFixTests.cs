using ToListinator.Analyzers;
using ToListinator.CodeFixes;

namespace ToListinator.Tests;

public class ToArrayLengthCodeFixTests
{
    [Fact]
    public async Task ReplaceToArrayLengthGreaterThanZeroWithAny()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|TL011:numbers.ToArray().Length > 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceToArrayLengthGreaterThanOrEqualOneWithAny()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|TL011:numbers.ToArray().Length >= 1|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceToArrayLengthNotEqualsZeroWithAny()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|TL011:numbers.ToArray().Length != 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceReversedComparisonWithAny()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|TL011:0 < numbers.ToArray().Length|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceEqualsZeroWithNotAny()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = {|TL011:numbers.ToArray().Length == 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceLessThanOrEqualZeroWithNotAny()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = {|TL011:numbers.ToArray().Length <= 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceLessThanOneWithNotAny()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = {|TL011:numbers.ToArray().Length < 1|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceWithinComplexExpression()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var result = {|TL011:numbers.Where(x => x > 1).ToArray().Length > 0|} && true;
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var result = numbers.Where(x => x > 1).Any() && true;
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PreservesCommentsWhenReplacing()
    {
        var testCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|TL011:numbers /* comment */.ToArray().Length > 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = numbers /* comment */.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToArrayLengthAnalyzer, ToArrayLengthCodeFixProvider>(
            testCode,
            fixedCode);

        await test.RunAsync(CancellationToken.None);
    }
}
