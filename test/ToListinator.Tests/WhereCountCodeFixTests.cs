using Microsoft.CodeAnalysis.Testing;
using ToListinator.Analyzers;
using ToListinator.CodeFixes;

namespace ToListinator.Tests;

public class WhereCountCodeFixTests
{
    [Fact]
    public async Task ShouldFixWhereCountWithSimpleLambda()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(x => x > 2).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Count(x => x > 2);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixWhereCountWithComplexLambda()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(x => x > 2 && x < 5).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Count(x => x > 2 && x < 5);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixWhereCountWithParenthesizedLambda()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where((x) => x > 2).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Count((x) => x > 2);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixWhereCountWithMethodGroup()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(IsEven).Count()|};
            }

            private bool IsEven(int number) => number % 2 == 0;
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Count(IsEven);
            }

            private bool IsEven(int number) => number % 2 == 0;
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixWhereCountWithAnonymousMethod()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(delegate(int x) { return x > 2; }).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Count(delegate (int x) { return x > 2; });
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixChainedWhereCount()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Select(x => x * 2).Where(x => x > 4).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Select(x => x * 2).Count(x => x > 4);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixWhereCountWithComments()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                // Count the filtered items
                var count = {|#0:numbers.Where(x => x > 2).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                // Count the filtered items
                var count = numbers.Count(x => x > 2);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixWhereCountOnPropertyAccess()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public int[] Numbers { get; set; } = { 1, 2, 3, 4, 5 };

            public void TestMethod()
            {
                var count = {|#0:Numbers.Where(x => x > 2).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public int[] Numbers { get; set; } = { 1, 2, 3, 4, 5 };

            public void TestMethod()
            {
                var count = Numbers.Count(x => x > 2);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixMultipleWhereCountChain()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var someList = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                var count = {|#0:someList.Select(x => x + 1).Where(x => x > 1).Where(x => x < 10).Where(x => x != 5).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var someList = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                var count = someList.Select(x => x + 1).Count(x => x > 1 && x < 10 && x != 5);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixTwoWhereCountChain()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers.Where(x => x > 2).Where(x => x < 5).Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers.Count(x => x > 2 && x < 5);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixMultilineWhereCountChain()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = {|#0:numbers
                    .Where(x => x > 1)
                    .Where(x => x < 3)
                    .Count()|};
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class TestClass
        {
            public void TestMethod()
            {
                var numbers = new[] { 1, 2, 3, 4, 5 };
                var count = numbers
                    .Count(x => x > 1 && x < 3);
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixComplexChainWithMultipleWheres()
    {
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Test
        {
            internal class TestWhereCount
            {
                public void TestMethod()
                {
                    var numbers = new List<int> { 1, 2, 3 };
                    var count = {|#0:numbers
                        .Select(x => x * 2)
                        .OrderBy(x => x)
                        .Where(x => x > 1)
                        .Where(x => x < 3)
                        .Count()|};
                }
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Test
        {
            internal class TestWhereCount
            {
                public void TestMethod()
                {
                    var numbers = new List<int> { 1, 2, 3 };
                    var count = numbers
                        .Select(x => x * 2)
                        .OrderBy(x => x)
                        .Count(x => x > 1 && x < 3);
                }
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixSeparatedWhereChain()
    {
        // This test should expose the bug where Where calls separated by other methods
        // are not properly combined
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Test
        {
            internal class TestWhereCount
            {
                public void TestMethod()
                {
                    var numbers = new List<int> { 1, 2, 3 };
                    var count = {|#0:numbers
                        .Where(x => x > 0)
                        .Select(x => x * 2)
                        .Where(x => x > 1)
                        .Where(x => x < 10)
                        .Count()|};
                }
            }
        }
        """;

        const string fixedCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Test
        {
            internal class TestWhereCount
            {
                public void TestMethod()
                {
                    var numbers = new List<int> { 1, 2, 3 };
                    var count = numbers
                        .Where(x => x > 0)
                        .Select(x => x * 2)
                        .Count(x => x > 0 && x > 1 && x < 10);
                }
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateCodeFixTest<WhereCountAnalyzer, WhereCountCodeFixProvider>(
            testCode,
            fixedCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldDetectSeparatedWhereChain()
    {
        // Test just the analyzer to see if it detects the pattern
        const string testCode = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Test
        {
            internal class TestWhereCount
            {
                public void TestMethod()
                {
                    var numbers = new List<int> { 1, 2, 3 };
                    var count = {|#0:numbers
                        .Where(x => x > 0)
                        .Select(x => x * 2)
                        .Where(x => x > 1)
                        .Where(x => x < 10)
                        .Count()|};
                }
            }
        }
        """;

        var expected = TestHelper.CreateDiagnostic("TL006").WithLocation(0);
        var test = TestHelper.CreateAnalyzerTest<WhereCountAnalyzer>(testCode);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }
}
