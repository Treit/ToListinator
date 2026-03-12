using System;
using System.Collections.Generic;
using System.Linq;

namespace ToListinator.TestApp;

internal class TestSingleElementAccess
{
    public void TestMethod()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        // These should trigger TL008 - unnecessary materialization before single element access

        // Method patterns
        var first = numbers.ToList().First();
        var last = numbers.ToArray().Last();
        var single = numbers.Where(x => x > 4).ToList().Single();
        var firstOrDefault = numbers.ToList().FirstOrDefault(x => x > 3);
        var lastOrDefault = numbers.ToArray().LastOrDefault();
        var singleOrDefault = numbers.ToList().SingleOrDefault();
        var elementAt = numbers.ToList().ElementAt(2);

        // Indexer patterns
        var indexed = numbers.ToList()[0];
        var arrayIndexed = numbers.ToArray()[1];
        var variableIndex = 3;
        var indexedVar = numbers.ToList()[variableIndex];

        // Chained patterns
        var chained = numbers
            .Where(x => x > 1)
            .ToList()
            .First();

        Console.WriteLine($"Results: {first}, {last}, {single}, {firstOrDefault}");
        Console.WriteLine($"Results: {lastOrDefault}, {singleOrDefault}, {elementAt}");
        Console.WriteLine($"Indexed: {indexed}, {arrayIndexed}, {indexedVar}");
        Console.WriteLine($"Chained: {chained}");

        // These should NOT trigger TL008

        // No materialization
        var okFirst = numbers.First();

        // Materialization result stored in variable
        var list = numbers.ToList();
        var okIndexed = list[0];

        // Non-single-element access after materialization
        var count = numbers.ToList().Count;
    }
}
