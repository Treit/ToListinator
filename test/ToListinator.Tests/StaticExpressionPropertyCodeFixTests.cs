using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ToListinator.Analyzers.StaticExpressionPropertyAnalyzer,
    ToListinator.CodeFixes.StaticExpressionPropertyCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

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

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("RoleInstance");
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Items");
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Numbers");
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Parts");
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Items");
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Items");
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Complex");
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = new[]
        {
            Verify.Diagnostic().WithLocation(0).WithArguments("EnvVar"),
            Verify.Diagnostic().WithLocation(1).WithArguments("Numbers")
        };
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = new[]
        {
            Verify.Diagnostic().WithLocation(0).WithArguments("InternalItems"),
            Verify.Diagnostic().WithLocation(1).WithArguments("ProtectedItems"),
            Verify.Diagnostic().WithLocation(2).WithArguments("PrivateItems")
        };
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
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

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Items");
        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
