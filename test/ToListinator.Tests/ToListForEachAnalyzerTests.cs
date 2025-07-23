using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ToListinator.Analyzers.ToListForEachAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

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

        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
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

        await Verify.VerifyAnalyzerAsync(testCode);
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

        await Verify.VerifyAnalyzerAsync(testCode);
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
        var expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }
}
