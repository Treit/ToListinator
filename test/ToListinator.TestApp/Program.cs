using System;
using System.Collections.Generic;
using System.Linq;

namespace ToListinator.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing ToListinator analyzer...");

            var numbers = new[] { 1, 2, 3, 4, 5, 6 };

            // This should trigger the analyzer warning
            numbers.ToList().ForEach(x => Console.WriteLine($"Number: {x}"));

            // This should also trigger the analyzer warning
            numbers.Where(x => x > 2).ToList().ForEach(x => Console.WriteLine($"Filtered: {x}"));

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
    }
}
