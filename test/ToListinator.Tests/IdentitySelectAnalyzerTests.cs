using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class IdentitySelectAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForIdentitySelect()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = {|#0:numbers.Select(x => x)|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<IdentitySelectAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL002").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForIdentitySelectWithParenthesizedLambda()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = {|#0:numbers.Select((x) => x)|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<IdentitySelectAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL002").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForChainedIdentitySelect()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers.Where(x => x > 1).{|#0:Select(y => y)|}.ToList();
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<IdentitySelectAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL002").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForTransformingSelect()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = numbers.Select(x => x * 2);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<IdentitySelectAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForSelectWithDifferentParameter()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var other = 42;
                var result = numbers.Select(x => other);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<IdentitySelectAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForSelectWithPropertyAccess()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var people = new[] { new Person { Name = "John" } };
                var result = people.Select(p => p.Name);
            }
        }

        public class Person
        {
            public string Name { get; set; }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<IdentitySelectAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNonEnumerableSelect()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                var custom = new CustomClass();
                custom.Select(x => x);
            }
        }

        public class CustomClass
        {
            public void Select(Func<int, int> selector) { }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<IdentitySelectAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }
}
