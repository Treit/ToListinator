using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;
using ToListinator.CodeFixes;

namespace ToListinator.Tests;

public class ToListCountCodeFixTests
{
    [Fact]
    public async Task ReplaceToListCountGreaterThanZeroWithAny()
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
                var hasAny = {|TL003:numbers.ToList().Count > 0|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceToListCountGreaterThanOrEqualOneWithAny()
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
                var hasAny = {|TL003:numbers.ToList().Count >= 1|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceToListCountNotEqualsZeroWithAny()
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
                var hasAny = {|TL003:numbers.ToList().Count != 0|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
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
                var hasAny = {|TL003:0 < numbers.ToList().Count|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
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
                var result = {|TL003:numbers.Where(x => x > 1).ToList().Count > 0|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
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
                var result = ({|TL003:numbers.Where(x => x > 1).ToList().Count > 0|}) && true;
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
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
                var result = {|TL003:numbers.Select(x => x * 2).Where(x => x > 2).ToList().Count > 0|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
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
                var message = {|TL003:numbers.ToList().Count > 0|} ? "Has items" : "Empty";
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceToListCountEqualsZeroWithNotAny()
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
                var isEmpty = {|TL003:numbers.ToList().Count == 0|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceToListCountLessThanOrEqualZeroWithNotAny()
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
                var isEmpty = {|TL003:numbers.ToList().Count <= 0|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceToListCountLessThanOneWithNotAny()
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
                var isEmpty = {|TL003:numbers.ToList().Count < 1|};
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

        var test = TestHelper.CreateCodeFixTest<ToListCountAnalyzer, ToListCountCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }
}
