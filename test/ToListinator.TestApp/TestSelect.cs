using System;
using System.Linq;

public class TestCode
{
    public void Test()
    {
        var numbers = new[] { 1, 2, 3 };
        numbers.Where(x => x > 1).ToList().ForEach(Console.Write);
        numbers.Where(x => x > 2).ToList().ForEach(Print);
    }

    void Print(int x)
    {
        Console.WriteLine(x);
    }
}
