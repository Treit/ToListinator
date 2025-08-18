using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class ToListToArrayMethodChainAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForToListThenSelect()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { 1, 2, 3 };
                var result = items.{|#0:ToList()|}.Select(x => x.ToString()).ToList();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL007").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToArrayThenSelect()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { 1, 2, 3 };
                var result = items.{|#0:ToArray()|}.Select(x => x.ToString()).ToList();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL007").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToListThenContains()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { "a", "b", "c" };
                var contains = items.Select(x => x.ToUpper()).{|#0:ToList()|}.Contains("A");
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL007").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToArrayThenWhere()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { 1, 2, 3, 4, 5 };
                var result = items.Select(x => x * 2).{|#0:ToArray()|}.Where(x => x > 4).ToList();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL007").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForDictionaryToListThenSelect()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
                var grouped = dict.{|#0:ToList()|}.Select(x => new { x.Key, x.Value }).ToList();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL007").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForFinalToList()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { 1, 2, 3 };
                var result = items.Select(x => x.ToString()).ToList();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForFinalToArray()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { 1, 2, 3 };
                var result = items.Select(x => x.ToString()).ToArray();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForIsolatedToList()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { 1, 2, 3 };
                var list = items.ToList();
                Console.WriteLine(list.Count);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForComplexChain()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { 1, 2, 3, 4, 5 };
                var result = items
                    .Where(x => x > 2)
                    .{|#0:ToList()|}
                    .Select(x => x.ToString())
                    .Where(s => s.Length > 0)
                    .ToList();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL007").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForMethodCallAcceptingEnumerable()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var items = new[] { 1, 2, 3 };
                ProcessItems(items.Select(x => x * 2).{|#0:ToList()|});
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

        var test = TestHelper.CreateAnalyzerTest<ToListToArrayMethodChainAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL007").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }
}
