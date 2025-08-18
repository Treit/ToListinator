using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;
using ToListinator.CodeFixes;

namespace ToListinator.Tests;

public class CountComparisonCodeFixTests
{
    [Fact]
    public async Task ReplaceCountGreaterThanZeroWithAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|TL009:numbers.Count() > 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceCountGreaterThanOrEqualOneWithAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|TL009:numbers.Count() >= 1|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceCountNotEqualsZeroWithAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|TL009:numbers.Count() != 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceReversedComparisonWithAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|TL009:0 < numbers.Count()|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceReversedGreaterThanOrEqualWithAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|TL009:1 <= numbers.Count()|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceReversedNotEqualsWithAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|TL009:0 != numbers.Count()|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceComplexExpressionWithAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = {|TL009:numbers.Where(x => x > 1).Count() > 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers.Where(x => x > 1).Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PreservesParenthesesInComplexExpression()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = ({|TL009:numbers.Where(x => x > 1).Count() > 0|}) && true;
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = (numbers.Where(x => x > 1).Any()) && true;
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task HandlesMethodChaining()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = {|TL009:numbers.Select(x => x * 2).Where(x => x > 2).Count() > 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers.Select(x => x * 2).Where(x => x > 2).Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task HandlesConditionalExpression()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var message = {|TL009:numbers.Count() > 0|} ? "Has items" : "Empty";
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var message = numbers.Any() ? "Has items" : "Empty";
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceCountEqualsZeroWithNotAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|TL009:numbers.Count() == 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceCountLessThanOrEqualZeroWithNotAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|TL009:numbers.Count() <= 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceCountLessThanOneWithNotAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|TL009:numbers.Count() < 1|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceReversedEqualsZeroWithNotAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|TL009:0 == numbers.Count()|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceReversedGreaterThanOrEqualZeroWithNotAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|TL009:0 >= numbers.Count()|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceReversedGreaterThanOneWithNotAny()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|TL009:1 > numbers.Count()|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = !numbers.Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PreservesCommentsAndFormatting()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                // Check if we have any items
                var hasItems = {|TL009:numbers.Where(x => x > 1).Count() > 0|};  // This should use Any()
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                // Check if we have any items
                var hasItems = numbers.Where(x => x > 1).Any();  // This should use Any()
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task HandlesMultilineFluentChain()
    {
        var testCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasItems = {|TL009:numbers
                    .Where(x => x > 1)
                    .Select(x => x * 2)
                    .Count() > 0|};
            }
        }
        """;

        var fixedCode =
        """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasItems = numbers
                    .Where(x => x > 1)
                    .Select(x => x * 2).Any();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<CountComparisonAnalyzer, CountComparisonCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }
}
