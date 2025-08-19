using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;
using ToListinator.CodeFixes;
using Xunit;

namespace ToListinator.Tests;

public class UnnecessaryToListCodeFixTests
{
    [Fact]
    public async Task ShouldFixListToListWithLinqOperation()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                var result = {|TL010:numbers.ToList()|}.Where(x => x > 0);
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                var result = numbers.Where(x => x > 0);
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixArrayToListWithLinqOperation()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                int[] numbers = { 1, 2, 3 };
                var result = {|TL010:numbers.ToList()|}.Select(x => x * 2);
            }
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                int[] numbers = { 1, 2, 3 };
                var result = numbers.Select(x => x * 2);
            }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixChainedLinqOperations()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                var result = {|TL010:numbers.ToList()|}.Where(x => x > 0).Select(x => x * 2).OrderBy(x => x);
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                var result = numbers.Where(x => x > 0).Select(x => x * 2).OrderBy(x => x);
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixPassedToMethodAcceptingIEnumerable()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                ProcessItems({|TL010:numbers.ToList()|});
            }

            private void ProcessItems(IEnumerable<int> items)
            {
                // Do something with items
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                ProcessItems(numbers);
            }

            private void ProcessItems(IEnumerable<int> items)
            {
                // Do something with items
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldPreserveTriviaAndComments()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                // Convert to list first
                var result = {|TL010:numbers.ToList()|}.Where(x => x > 0); // Filter positive
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                // Convert to list first
                var result = numbers.Where(x => x > 0); // Filter positive
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixHashSetToListWithLinqOperation()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                HashSet<string> items = GetItems();
                var result = {|TL010:items.ToList()|}.OrderBy(x => x);
            }

            private HashSet<string> GetItems() => new HashSet<string> { "a", "b", "c" };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                HashSet<string> items = GetItems();
                var result = items.OrderBy(x => x);
            }

            private HashSet<string> GetItems() => new HashSet<string> { "a", "b", "c" };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixIListToListWithLinqOperation()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                IList<int> numbers = GetNumbers();
                var result = {|TL010:numbers.ToList()|}.Count(x => x > 0);
            }

            private IList<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                IList<int> numbers = GetNumbers();
                var result = numbers.Count(x => x > 0);
            }

            private IList<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixWithComplexExpression()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var data = GetComplexData();
                var result = {|TL010:data.Items.ToList()|}.Where(x => x.Value > 0).Select(x => x.Value);
            }

            private ComplexData GetComplexData() => new ComplexData();
        }

        class ComplexData
        {
            public List<Item> Items { get; set; } = new List<Item>();
        }

        class Item
        {
            public int Value { get; set; }
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var data = GetComplexData();
                var result = data.Items.Where(x => x.Value > 0).Select(x => x.Value);
            }

            private ComplexData GetComplexData() => new ComplexData();
        }

        class ComplexData
        {
            public List<Item> Items { get; set; } = new List<Item>();
        }

        class Item
        {
            public int Value { get; set; }
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixMultilineFluentChain()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                var result = {|TL010:numbers.ToList()|}
                    .Where(x => x > 0)
                    .Select(x => x * 2)
                    .OrderBy(x => x);
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                var result = numbers
                    .Where(x => x > 0)
                    .Select(x => x * 2)
                    .OrderBy(x => x);
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldFixCollectionToListWithAggregateMethod()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Collections.ObjectModel;
        using System.Linq;

        class C
        {
            void M()
            {
                Collection<string> items = GetItems();
                var result = {|TL010:items.ToList()|}.Any(x => x.Length > 0);
            }

            private Collection<string> GetItems() => new Collection<string> { "a", "b", "c" };
        }
        """;

        const string fixedCode = """
        using System.Collections.Generic;
        using System.Collections.ObjectModel;
        using System.Linq;

        class C
        {
            void M()
            {
                Collection<string> items = GetItems();
                var result = items.Any(x => x.Length > 0);
            }

            private Collection<string> GetItems() => new Collection<string> { "a", "b", "c" };
        }
        """;

        var test = TestHelper.CreateCodeFixTest<UnnecessaryToListAnalyzer, UnnecessaryToListCodeFixProvider>(
            testCode,
            fixedCode
        );

        await test.RunAsync(CancellationToken.None);
    }
}
