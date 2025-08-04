using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class NullCoalescingForeachAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForNewListCoalescing()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;

        public class TestClass
        {
            public void TestMethod()
            {
                List<string>? list = null;
                foreach (var item in {|#0:list ?? new List<string>()|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL004").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForArrayEmptyCoalescing()
    {
        const string testCode = """
        using System;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                string[]? array = null;
                foreach (var item in {|#0:array ?? Array.Empty<string>()|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL004").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForEnumerableEmptyCoalescing()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                IEnumerable<int>? enumerable = null;
                foreach (var item in {|#0:enumerable ?? Enumerable.Empty<int>()|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL004").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForEmptyArrayLiteralCoalescing()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                string[]? array = null;
                foreach (var item in {|#0:array ?? new string[0]|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL004").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForEmptyArrayInitializerCoalescing()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                string[]? array = null;
                foreach (var item in {|#0:array ?? new string[] { }|})
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL004").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForRegularForeach()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;

        public class TestClass
        {
            public void TestMethod()
            {
                var list = new List<string> { "a", "b", "c" };
                foreach (var item in list)
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNonEmptyCoalescing()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;

        public class TestClass
        {
            public void TestMethod()
            {
                List<string>? list = null;
                var fallback = new List<string> { "default" };
                foreach (var item in list ?? fallback)
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNonEmptyArrayCoalescing()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                string[]? array = null;
                foreach (var item in array ?? new string[] { "default" })
                {
                    Console.WriteLine(item);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForCoalescingOutsideForeach()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                List<string>? list = null;
                var result = list ?? new List<string>();
                var count = result.Count();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<NullCoalescingForeachAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }
}
