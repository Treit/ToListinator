using ToListinator.Analyzers;
using ToListinator.CodeFixes;

namespace ToListinator.Tests;

public class ArrayEmptyCodeFixTests
{
    [Fact]
    public async Task ShouldFixEmptyStringArrayWithExplicitSize()
    {
        var testCode = """
        using System;

        class C
        {
            void M()
            {
                var array = {|TL006:new string[0]|};
            }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                var array = Array.Empty<string>();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ArrayEmptyAnalyzer, ArrayEmptyCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixEmptyStringArrayWithEmptyInitializer()
    {
        var testCode = """
        using System;

        class C
        {
            void M()
            {
                var array = {|TL006:new string[] {}|};
            }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                var array = Array.Empty<string>();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ArrayEmptyAnalyzer, ArrayEmptyCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixEmptyIntArray()
    {
        var testCode = """
        using System;

        class C
        {
            void M()
            {
                var array = {|TL006:new int[0]|};
            }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                var array = Array.Empty<int>();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ArrayEmptyAnalyzer, ArrayEmptyCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixEmptyCustomTypeArray()
    {
        var testCode = """
        using System;

        public class TestClass
        {
        }

        class C
        {
            void M()
            {
                var array = {|TL006:new TestClass[0]|};
            }
        }
        """;

        var fixedCode = """
        using System;

        public class TestClass
        {
        }

        class C
        {
            void M()
            {
                var array = Array.Empty<TestClass>();
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ArrayEmptyAnalyzer, ArrayEmptyCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldPreserveComments()
    {
        var testCode = """
        using System;

        class C
        {
            void M()
            {
                // This is a comment
                var array = {|TL006:new string[0]|}; // Another comment
            }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                // This is a comment
                var array = Array.Empty<string>(); // Another comment
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ArrayEmptyAnalyzer, ArrayEmptyCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixInMethodParameter()
    {
        var testCode = """
        using System;

        class C
        {
            void M()
            {
                SomeMethod({|TL006:new string[0]|});
            }
            
            void SomeMethod(string[] array) { }
        }
        """;

        var fixedCode = """
        using System;

        class C
        {
            void M()
            {
                SomeMethod(Array.Empty<string>());
            }
            
            void SomeMethod(string[] array) { }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<ArrayEmptyAnalyzer, ArrayEmptyCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }
}