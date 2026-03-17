using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace DateTimeDetector.Tests;

public static class TestHelper
{
    private const string EditorConfig = """
        root = true

        [*.cs]
        charset = utf-8
        end_of_line = lf
        indent_style = space
        insert_final_newline = true
        indent_size = 4
        """;

    public static CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> CreateCodeFixTest<TAnalyzer, TCodeFix>(
        [StringSyntax("c#-test")] string inputSource,
        [StringSyntax("c#-test")] string fixedSource
    )
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var csTest = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestState =
            {
                Sources = { inputSource },
                AnalyzerConfigFiles =
                {
                    { ("/.editorconfig", EditorConfig) },
                },
            },
            FixedState =
            {
                MarkupHandling = MarkupMode.IgnoreFixable,
                Sources = { fixedSource },
            },
        };

        csTest.TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        csTest.FixedState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        return csTest;
    }

    public static CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> CreateAnalyzerTest<TAnalyzer>(
        [StringSyntax("c#-test")] string source
    )
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source },
                AnalyzerConfigFiles =
                {
                    { ("/.editorconfig", EditorConfig) },
                },
            },
        };

        test.TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        return test;
    }

    public static CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> CreateAnalyzerTest<TAnalyzer>(
        [StringSyntax("c#-test")] string source,
        DiagnosticResult expectedDiagnostic
    )
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = CreateAnalyzerTest<TAnalyzer>(source);
        test.ExpectedDiagnostics.Add(expectedDiagnostic);
        return test;
    }

    public static DiagnosticResult CreateDiagnostic(string diagnosticId)
    {
        return new DiagnosticResult(diagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
    }
}
