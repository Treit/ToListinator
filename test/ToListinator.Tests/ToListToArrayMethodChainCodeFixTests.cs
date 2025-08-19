using ToListinator.Analyzers;
using ToListinator.CodeFixes;

namespace ToListinator.Tests;

public class ToListToArrayMethodChainCodeFixTests
{
    [Fact]
    public async Task FixesToListInSelectChain()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var items = new[] { 1, 2, 3 };
                var result = items.{|TL007:ToList()|}.Select(x => x.ToString()).ToList();
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
                var items = new[] { 1, 2, 3 };
                var result = items.Select(x => x.ToString()).ToList();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToListToArrayMethodChainAnalyzer, ToListToArrayMethodChainCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToArrayInSelectChain()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var items = new[] { 1, 2, 3 };
                var result = items.{|TL007:ToArray()|}.Select(x => x.ToString()).ToList();
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
                var items = new[] { 1, 2, 3 };
                var result = items.Select(x => x.ToString()).ToList();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToListToArrayMethodChainAnalyzer, ToListToArrayMethodChainCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToListInContainsChain()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var items = new[] { "a", "b", "c" };
                var contains = items.Select(x => x.ToUpper()).{|TL007:ToList()|}.Contains("A");
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
                var items = new[] { "a", "b", "c" };
                var contains = items.Select(x => x.ToUpper()).Contains("A");
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToListToArrayMethodChainAnalyzer, ToListToArrayMethodChainCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToArrayInWhereChain()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var items = new[] { 1, 2, 3, 4, 5 };
                var result = items.Select(x => x * 2).{|TL007:ToArray()|}.Where(x => x > 4).ToList();
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
                var items = new[] { 1, 2, 3, 4, 5 };
                var result = items.Select(x => x * 2).Where(x => x > 4).ToList();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToListToArrayMethodChainAnalyzer, ToListToArrayMethodChainCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesDictionaryToListInSelectChain()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
                var grouped = dict.{|TL007:ToList()|}.Select(x => new { x.Key, x.Value }).ToList();
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
                var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
                var grouped = dict.Select(x => new { x.Key, x.Value }).ToList();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToListToArrayMethodChainAnalyzer, ToListToArrayMethodChainCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesComplexChain()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var items = new[] { 1, 2, 3, 4, 5 };
                var result = items
                    .Where(x => x > 2)
                    .{|TL007:ToList()|}
                    .Select(x => x.ToString())
                    .Where(s => s.Length > 0)
                    .ToList();
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
                var items = new[] { 1, 2, 3, 4, 5 };
                var result = items
                    .Where(x => x > 2)
                    .Select(x => x.ToString())
                    .Where(s => s.Length > 0)
                    .ToList();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToListToArrayMethodChainAnalyzer, ToListToArrayMethodChainCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesMethodCallAcceptingEnumerable()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var items = new[] { 1, 2, 3 };
                ProcessItems(items.Select(x => x * 2).{|TL007:ToList()|});
            }

            private void ProcessItems(IEnumerable<int> items)
            {
                foreach (var item in items)
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
                var items = new[] { 1, 2, 3 };
                ProcessItems(items.Select(x => x * 2));
            }

            private void ProcessItems(IEnumerable<int> items)
            {
                foreach (var item in items)
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToListToArrayMethodChainAnalyzer, ToListToArrayMethodChainCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PreservesCommentsAndFormatting()
    {
        var testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var items = new[] { 1, 2, 3 };
                // This comment should be preserved
                var result = items
                    .Where(x => x > 1) // Filter comment
                    .{|TL007:ToList()|} // Unnecessary materialization
                    .Select(x => x.ToString()) // Convert to string
                    .ToList();
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
                var items = new[] { 1, 2, 3 };
                // This comment should be preserved
                var result = items
                    .Where(x => x > 1) // Filter comment
                    .Select(x => x.ToString()) // Convert to string
                    .ToList();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ToListToArrayMethodChainAnalyzer, ToListToArrayMethodChainCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }
}
