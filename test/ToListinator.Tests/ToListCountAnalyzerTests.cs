using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;

namespace ToListinator.Tests;

public class ToListCountAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForToListCountGreaterThanZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:numbers.ToList().Count > 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToListCountGreaterThanOrEqualOne()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:numbers.ToList().Count >= 1|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToListCountNotEqualsZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var hasAny = {|#0:numbers.ToList().Count != 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
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
                var hasAny = {|#0:0 < numbers.ToList().Count|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
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
                var hasAny = {|#0:1 <= numbers.ToList().Count|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
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
                var hasAny = {|#0:0 != numbers.ToList().Count|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForRegularCount()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var count = numbers.Count();
                var hasAny = count > 0;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForListCount()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new List<int> { 1, 2, 3 };
                var hasAny = numbers.Count > 0;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(testCode);
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
                var moreThanTwo = numbers.ToList().Count > 2;
                var exactlyThree = numbers.ToList().Count == 3;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(testCode);
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
                var result = {|#0:numbers.Where(x => x > 1).ToList().Count > 0|} && true;
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToListCountEqualsZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:numbers.ToList().Count == 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToListCountLessThanOrEqualZero()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:numbers.ToList().Count <= 0|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForToListCountLessThanOne()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3 };
                var isEmpty = {|#0:numbers.ToList().Count < 1|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
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
                var isEmpty = {|#0:0 == numbers.ToList().Count|};
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<ToListCountAnalyzer>(
            testCode,
            TestHelper.CreateDiagnostic("TL003").WithLocation(0)
        );
        await test.RunAsync(CancellationToken.None);
    }
}
