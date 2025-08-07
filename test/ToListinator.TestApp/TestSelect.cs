using System;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Linq;

public class TestCode
{
    static HashSet<string>? _data = null;
    public static HashSet<string> Data => new HashSet<string> { "A", "B", "C" };

    public static HashSet<string> Data2 => _data ??= new HashSet<string> { "A", "B", "C" };

    // Test with ReadOnlySet
    public static ReadOnlySet<string> Data3 => new ReadOnlySet<string>(Data2);

    // Test with FrozenSet
    public static FrozenSet<string> Data4 => _data!.ToFrozenSet();

    public void Test()
    {
        var numbers = new[] { 1, 2, 3 };
        numbers.Where(x => x > 2).ToList().ForEach(Print);
        numbers.Where(x => x > 1).ToList().ForEach(Console.Write);

        var selected = numbers.Select(x => x).Where(x => x > 1);

        var count = numbers
            .Where(x => x > 1)
            .Where(x => x < 3)
            .Count();

        // Print the numbers!
        foreach (var num in numbers ?? Array.Empty<int>()) // Will only run if numbers is not null
        {
            // Some comment about the loop
            Print(num); // Output!
        }

        void Print(int x)
        {
            Console.WriteLine(x);
        }
    }
}
