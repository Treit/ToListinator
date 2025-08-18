using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class CountComparisonAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForCountGreaterThanZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:numbers.Count() > 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForCountGreaterThanOrEqualOne()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:numbers.Count() >= 1|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForCountNotEqualsZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:numbers.Count() != 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForReversedComparison()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:0 < numbers.Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForReversedGreaterThanOrEqual()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:1 <= numbers.Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForReversedNotEquals()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:0 != numbers.Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForCountEqualsZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:numbers.Count() == 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForCountLessThanOrEqualZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:numbers.Count() <= 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForCountLessThanOne()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:numbers.Count() < 1|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForReversedEqualsZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:0 == numbers.Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForReversedGreaterThanOrEqualZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:0 >= numbers.Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForReversedGreaterThanOne()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:1 > numbers.Count()|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningInComplexExpression()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var result = {|#0:numbers.Where(x => x > 1).Count() > 0|} && true;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL009").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForCountWithPredicate()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.Count(x => x > 1) > 0;  // Handled by WhereCountAnalyzer (TL006)
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForListCountProperty()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new List<int> { 1, 2, 3 };
                var hasAny = numbers.Count > 0;  // Property, not method
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForArrayLength()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.Length > 0;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForOtherComparisons()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var moreThanTwo = numbers.Count() > 2;
                var exactlyThree = numbers.Count() == 3;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForToListCount()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = numbers.ToList().Count > 0;  // Handled by ToListCountAnalyzer (TL003)
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNonLinqCount()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public int Count() => 42;

            public void TestMethod()
            {
                var hasAny = Count() > 0;  // Not LINQ Count
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<CountComparisonAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }
}
