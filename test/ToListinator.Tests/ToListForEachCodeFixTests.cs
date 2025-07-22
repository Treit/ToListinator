using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ToListinator;
using ToListinator.CodeFixes;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ToListinator.ToListForEachAnalyzer,
    ToListinator.CodeFixes.ToListForEachCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

public class ToListForEachCodeFixTests
{
    [Fact]
    public async Task ShouldReportWarningForToListForEach()
    {
        var testCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var list = new List<int> { 1, 2, 3 };
                list.ToList().ForEach(x => Console.WriteLine(x));
            }
        }
        """;

        var fixedCode =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var list = new List<int> { 1, 2, 3 };
                list.ToList().ForEach(x => Console.WriteLine(x));
            }
        }// CodeFix was here
        """;
        var expected = Verify.Diagnostic().WithLocation(10, 9);

        await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
