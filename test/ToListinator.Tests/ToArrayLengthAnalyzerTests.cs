using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class ToArrayLengthAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForToArrayLengthGreaterThanZero()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|#0:numbers.ToArray().Length > 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL011").WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToArrayLengthGreaterThanOrEqualOne()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|#0:numbers.ToArray().Length >= 1|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL011").WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToArrayLengthNotEqualsZero()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|#0:numbers.ToArray().Length != 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL011").WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForReversedComparison()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasAny = {|#0:0 < numbers.ToArray().Length|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL011").WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForEqualsZero()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = {|#0:numbers.ToArray().Length == 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL011").WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForLessThanOrEqualZero()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = {|#0:numbers.ToArray().Length <= 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL011").WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForLessThanOne()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = Enumerable.Range(0, 3);
                var isEmpty = {|#0:numbers.ToArray().Length < 1|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL011").WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForArrayLengthComparison()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var array = new int[3];
                var hasItems = array.Length > 0;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(testCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNonZeroComparison()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = Enumerable.Range(0, 3);
                var hasEnough = numbers.ToArray().Length > 2;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(testCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForCustomToArrayInstanceMethod()
    {
        const string testCode = """
        public class TestClass
        {
            public void TestMethod()
            {
                var custom = new CustomCollection();
                var hasItems = custom.ToArray().Length > 0;
            }
        }

        public class CustomCollection
        {
            public int[] ToArray() => new int[0];
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(testCode);

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForCustomToArrayReturningTypeWithLength()
    {
        const string testCode = """
        public class TestClass
        {
            public void TestMethod()
            {
                var custom = new MySource();
                var hasItems = custom.ToArray().Length > 0;
            }
        }

        public class MyResult
        {
            public int Length => 5;
        }

        public class MySource
        {
            public MyResult ToArray() => new MyResult();
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToArrayLengthAnalyzer>(testCode);

        await test.RunAsync(CancellationToken.None);
    }
}
