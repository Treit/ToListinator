using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;
using ToListinator.CodeFixes;
using Xunit;

namespace ToListinator.Tests;

public class SingleElementAccessCodeFixTests
{
    [Fact]
    public async Task FixesToListFirst()
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

        const string fixedCode = """
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

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToArrayLast()
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

        const string fixedCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var last = items.Last();
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToListFirstWithPredicate()
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

        const string fixedCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var first = items.First(x => x > 1);
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesChainedToListFirst()
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

        const string fixedCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var first = items.Where(x => x > 0).First();
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToListIndexerWithElementAt()
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

        const string fixedCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var element = items.ElementAt(0);
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToArrayIndexerWithElementAt()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var element = {|TL008:items.ToArray()[2]|};
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
                    var items = new[] { 1, 2, 3 };
                    var element = items.ElementAt(2);
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesIndexerWithVariableIndex()
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

        const string fixedCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    int index = 1;
                    var element = items.ElementAt(index);
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PreservesLeadingComment()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    // Get the first item
                    var first = {|TL008:items.ToList().First()|};
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
                    var items = new[] { 1, 2, 3 };
                    // Get the first item
                    var first = items.First();
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToListSingleOrDefault()
    {
        const string testCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1 };
                    var single = {|TL008:items.ToList().SingleOrDefault()|};
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
                    var items = new[] { 1 };
                    var single = items.SingleOrDefault();
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixesToListElementAt()
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

        const string fixedCode = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var element = items.ElementAt(1);
                }
            }
            """;

        var test = TestHelper.CreateCodeFixTest<SingleElementAccessAnalyzer, SingleElementAccessCodeFixProvider>(
            testCode, fixedCode);
        await test.RunAsync(CancellationToken.None);
    }
}
