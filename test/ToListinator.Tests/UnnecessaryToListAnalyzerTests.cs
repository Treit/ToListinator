using Microsoft.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;
using Xunit;

namespace ToListinator.Tests;

public class UnnecessaryToListAnalyzerTests
{
    [Fact]
    public async Task ShouldReportWarningForListToListWithLinqOperation()
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

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForArrayToListWithLinqOperation()
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

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForHashSetToListWithLinqOperation()
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

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForIListToListWithLinqOperation()
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

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForChainedLinqOperations()
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

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForPassedToMethodAcceptingIEnumerable()
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

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForIEnumerableToList()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                IEnumerable<int> numbers = GetNumbers();
                var result = numbers.ToList().Where(x => x > 0);
            }

            private IEnumerable<int> GetNumbers() => Enumerable.Range(1, 10);
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForQueryToList()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                var numbers = new[] { 1, 2, 3 };
                var query = numbers.Where(x => x > 0);
                var result = query.ToList().Select(x => x * 2);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForToListWithoutLinqOperations()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                var copy = numbers.ToList(); // Creating a copy for other reasons
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldNotReportWarningForToListWithArguments()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                List<int> numbers = GetNumbers();
                var result = numbers.Where(x => x > 0).ToList(); // This is fine, normal ToList() usage
            }

            private List<int> GetNumbers() => new List<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForCollectionToList()
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

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForObservableCollectionToList()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Collections.ObjectModel;
        using System.Linq;

        class C
        {
            void M()
            {
                ObservableCollection<int> items = GetItems();
                var result = {|TL010:items.ToList()|}.First();
            }

            private ObservableCollection<int> GetItems() => new ObservableCollection<int> { 1, 2, 3 };
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForSortedSetToList()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                SortedSet<double> values = GetValues();
                var result = {|TL010:values.ToList()|}.Max();
            }

            private SortedSet<double> GetValues() => new SortedSet<double> { 1.0, 2.0, 3.0 };
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldReportWarningForMultiDimensionalArray()
    {
        const string testCode = """
        using System.Collections.Generic;
        using System.Linq;

        class C
        {
            void M()
            {
                int[,] matrix = { { 1, 2 }, { 3, 4 } };
                var flattened = matrix.Cast<int>();
                var list = flattened.ToArray(); // This is fine, different pattern

                int[] array = { 1, 2, 3 };
                var result = {|TL010:array.ToList()|}.Where(x => x > 0);
            }
        }
        """;

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShouldHandleComplexNestedExpressions()
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

        var test = TestHelper.CreateAnalyzerTest<UnnecessaryToListAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }
}
