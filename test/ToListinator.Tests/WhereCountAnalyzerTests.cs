using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class WhereCountAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForWhereCount()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(x => x > 2).Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForWhereCountWithComplexPredicate()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(x => x > 2 && x < 5).Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForWhereCountWithParenthesizedLambda()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where((x) => x > 2).Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForWhereCountWithMethodGroup()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(IsEven).Count()|};
            }

            private bool IsEven(int number) => number % 2 == 0;
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForWhereCountWithAnonymousMethod()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(delegate(int x) { return x > 2; }).Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForChainedWhereCount()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Select(x => x * 2).Where(x => x > 4).Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForCountWithPredicate()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Count(x => x > 2);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForWhereWithoutCount()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var filtered = numbers.Where(x => x > 2);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForCountOnly()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Count();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForWhereWithDifferentOverload()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                // Where with index parameter (different overload)
                var filtered = numbers.Where((x, index) => x > 2 && index > 0);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForCountWithArguments()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Where(x => x > 2).Count(y => y < 10);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }
}
