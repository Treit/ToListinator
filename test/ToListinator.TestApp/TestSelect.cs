using System;
using System.Linq;

public class TestCode
{
    public void Test()
    {
        var numbers = new[] { 1, 2, 3 };
        numbers.Where(x => x > 2).ToList().ForEach(Print);
        numbers.Where(x => x > 1).ToList().ForEach(Console.Write);

        var selected = numbers.Select(x => x).Where(x => x > 1);

        // Print the numbers!
        foreach (var num in numbers ?? Array.Empty<int>()) // Will only run if numbers is not null
        {
            // Some comment about the loop
            Print(num); // Output!
        }

    }

    void Print(int x)
    {
        Console.WriteLine(x);
    }
}
