using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;
using Xunit;

namespace ToListinator.Tests;

public class SingleElementAccessAnalyzerTests
{
    [Fact]
    public async Task DetectsToListFirst()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var first = {|TL008:items.ToList().First()|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToArrayLast()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var last = {|TL008:items.ToArray().Last()|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToListFirstOrDefault()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var first = {|TL008:items.ToList().FirstOrDefault()|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToArrayLastOrDefault()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var last = {|TL008:items.ToArray().LastOrDefault()|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToListSingle()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1 };
                    var single = {|TL008:items.ToList().Single()|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToArraySingleOrDefault()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1 };
                    var single = {|TL008:items.ToArray().SingleOrDefault()|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToListElementAt()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var element = {|TL008:items.ToList().ElementAt(1)|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToArrayElementAtOrDefault()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var element = {|TL008:items.ToArray().ElementAtOrDefault(1)|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToListFirstWithPredicate()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var first = {|TL008:items.ToList().First(x => x > 1)|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsChainedWithToListFirst()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var first = {|TL008:items.Where(x => x > 0).ToList().First()|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToListIndexer()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var element = {|TL008:items.ToList()[0]|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToArrayIndexer()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var element = {|TL008:items.ToArray()[0]|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DetectsToListIndexerWithVariable()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    int index = 1;
                    var element = {|TL008:items.ToList()[index]|};
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotDetectDirectFirst()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var first = items.First();
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotDetectToListWithoutElementAccess()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var list = items.ToList();
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotDetectToListWhere()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var filtered = items.ToList().Where(x => x > 0);
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotDetectDirectIndexer()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var list = new List<int> { 1, 2, 3 };
                    var element = list[0];
                }
            }
            """;

        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DoesNotDetectToListForEach()
    {
        const string testCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    items.ToList().ForEach(x => Console.WriteLine(x));
                }
            }
            """;

        // TL001 handles this, TL008 should not fire
        var test = TestHelper.CreateAnalyzerTest<SingleElementAccessAnalyzer>(testCode);
        await test.RunAsync(CancellationToken.None);
    }
}
