using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ToListinator.Analyzers.ToListCountAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        await Verify.VerifyAnalyzerAsync(testCode);
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

        await Verify.VerifyAnalyzerAsync(testCode);
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

        await Verify.VerifyAnalyzerAsync(testCode);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }
}
