using System;
using System.Collections.Generic;
using System.Linq;

namespace ToListinator.TestApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Testing ToListinator analyzer...");

        var numbers = new[] { 1, 2, 3, 4, 5, 6 };

        // This should trigger TL007 - unnecessary ToList() in method chain
        var items = numbers.ToList().Select(x => x * 2).ToList();

        // This should trigger TL007 - unnecessary ToArray() in method chain
        var moreItems = numbers.ToArray().Where(x => x > 3).ToList();

        // This should trigger an analyzer warning.
        var notany = numbers.ToList().Count <= 0;

        // This should trigger an analyzer warning.
        var any = numbers.ToList().Count > 0;

        // This should trigger an analyzer warning
        numbers.ToList().ForEach(x => Console.WriteLine($"Number: {x}"));

        // This should also trigger an analyzer warning
        numbers.Where(x => x > 2).ToList().ForEach(x => Console.WriteLine($"Filtered: {x}"));

        numbers.Where(x => x > 2).ToList().ForEach(Print);

        // This should NOT trigger the analyzer warning (regular foreach)
        foreach (var number in numbers)
        {
            Console.WriteLine($"Regular foreach: {number}");
        }

        // This should NOT trigger the analyzer warning (ToList without ForEach)
        var list = numbers.ToList();
        Console.WriteLine($"List count: {list.Count}");

        // TL010 Test Cases - Unnecessary ToList() on already materialized collections
        TestTL010();

        Console.WriteLine("Done!");
    }

    static void TestTL010()
    {
        Console.WriteLine("\n--- Testing TL010: Unnecessary ToList() on materialized collections ---");

        // Case 1: List<T> -> ToList() -> LINQ operation (should trigger TL010)
        List<int> numberList = new List<int> { 1, 2, 3, 4, 5 };
        var result1 = numberList.ToList().Where(x => x > 2);

        // Case 2: Array -> ToList() -> LINQ operation (should trigger TL010)
        int[] numberArray = { 1, 2, 3, 4, 5 };
        var result2 = numberArray.ToList().Select(x => x * 2);

        // Case 3: HashSet -> ToList() -> LINQ operation (should trigger TL010)
        HashSet<string> stringSet = new HashSet<string> { "a", "b", "c" };
        var result3 = stringSet.ToList().OrderBy(x => x);

        // Case 4: IList -> ToList() -> LINQ operation (should trigger TL010)
        IList<double> valueList = new List<double> { 1.0, 2.0, 3.0 };
        var hasAny = valueList.ToList().Any(x => x > 1.5);

        // Case 5: Method parameter accepting IEnumerable (should trigger TL010)
        ProcessItems(numberList.ToList());

        // Case 6: Property access -> ToList() -> LINQ (should trigger TL010)
        var data = new TestData { Items = new List<string> { "x", "y", "z" } };
        var orderedItems = data.Items.ToList().OrderByDescending(x => x);

        // These should NOT trigger TL010 (correct usage):

        // Query result -> ToList() -> LINQ (should NOT trigger - this is fine)
        var query = numberList.Where(x => x > 0);
        var queryResult = query.ToList().Select(x => x * 3);

        // IEnumerable result -> ToList() -> LINQ (should NOT trigger - this is fine)
        IEnumerable<int> enumerable = GetEnumerable();
        var enumerableResult = enumerable.ToList().Where(x => x < 10);

        Console.WriteLine($"TL010 test cases completed. Results count: {result1.Count()}, {result2.Count()}, {result3.Count()}");
        Console.WriteLine($"Has any: {hasAny}, Ordered items: {orderedItems.Count()}");
        Console.WriteLine($"Query result: {queryResult.Count()}, Enumerable result: {enumerableResult.Count()}");

        var x = orderedItems.Where(x => x.Length > 0).Count();

        // Create mock response object to test analyzer
        var response = new MockResponse
        {
            LogData = new MockLogData
            {
                ErrorLogData = new List<MockErrorLogEntry>
                {
                    new MockErrorLogEntry { ErrorMessage = "Using mock ranker results for testing" },
                    new MockErrorLogEntry { ErrorMessage = "Another error message" },
                    new MockErrorLogEntry { ErrorMessage = "Using mock ranker results again" }
                }
            }
        };

        Test(response.LogData.ErrorLogData.Where(e => e.ErrorMessage.Contains("Using mock ranker results")).Count());
    }

    static void Test(int j)
    {
    }

    static void ProcessItems(IEnumerable<int> items)
    {
        foreach (var item in items.Take(3))
        {
            Console.WriteLine($"Processing: {item}");
        }
    }

    static IEnumerable<int> GetEnumerable()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return i;
        }
    }

    class TestData
    {
        public List<string> Items { get; set; } = new List<string>();
    }

    static void Print<T>(T item)
    {
        Console.WriteLine(item);
    }

    // Mock types to support the test case
    class MockResponse
    {
        public MockLogData LogData { get; set; } = new MockLogData();
    }

    class MockLogData
    {
        public List<MockErrorLogEntry> ErrorLogData { get; set; } = new List<MockErrorLogEntry>();
    }

    class MockErrorLogEntry
    {
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
