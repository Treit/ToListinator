using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator.CodeFixes;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ToListinator.Analyzers.ToListCountAnalyzer,
    ToListinator.CodeFixes.ToListCountCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

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
                var hasAny = numbers.ToList().Count > 0;
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
        var expected = Verify.Diagnostic().WithLocation(9, 22);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var hasAny = numbers.ToList().Count >= 1;
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
        var expected = Verify.Diagnostic().WithLocation(9, 22);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var hasAny = numbers.ToList().Count != 0;
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
        var expected = Verify.Diagnostic().WithLocation(9, 22);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var hasAny = 0 < numbers.ToList().Count;
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
        var expected = Verify.Diagnostic().WithLocation(9, 22);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var result = numbers.Where(x => x > 1).ToList().Count > 0;
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
        var expected = Verify.Diagnostic().WithLocation(9, 22);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var result = (numbers.Where(x => x > 1).ToList().Count > 0) && true;
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
        var expected = Verify.Diagnostic().WithLocation(9, 23);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var result = numbers.Select(x => x * 2).Where(x => x > 2).ToList().Count > 0;
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
        var expected = Verify.Diagnostic().WithLocation(9, 22);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var message = numbers.ToList().Count > 0 ? "Has items" : "Empty";
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
        var expected = Verify.Diagnostic().WithLocation(9, 23);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var isEmpty = numbers.ToList().Count == 0;
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
        var expected = Verify.Diagnostic().WithLocation(9, 23);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var isEmpty = numbers.ToList().Count <= 0;
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
        var expected = Verify.Diagnostic().WithLocation(9, 23);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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
                var isEmpty = numbers.ToList().Count < 1;
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
        var expected = Verify.Diagnostic().WithLocation(9, 23);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
