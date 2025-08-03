using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ToListinator.Tests;

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
        [StringSyntax("c#-test")] string fixedSource,
        int codeActionIndex = 0
    )
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var csTest = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            CodeActionIndex = codeActionIndex,
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

        // Add .NET 8 reference assemblies to support FrozenSet and other newer types
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

        // Add .NET 8 reference assemblies to support FrozenSet and other newer types
        test.TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        return test;
    }
}
