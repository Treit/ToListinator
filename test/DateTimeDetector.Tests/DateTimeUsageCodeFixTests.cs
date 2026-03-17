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
}
