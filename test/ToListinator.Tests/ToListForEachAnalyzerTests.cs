using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class ToListForEachAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForToListForEach()
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
                {|#0:numbers.ToList().ForEach(x => Console.WriteLine(x))|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListForEachAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL001").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForRegularForEach()
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
                foreach (var number in numbers)
                {
                    Console.WriteLine(number);
                }
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListForEachAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForToListWithoutForEach()
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
                    var list = numbers.ToList();
                    Console.WriteLine(list.Count);
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<ToListForEachAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForChainedToListForEach()
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
                    {|#0:numbers.Where(x => x > 1).ToList().ForEach(x => Console.WriteLine(x))|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<ToListForEachAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL001").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }
}
