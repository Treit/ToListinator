using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Xunit;
using ToListinator;

public class ToListinatorAnalyzerTests
{
    [Fact]
    public async Task NoDiagnosticsForEmptyCode()
    {
        var test = new CSharpAnalyzerTest<ToListinatorAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = ""
        };

        await test.RunAsync();
    }
}
