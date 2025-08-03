using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;
using ToListinator.CodeFixes;

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
                var result = {|TL002:numbers.Select(x => x)|};
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

        var test = CodeFixTestHelper.CreateCodeFixTest<IdentitySelectAnalyzer, IdentitySelectCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
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
                var result = {|TL002:numbers.Select((x) => x)|};
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

        var test = CodeFixTestHelper.CreateCodeFixTest<IdentitySelectAnalyzer, IdentitySelectCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
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
                var result = numbers.Where(x => x > 1).{|TL002:Select(y => y)|}.ToList();
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

        var test = CodeFixTestHelper.CreateCodeFixTest<IdentitySelectAnalyzer, IdentitySelectCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
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
                    .{|TL002:Select(item => item)|}
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

        var test = CodeFixTestHelper.CreateCodeFixTest<IdentitySelectAnalyzer, IdentitySelectCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
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
                var result = {|TL002:numbers.Select(x => x)|}; // End comment
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

        var test = CodeFixTestHelper.CreateCodeFixTest<IdentitySelectAnalyzer, IdentitySelectCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
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
                var result1 = {|TL002:numbers.Select(x => x)|};
                var result2 = {|TL002:numbers.Where(n => n > 1).Select(y => y)|};
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

        var test = CodeFixTestHelper.CreateCodeFixTest<IdentitySelectAnalyzer, IdentitySelectCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }
}
