using DateTimeDetector.Analyzers;
using Microsoft.CodeAnalysis;

namespace DateTimeDetector.Tests;

public class DateTimeUsageAnalyzerTests
{
    [Fact]
    public async Task DetectsDateTimeLocalVariable()
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

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsDateTimeParameter()
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

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsDateTimeReturnType()
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

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsDateTimeProperty()
    {
        var testCode = """
            using System;

            class C
            {
                {|DT001:DateTime|} Timestamp { get; set; }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsDateTimeField()
    {
        var testCode = """
            using System;

            class C
            {
                private {|DT001:DateTime|} _created;
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsDateTimeUtcNow()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    var now = {|DT001:DateTime|}.UtcNow;
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsFullyQualifiedDateTimeStaticMembers()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var now = System.{|DT001:DateTime|}.Now;
                    var utcNow = System.{|DT001:DateTime|}.UtcNow;
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsTopLevelFullyQualifiedDateTimeStaticMembers()
    {
        var testCode = """
            using System;

            var localNow = System.{|DT001:DateTime|}.Now;
            var utcNow = System.{|DT001:DateTime|}.UtcNow;

            Console.WriteLine(localNow);
            Console.WriteLine(utcNow);
            """;

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsNewDateTime()
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

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFlagDateTimeOffset()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    DateTimeOffset now = DateTimeOffset.Now;
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFlagDateTimeOffsetDateTimeProperty()
    {
        var testCode = """
            using System;

            class C
            {
                void M()
                {
                    var dt = DateTimeOffset.Now.DateTime;
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotFlagNonSystemDateTime()
    {
        var testCode = """
            namespace MyApp
            {
                class DateTime
                {
                    public static DateTime Now => new DateTime();
                }

                class C
                {
                    void M()
                    {
                        DateTime now = DateTime.Now;
                    }
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsDateTimeInGenericTypeArgument()
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

        var test = TestHelper.CreateAnalyzerTest<DateTimeUsageAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }
}
