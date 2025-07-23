using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator.CodeFixes;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ToListinator.Analyzers.IdentitySelectAnalyzer,
    ToListinator.CodeFixes.IdentitySelectCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace ToListinator.Tests;

public class IdentitySelectCodeFixTests
{
    [Fact]
    public async Task BasicIdentitySelect()
    {
        var testCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers.Select(x => x);
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers;
            }
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(10, 22);
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task IdentitySelectWithParenthesizedLambda()
    {
        var testCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers.Select((x) => x);
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers;
            }
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(10, 22);
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ChainedIdentitySelect()
    {
        var testCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers.Where(x => x > 1).Select(y => y).ToList();
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers.Where(x => x > 1).ToList();
            }
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(10, 48);
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task IdentitySelectInComplexExpression()
    {
        var testCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers
                    .Where(x => x > 0)
                    .Select(item => item)
                    .OrderBy(x => x)
                    .ToArray();
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers
                    .Where(x => x > 0)
                    .OrderBy(x => x)
                    .ToArray();
            }
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(12, 14);
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task IdentitySelectWithComments()
    {
        var testCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                // This is a comment
                var result = numbers.Select(x => x); // End comment
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                // This is a comment
                var result = numbers; // End comment
            }
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(11, 22);
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task MultipleIdentitySelects()
    {
        var testCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result1 = numbers.Select(x => x);
                var result2 = numbers.Where(n => n > 1).Select(y => y);
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var result1 = numbers;
                var result2 = numbers.Where(n => n > 1);
            }
        }
        """;

        var expected = new[]
        {
            Verify.Diagnostic().WithLocation(10, 23),
            Verify.Diagnostic().WithLocation(11, 23)
        };
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
