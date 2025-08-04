using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class ArrayEmptyAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForEmptyStringArrayWithExplicitSize()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = {|#0:new string[0]|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ArrayEmptyAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForEmptyStringArrayWithEmptyInitializer()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = {|#0:new string[] {}|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ArrayEmptyAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForEmptyIntArrayWithExplicitSize()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = {|#0:new int[0]|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ArrayEmptyAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForEmptyCustomTypeArray()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = {|#0:new TestClass[0]|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ArrayEmptyAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL006").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNonEmptyArray()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = new string[5];
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ArrayEmptyAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForArrayWithInitializer()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = new string[] { "hello", "world" };
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ArrayEmptyAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForImplicitArrayWithItems()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = new[] { 1, 2, 3 };
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ArrayEmptyAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForMultidimensionalArray()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = new string[2, 3];
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ArrayEmptyAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }
}