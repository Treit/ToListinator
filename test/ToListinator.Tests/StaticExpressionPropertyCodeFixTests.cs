using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;
using ToListinator.CodeFixes;

namespace ToListinator.Tests;

public class StaticExpressionPropertyCodeFixTests
{
    [Fact]
    public async Task ShouldFixEnvironmentMethodCall()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public static string? {|#0:RoleInstance|} => Environment.GetEnvironmentVariable("MONITORING_ROLE_INSTANCE");
        }
        """;

        const string fixedCode = """
        using System;

        public class TestClass
        {
            public static string? RoleInstance { get; } = Environment.GetEnvironmentVariable("MONITORING_ROLE_INSTANCE");
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "RoleInstance").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixNewHashSetCreation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static HashSet<string> {|#0:Items|} => new HashSet<string> { "A", "B", "C" };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static HashSet<string> Items { get; } = new HashSet<string> { "A", "B", "C" };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "Items").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixNewListCreation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static List<int> {|#0:Numbers|} => new List<int> { 1, 2, 3 };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static List<int> Numbers { get; } = new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "Numbers").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixStringSplit()
    {
        const string testCode = """
        public class TestClass
        {
            public static string[] {|#0:Parts|} => "a,b,c".Split(',');
        }
        """;

        const string fixedCode = """
        public class TestClass
        {
            public static string[] Parts { get; } = "a,b,c".Split(',');
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "Parts").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixPropertyWithXmlDocumentation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            /// <summary>
            /// Gets the list of items.
            /// </summary>
            public static List<string> {|#0:Items|} => new List<string> { "A", "B" };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            /// <summary>
            /// Gets the list of items.
            /// </summary>
            public static List<string> Items { get; } = new List<string> { "A", "B" };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "Items").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixPropertyWithAttributes()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;

        public class TestClass
        {
            [Obsolete("Use NewItems instead")]
            public static List<string> {|#0:Items|} => new List<string> { "A", "B" };
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;

        public class TestClass
        {
            [Obsolete("Use NewItems instead")]
            public static List<string> Items { get; } = new List<string> { "A", "B" };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "Items").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixComplexExpression()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public static string {|#0:Complex|} => Environment.GetEnvironmentVariable("PATH") ?? "default";
        }
        """;

        const string fixedCode = """
        using System;

        public class TestClass
        {
            public static string Complex { get; } = Environment.GetEnvironmentVariable("PATH") ?? "default";
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "Complex").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixMultipleProperties()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;

        public class TestClass
        {
            public static string? {|#0:EnvVar|} => Environment.GetEnvironmentVariable("KEY");
            public static List<int> {|#1:Numbers|} => new List<int> { 1, 2, 3 };
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;

        public class TestClass
        {
            public static string? EnvVar { get; } = Environment.GetEnvironmentVariable("KEY");
            public static List<int> Numbers { get; } = new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "EnvVar").WithLocation(0));
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "Numbers").WithLocation(1));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixPropertyWithDifferentAccessModifiers()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            internal static List<string> {|#0:InternalItems|} => new List<string> { "A" };
            protected static List<string> {|#1:ProtectedItems|} => new List<string> { "B" };
            private static List<string> {|#2:PrivateItems|} => new List<string> { "C" };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            internal static List<string> InternalItems { get; } = new List<string> { "A" };
            protected static List<string> ProtectedItems { get; } = new List<string> { "B" };
            private static List<string> PrivateItems { get; } = new List<string> { "C" };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "InternalItems").WithLocation(0));
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "ProtectedItems").WithLocation(1));
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "PrivateItems").WithLocation(2));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixPropertyWithTrailingComments()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static List<string> {|#0:Items|} => new List<string> { "A" }; // This creates a new list
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static List<string> Items { get; } = new List<string> { "A" }; // This creates a new list
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "Items").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixMultiLineCollectionExpression()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public static byte[] {|#0:CharToHexLookupArr|} =>
            [
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 15
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 31
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 47
                0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF  // 63
            ];
        }
        """;

        const string fixedCode = """
        using System;

        public class TestClass
        {
            public static byte[] CharToHexLookupArr { get; } =
            [
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 15
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 31
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 47
                0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF  // 63
            ];
        }
        """;

        var test = TestHelper.CreateCodeFixTest<StaticExpressionPropertyAnalyzer, StaticExpressionPropertyCodeFixProvider>(
            testCode,
            fixedCode
        );
        test.ExpectedDiagnostics.Add(TestHelper.CreateDiagnostic("TL005", "CharToHexLookupArr").WithLocation(0));
        await test.RunAsync(CancellationToken.None);
    }
}
