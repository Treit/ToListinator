using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator;
using ToListinator.CodeFixes;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ToListinator.ToListForEachAnalyzer,
    ToListinator.CodeFixes.ToListForEachCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace ToListinator.Tests;

public class ToListForEachCodeFixTests
{
    [Fact]
    public async Task BasicLambda()
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
                var list = new List<int> { 1, 2, 3 };
                list.ToList().ForEach(x => Console.WriteLine(x));
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
                var list = new List<int> { 1, 2, 3 };
                foreach (var x in list)
                {
                    Console.WriteLine(x);
                }
            }
        }
        """;
        var expected = Verify.Diagnostic().WithLocation(10, 9);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task BasicLambdaChainedToWhere()
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
                var list = new List<int> { 1, 2, 3 };
                list.Select(x => x).OrderBy(x => x).Where(x => x != -1).Where(x => x > 1).ToList().ForEach(x => Console.WriteLine(x));
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
                var list = new List<int> { 1, 2, 3 };
                foreach (var x in list.Select(x => x).OrderBy(x => x).Where(x => x != -1).Where(x => x > 1))
                {
                    Console.WriteLine(x);
                }
            }
        }
        """;
        var expected = Verify.Diagnostic().WithLocation(10, 9);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task BasicLambdaChainedToWhereWithBasicLeadingTrivia()
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
                var list = new List<int> { 1, 2, 3 };

                // Process the list
                list.Select(x => x).OrderBy(x => x).Where(x => x != -1).Where(x => x > 1).ToList().ForEach(x => Console.WriteLine(x));
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
                var list = new List<int> { 1, 2, 3 };

                // Process the list
                foreach (var x in list.Select(x => x).OrderBy(x => x).Where(x => x != -1).Where(x => x > 1))
                {
                    Console.WriteLine(x);
                }
            }
        }
        """;
        var expected = Verify.Diagnostic().WithLocation(12, 9);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task BasicLambdaChainedToWhereWithBasicLeadingAndTrailingTrivia()
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
                var list = new List<int> { 1, 2, 3 };

                // Process the list
                list.Select(x => x).OrderBy(x => x).Where(x => x != -1).Where(x => x > 1).ToList().ForEach(x => Console.WriteLine(x)); // Trailing comment
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
                var list = new List<int> { 1, 2, 3 };

                // Process the list
                foreach (var x in list.Select(x => x).OrderBy(x => x).Where(x => x != -1).Where(x => x > 1)) // Trailing comment
                {
                    Console.WriteLine(x);
                }
            }
        }
        """;
        var expected = Verify.Diagnostic().WithLocation(12, 9);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
