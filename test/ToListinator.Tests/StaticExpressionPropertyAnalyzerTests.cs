using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ToListinator.Analyzers.StaticExpressionPropertyAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace ToListinator.Tests;

public class StaticExpressionPropertyAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForEnvironmentMethodCall()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public static string? {|#0:RoleInstance|} => Environment.GetEnvironmentVariable("MONITORING_ROLE_INSTANCE");
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("RoleInstance");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForNewHashSetCreation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static HashSet<string> {|#0:Items|} => new HashSet<string> { "A", "B", "C" };
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Items");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForNewListCreation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static List<int> {|#0:Numbers|} => new List<int> { 1, 2, 3 };
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Numbers");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForNewDictionaryCreation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static Dictionary<string, int> {|#0:Map|} => new Dictionary<string, int>();
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Map");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForStringSplit()
    {
        const string testCode = """
        public class TestClass
        {
            public static string[] {|#0:Parts|} => "a,b,c".Split(',');
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Parts");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForStringToUpper()
    {
        const string testCode = """
        public class TestClass
        {
            public static string {|#0:Upper|} => "hello".ToUpper();
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Upper");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForLinqWhere()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            private static readonly int[] data = { 1, 2, 3, 4, 5 };
            public static IEnumerable<int> {|#0:Filtered|} => data.Where(x => x > 0);
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Filtered");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForToList()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            private static readonly int[] data = { 1, 2, 3, 4, 5 };
            public static List<int> {|#0:List|} => data.ToList();
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("List");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForArrayCreation()
    {
        const string testCode = """
        public class TestClass
        {
            public static int[] {|#0:Array|} => new int[] { 1, 2, 3 };
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Array");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNonStaticProperty()
    {
        const string testCode = """
        public class TestClass
        {
            public string Instance => "value";
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldNotReportWarningForGetterOnlyProperty()
    {
        const string testCode = """
        using System;

        public class TestClass
        {
            public static string? Cached { get; } = Environment.GetEnvironmentVariable("KEY");
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldNotReportWarningForStringLiteral()
    {
        const string testCode = """
        public class TestClass
        {
            public static string Literal => "constant";
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldNotReportWarningForIntegerLiteral()
    {
        const string testCode = """
        public class TestClass
        {
            public static int Number => 42;
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldNotReportWarningForBooleanLiteral()
    {
        const string testCode = """
        public class TestClass
        {
            public static bool Flag => true;
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldNotReportWarningForStaticFieldReference()
    {
        const string testCode = """
        public class TestClass
        {
            private static readonly string _field = "value";
            public static string Property => _field;
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldNotReportWarningForSimpleArithmetic()
    {
        const string testCode = """
        public class TestClass
        {
            public static int Calculate => 5 + 3;
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldReportWarningForConditionalWithAllocation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            private static readonly bool condition = true;
            public static List<string> {|#0:Conditional|} => condition ? new List<string> { "yes" } : new List<string> { "no" };
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Conditional");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldNotReportWarningForConditionalWithoutAllocation()
    {
        const string testCode = """
        public class TestClass
        {
            private static readonly bool condition = true;
            public static string Conditional => condition ? "yes" : "no";
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldReportWarningForMultipleViolations()
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

        var expected = new[]
        {
            Verify.Diagnostic().WithLocation(0).WithArguments("EnvVar"),
            Verify.Diagnostic().WithLocation(1).WithArguments("Numbers")
        };
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForToFrozenSet()
    {
        const string testCode = """
        using System.Collections.Frozen;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            private static readonly HashSet<string> _data = new HashSet<string> { "A", "B", "C" };
            public static FrozenSet<string> {|#0:Data4|} => _data.ToFrozenSet();
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<StaticExpressionPropertyAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TL005", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Data4"));
        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForToImmutableArray()
    {
        const string testCode = """
        using System.Collections.Immutable;
        using System.Linq;

        public class TestClass
        {
            private static readonly int[] _data = { 1, 2, 3 };
            public static ImmutableArray<int> {|#0:Numbers|} => _data.ToImmutableArray();
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<StaticExpressionPropertyAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TL005", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Numbers"));
        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForToImmutableList()
    {
        const string testCode = """
        using System.Collections.Immutable;
        using System.Linq;

        public class TestClass
        {
            private static readonly int[] _data = { 1, 2, 3 };
            public static ImmutableList<int> {|#0:Numbers|} => _data.ToImmutableList();
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<StaticExpressionPropertyAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TL005", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Numbers"));
        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForToImmutableHashSet()
    {
        const string testCode = """
        using System.Collections.Immutable;
        using System.Linq;

        public class TestClass
        {
            private static readonly string[] _data = { "A", "B", "C" };
            public static ImmutableHashSet<string> {|#0:Items|} => _data.ToImmutableHashSet();
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<StaticExpressionPropertyAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TL005", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Items"));
        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForToImmutableDictionary()
    {
        const string testCode = """
        using System.Collections.Immutable;
        using System.Linq;

        public class TestClass
        {
            private static readonly string[] _data = { "A", "B", "C" };
            public static ImmutableDictionary<string, int> {|#0:Map|} => _data.ToImmutableDictionary(x => x, x => x.Length);
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<StaticExpressionPropertyAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TL005", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Map"));
        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForToImmutableSortedSet()
    {
        const string testCode = """
        using System.Collections.Immutable;
        using System.Linq;

        public class TestClass
        {
            private static readonly int[] _data = { 3, 1, 2 };
            public static ImmutableSortedSet<int> {|#0:SortedNumbers|} => _data.ToImmutableSortedSet();
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<StaticExpressionPropertyAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TL005", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("SortedNumbers"));
        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForToImmutableSortedDictionary()
    {
        const string testCode = """
        using System.Collections.Immutable;
        using System.Linq;

        public class TestClass
        {
            private static readonly string[] _data = { "C", "A", "B" };
            public static ImmutableSortedDictionary<string, int> {|#0:SortedMap|} => _data.ToImmutableSortedDictionary(x => x, x => x.Length);
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<StaticExpressionPropertyAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TL005", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("SortedMap"));
        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForToLookup()
    {
        const string testCode = """
        using System.Linq;

        public class TestClass
        {
            private static readonly string[] _data = { "Apple", "Banana", "Apricot" };
            public static ILookup<char, string> {|#0:GroupedByFirstLetter|} => _data.ToLookup(x => x[0]);
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<StaticExpressionPropertyAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("TL005", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("GroupedByFirstLetter"));
        await test.RunAsync();
    }

    [Fact]
    public async Task ShouldReportWarningForDiscardAssignmentWithAllocation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static HashSet<string> {|#0:Data5|} => _ = new HashSet<string> { "A", "B", "C" };
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Data5");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNullCoalescingAssignment()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            private static HashSet<string>? _data;
            public static HashSet<string> Data2 => _data ??= new HashSet<string> { "A", "B", "C" };
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldReportWarningForRegularAssignmentWithAllocation()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            public static HashSet<string> {|#0:Data|} => _data = new HashSet<string> { "A", "B", "C" };
            private static HashSet<string> _data;
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Data");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldNotReportWarningForIsNotNullConditionalInitialization()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            private static HashSet<string>? _data;
            public static HashSet<string> Data => _data is not null ? _data : new HashSet<string> { "A", "B", "C" };
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldNotReportWarningForNotEqualsNullConditionalInitialization()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            private static HashSet<string>? _data;
            public static HashSet<string> Data => _data != null ? _data : new HashSet<string> { "A", "B", "C" };
        }
        """;

        await Verify.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ShouldReportWarningForConditionalWithAllocatingBranches()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            private static bool _condition = true;
            public static HashSet<string> {|#0:Data|} => _condition ? new HashSet<string> { "A" } : new HashSet<string> { "B" };
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Data");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ShouldReportWarningForConditionalWithDifferentFieldNames()
    {
        const string testCode = """
        using System.Collections.Generic;

        public class TestClass
        {
            private static HashSet<string>? _data;
            private static HashSet<string>? _otherData;
            public static HashSet<string> {|#0:Data|} => _data != null ? _otherData : new HashSet<string> { "A", "B", "C" };
        }
        """;

        var expected = Verify.Diagnostic().WithLocation(0).WithArguments("Data");
        await Verify.VerifyAnalyzerAsync(testCode, expected);
    }
}
