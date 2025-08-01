using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;
using ToListinator.CodeFixes;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ToListinator.Analyzers.ToListForEachAnalyzer,
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
                {|TL001:list.ToList().ForEach(x => Console.WriteLine(x))|};
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

        var test = CodeFixTestHelper.CreateCodeFixTest<ToListForEachAnalyzer, ToListForEachCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task BasicParenthesizedLambda()
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
                list.ToList().ForEach((x) => Console.WriteLine(x));
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

    [Fact]
    public async Task BasicMethodGroupWithCompoundMethodCall()
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
                list.OrderBy(x => x).ToList().ForEach(Console.Write); // Trailing comment
            }

            private static void Print<T>(T item)
            {
                Console.WriteLine(item);
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
                foreach (var x in list.OrderBy(x => x)) // Trailing comment
                {
                    Console.Write(x);
                }
            }

            private static void Print<T>(T item)
            {
                Console.WriteLine(item);
            }
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(12, 9);
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }


    [Fact]
    public async Task BasicMethodGroupWithLeadingAndTrailingTrivia()
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
                list.OrderBy(x => x).ToList().ForEach(Print); // Trailing comment
            }

            private static void Print<T>(T item)
            {
                Console.WriteLine(item);
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
                foreach (var x in list.OrderBy(x => x)) // Trailing comment
                {
                    Print(x);
                }
            }

            private static void Print<T>(T item)
            {
                Console.WriteLine(item);
            }
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(12, 9);
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task BasicDelegateWithLeadingAndTrailingTrivia()
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

                // Process the list with delegate
                list.Where(x => x > 0).ToList().ForEach(delegate(int item) { Console.WriteLine(item); }); // Trailing comment
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

                // Process the list with delegate
                foreach (var item in list.Where(x => x > 0)) // Trailing comment
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;
        var expected = Verify.Diagnostic().WithLocation(12, 9);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task MultiLineChainedLinqOperations()
    {
        var testCode =
        """
        using System;
        using System.Linq;
        using System.Collections.Generic;

        class TestClass
        {
            void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5, 6 };
                numbers
                    .Where(x => x > 2)
                    .Select(x => x * 2)
                    .Where(x => x < 10)
                    .ToList()
                    .ForEach(x => Console.WriteLine(x));
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Linq;
        using System.Collections.Generic;

        class TestClass
        {
            void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5, 6 };
                foreach (var x in numbers
                    .Where(x => x > 2)
                    .Select(x => x * 2)
                    .Where(x => x < 10))
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
    public async Task MultiLineChainedLinqOperationsWithTrailingComment()
    {
        var testCode =
        """
        using System;
        using System.Linq;
        using System.Collections.Generic;

        class TestClass
        {
            void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5, 6 };
                numbers
                    .Where(x => x > 2)
                    .Select(x => x * 2)
                    .Where(x => x < 10) // Important filtering logic
                    .ToList()
                    .ForEach(x => Console.WriteLine(x));
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Linq;
        using System.Collections.Generic;

        class TestClass
        {
            void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5, 6 };
                foreach (var x in numbers
                    .Where(x => x > 2)
                    .Select(x => x * 2)
                    .Where(x => x < 10)) // Important filtering logic
                {
                    Console.WriteLine(x);
                }
            }
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(10, 9);
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
