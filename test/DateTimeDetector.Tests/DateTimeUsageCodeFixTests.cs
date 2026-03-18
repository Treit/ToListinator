using DateTimeDetector.Analyzers;
using DateTimeDetector.CodeFixes;

namespace DateTimeDetector.Tests;

public class DateTimeUsageCodeFixTests
{
    [Fact]
    public async Task FixesDateTimeLocalVariable()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    {|DT001:DateTime|} now = {|DT001:DateTime|}.Now;
                }
            }
            """;

        var fixedCode = """
            using System;

            class C
            {
                void M()
                {
                    DateTimeOffset now = DateTimeOffset.Now;
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesDateTimeParameter()
    {
        var testCode = """
            using System;

            class C
            {
                void M({|DT001:DateTime|} value)
                {
                }
            }
            """;

        var fixedCode = """
            using System;

            class C
            {
                void M(DateTimeOffset value)
                {
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesDateTimeReturnType()
    {
        var testCode = """
            using System;

            class C
            {
                {|DT001:DateTime|} M()
                {
                    return {|DT001:DateTime|}.Now;
                }
            }
            """;

        var fixedCode = """
            using System;

            class C
            {
                DateTimeOffset M()
                {
                    return DateTimeOffset.Now;
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesDateTimeProperty()
    {
        var testCode = """
            using System;

            class C
            {
                {|DT001:DateTime|} Timestamp { get; set; }
            }
            """;

        var fixedCode = """
            using System;

            class C
            {
                DateTimeOffset Timestamp { get; set; }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesNewDateTime()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    var dt = new {|DT001:DateTime|}();
                }
            }
            """;

        var fixedCode = """
            using System;

            class C
            {
                void M()
                {
                    var dt = new DateTimeOffset();
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesDateTimeInGenericTypeArgument()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<{|DT001:DateTime|}>();
                }
            }
            """;

        var fixedCode = """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<DateTimeOffset>();
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesFullyQualifiedDateTime()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var now = {|DT001:System.DateTime|}.Now;
                    var utcNow = {|DT001:System.DateTime|}.UtcNow;
                }
            }
            """;

        var fixedCode = """
            class C
            {
                void M()
                {
                    var now = System.DateTimeOffset.Now;
                    var utcNow = System.DateTimeOffset.UtcNow;
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesFullyQualifiedDateTimeProperty()
    {
        var testCode = """
            class C
            {
                {|DT001:System.DateTime|} StartTime { get; set; }
            }
            """;

        var fixedCode = """
            class C
            {
                System.DateTimeOffset StartTime { get; set; }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesTopLevelFullyQualifiedDateTime()
    {
        var testCode = """
            using System;

            var localNow = {|DT001:System.DateTime|}.Now;
            var utcNow = {|DT001:System.DateTime|}.UtcNow;

            Console.WriteLine(localNow);
            Console.WriteLine(utcNow);
            """;

        var fixedCode = """
            using System;

            var localNow = System.DateTimeOffset.Now;
            var utcNow = System.DateTimeOffset.UtcNow;

            Console.WriteLine(localNow);
            Console.WriteLine(utcNow);
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, fixedCode);
        test.TestState.OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication;
        test.FixedState.OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication;
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFixDateTimeNowKind()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    var kind = {|DT001:DateTime|}.Now.Kind;
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, testCode);
        test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFixDateTimeNowToBinary()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    var binary = {|DT001:DateTime|}.Now.ToBinary();
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, testCode);
        test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFixDateTimeNowToShortDateString()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    var text = {|DT001:DateTime|}.Now.ToShortDateString();
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, testCode);
        test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFixNewDateTimeThreeArgs()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    var value = new {|DT001:DateTime|}(2024, 1, 2);
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, testCode);
        test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFixNewDateTimeTicks()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    var value = new {|DT001:DateTime|}(638460000000000000L);
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<DateTimeUsageAnalyzer, DateTimeUsageCodeFixProvider>(
            testCode, testCode);
        test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;
        await test.RunAsync(CancellationToken.None);
    }
}
