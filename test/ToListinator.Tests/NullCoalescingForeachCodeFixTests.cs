using ToListinator.Analyzers;
using ToListinator.CodeFixes;

namespace ToListinator.Tests;

public class NullCoalescingForeachCodeFixTests
{
    [Fact]
    public async Task ReplaceNewListCoalescingWithNullCheck()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;

        class C
        {
            void M()
            {
                List<string>? list = null;
                foreach (var item in {|TL004:list ?? new List<string>()|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var fixedCode = """
        using System;
        using System.Collections.Generic;

        class C
        {
            void M()
            {
                List<string>? list = null;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        Console.WriteLine(item);
                    }
                }
            }
        }
        """;

        var test = CodeFixTestHelper.CreateCodeFixTest<NullCoalescingForeachAnalyzer, NullCoalescingForeachCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceArrayEmptyCoalescingWithNullCheck()
    {
        var testCode = """
        using System;

        class C
        {
            void M()
            {
                string[]? array = null;
                foreach (var item in {|TL004:array ?? Array.Empty<string>()|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                string[]? array = null;
                if (array != null)
                {
                    foreach (var item in array)
                    {
                        Console.WriteLine(item);
                    }
                }
            }
        }
        """;

        var test = CodeFixTestHelper.CreateCodeFixTest<NullCoalescingForeachAnalyzer, NullCoalescingForeachCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceEnumerableEmptyCoalescingWithNullCheck()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                IEnumerable<int>? enumerable = null;
                foreach (var item in {|TL004:enumerable ?? Enumerable.Empty<int>()|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                IEnumerable<int>? enumerable = null;
                if (enumerable != null)
                {
                    foreach (var item in enumerable)
                    {
                        Console.WriteLine(item);
                    }
                }
            }
        }
        """;

        var test = CodeFixTestHelper.CreateCodeFixTest<NullCoalescingForeachAnalyzer, NullCoalescingForeachCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceEmptyArrayLiteralCoalescingWithNullCheck()
    {
        var testCode = """
        using System;

        class C
        {
            void M()
            {
                string[]? array = null;
                foreach (var item in {|TL004:array ?? new string[0]|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                string[]? array = null;
                if (array != null)
                {
                    foreach (var item in array)
                    {
                        Console.WriteLine(item);
                    }
                }
            }
        }
        """;

        var test = CodeFixTestHelper.CreateCodeFixTest<NullCoalescingForeachAnalyzer, NullCoalescingForeachCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceEmptyArrayInitializerCoalescingWithNullCheck()
    {
        var testCode = """
        using System;

        class C
        {
            void M()
            {
                string[]? array = null;
                foreach (var item in {|TL004:array ?? new string[] { }|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                string[]? array = null;
                if (array != null)
                {
                    foreach (var item in array)
                    {
                        Console.WriteLine(item);
                    }
                }
            }
        }
        """;

        var test = CodeFixTestHelper.CreateCodeFixTest<NullCoalescingForeachAnalyzer, NullCoalescingForeachCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PreserveTrivia()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;

        class C
        {
            void M()
            {
                List<string>? list = null;

                // Check list and iterate
                foreach (var item in {|TL004:list ?? new List<string>()|}) // Comment after
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var fixedCode = """
        using System;
        using System.Collections.Generic;

        class C
        {
            void M()
            {
                List<string>? list = null;

                // Check list and iterate
                if (list != null)
                {
                    foreach (var item in list) // Comment after
                    {
                        Console.WriteLine(item);
                    }
                }
            }
        }
        """;

        var test = CodeFixTestHelper.CreateCodeFixTest<NullCoalescingForeachAnalyzer, NullCoalescingForeachCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceComplexExpression()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var complexList = GetList()?.Where(x => x.Length > 0).ToList();
                foreach (var item in {|TL004:complexList ?? new List<string>()|})
                {
                    Console.WriteLine(item);
                }
            }

            private List<string>? GetList() => null;
        }
        """;

        var fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var complexList = GetList()?.Where(x => x.Length > 0).ToList();
                if (complexList != null)
                {
                    foreach (var item in complexList)
                    {
                        Console.WriteLine(item);
                    }
                }
            }

            private List<string>? GetList() => null;
        }
        """;

        var test = CodeFixTestHelper.CreateCodeFixTest<NullCoalescingForeachAnalyzer, NullCoalescingForeachCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }
}
