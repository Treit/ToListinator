using System;

namespace ToListinator.TestApp;

/// <summary>
/// Test cases for the DateTimeDetector analyzer (DT001).
/// All DateTime usages below should trigger DT001 warnings suggesting DateTimeOffset.
/// </summary>
static class TestDateTime
{
    // Should trigger DT001: DateTime field
    private static DateTime _created;

    // Should trigger DT001: DateTime property
    public static DateTime Timestamp { get; set; }

    // Should trigger DT001: DateTime return type and DateTime.Now
    static DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }

    // Should trigger DT001: DateTime parameter
    static void ProcessTime(DateTime value)
    {
        Console.WriteLine($"Time: {value}");
    }

    // Should trigger DT001: DateTime local variable and DateTime.UtcNow
    static void TestLocalVariables()
    {
        DateTime now = DateTime.Now;
        DateTime utcNow = DateTime.UtcNow;
        var today = DateTime.Today;
        Console.WriteLine($"Now: {now}, UTC: {utcNow}, Today: {today}");
    }

    // Should trigger DT001: new DateTime()
    static void TestConstructor()
    {
        var dt = new DateTime();
        Console.WriteLine($"Default: {dt}");
    }

    // Should trigger DT001: DateTime in generic type argument
    static void TestGenericUsage()
    {
        var list = new System.Collections.Generic.List<DateTime>();
        list.Add(DateTime.Now);
        Console.WriteLine($"Count: {list.Count}");
    }

    // Should NOT trigger DT001: DateTimeOffset usage is fine
    static void TestDateTimeOffset()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        Console.WriteLine($"Offset now: {now}, UTC: {utcNow}");
    }

    public static void RunAll()
    {
        Console.WriteLine("\n--- Testing DT001: DateTime usage detection ---");
        _created = DateTime.Now;
        Timestamp = DateTime.Now;
        var time = GetCurrentTime();
        ProcessTime(time);
        TestLocalVariables();
        TestConstructor();
        TestGenericUsage();
        TestDateTimeOffset();
        Console.WriteLine($"Created: {_created}");
    }
}
