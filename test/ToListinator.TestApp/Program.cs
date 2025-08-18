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
        IEnumerable<int> numbersEnumerable = numbers;

        // This should trigger an analyzer warning.
        var notany = numbers.ToList().Count <= 0;

        // This should trigger an analyzer warning.
        var any = numbers.ToList().Count > 0;

        // This should trigger an analyzer warning
        numbers.ToList().ForEach(x => Console.WriteLine($"Number: {x}"));

        // This should also trigger an analyzer warning
        numbers.Where(x => x > 2).ToList().ForEach(x => Console.WriteLine($"Filtered: {x}"));

        numbers.Where(x => x > 2).ToList().ForEach(Print);

        // TL009 test cases - these should trigger Count comparison analyzer
        if (numbers.Count() > 0) // Should trigger TL009
            Console.WriteLine("Has items");

        if (numbersEnumerable.Where(x => x > 2).Count() >= 1) // Should trigger TL009
            Console.WriteLine("Has filtered items");

        if (numbersEnumerable.Count() != 0) // Should trigger TL009
            Console.WriteLine("Not empty");

        if (0 < numbersEnumerable.Count()) // Should trigger TL009
            Console.WriteLine("Reversed comparison");

        if (numbersEnumerable.Count() == 0) // Should trigger TL009
            Console.WriteLine("Is empty");

        // This should NOT trigger TL009 (Count with predicate - handled by TL006)
        if (numbersEnumerable.Count(x => x > 3) > 0) // Should NOT trigger TL009
            Console.WriteLine("Count with predicate");

        // This should NOT trigger TL009 (List.Count property)
        var list2 = new List<int> { 1, 2, 3 };
        if (list2.Count > 0) // Should NOT trigger TL009
            Console.WriteLine("List.Count property");

        // This should NOT trigger TL009 (other comparisons)
        if (numbers.Count() > 2) // Should NOT trigger TL009
            Console.WriteLine("More than 2");

        // This should NOT trigger the analyzer warning (regular foreach)
        foreach (var number in numbers)
        {
            Console.WriteLine($"Regular foreach: {number}");
        }

        // This should NOT trigger the analyzer warning (ToList without ForEach)
        var list = numbers.ToList();
        Console.WriteLine($"List count: {list.Count}");

        Console.WriteLine("Done!");
    }

    static void Print<T>(T item)
    {
        Console.WriteLine(item);
    }
}
