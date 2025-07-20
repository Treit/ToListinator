using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator;

public class ToListinatorAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForToListForEach()
    {
        const string testCode = @"
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
}";

        var test = new CSharpAnalyzerTest<ToListinatorAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        };

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning(ToListinatorAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithMessage("Avoid using ToList().ForEach, which allocates a List unnecessarily. Use a regular foreach loop instead.")
        );

        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldNotReportWarningForRegularForEach()
    {
        const string testCode = @"
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
}";

        var test = new CSharpAnalyzerTest<ToListinatorAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldNotReportWarningForToListWithoutForEach()
    {
        const string testCode = @"
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
}";

        var test = new CSharpAnalyzerTest<ToListinatorAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForChainedToListForEach()
    {
        const string testCode = @"
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
}";

        var test = new CSharpAnalyzerTest<ToListinatorAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
        };

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning(ToListinatorAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithMessage("Avoid using ToList().ForEach, which allocates a List unnecessarily. Use a regular foreach loop instead.")
        );

        await test.RunAsync();
    }
}
