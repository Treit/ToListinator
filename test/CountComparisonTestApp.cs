using System;
using System.Collections.Generic;
using System.Linq;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };

            // These should trigger TL009 analyzer
            if (numbers.Count() > 0)
                Console.WriteLine("Has items (should trigger TL009)");

            if (numbers.Where(x => x > 2).Count() >= 1)
                Console.WriteLine("Has items >= 1 (should trigger TL009)");

            if (numbers.Count() != 0)
                Console.WriteLine("Not empty (should trigger TL009)");

            if (0 < numbers.Count())
                Console.WriteLine("Reversed comparison (should trigger TL009)");

            if (numbers.Count() == 0)
                Console.WriteLine("Is empty (should trigger TL009)");

            // These should NOT trigger TL009
            if (numbers.Count() > 2)
                Console.WriteLine("More than 2 (should NOT trigger)");

            if (numbers.Count(x => x > 3) > 0)
                Console.WriteLine("Count with predicate (should be handled by TL006)");

            var list = new List<int> { 1, 2, 3 };
            if (list.Count > 0)
                Console.WriteLine("List.Count property (should NOT trigger)");
        }
    }
}
