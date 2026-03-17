# DateTimeDetector

A Roslyn analyzer that detects usage of `System.DateTime` and suggests replacing it with `System.DateTimeOffset`.

## Why?

`DateTime` does not store time zone information, which can lead to subtle bugs — especially in distributed systems, serialization, and database interactions. `DateTimeOffset` preserves the offset from UTC and is generally the safer default choice.

See:
- [Microsoft: Choosing between DateTime, DateTimeOffset, TimeSpan, and TimeZoneInfo](https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime)
- [The case for DateTimeOffset](https://stackoverflow.com/questions/4331189/datetime-vs-datetimeoffset)

## Installation

```shell
dotnet add package DateTimeDetector
```

## Rules

| Rule ID | Category    | Severity | Description                              |
|---------|-------------|----------|------------------------------------------|
| DT001   | Reliability | Warning  | Prefer DateTimeOffset over DateTime      |

## Examples

```csharp
// ❌ Flagged by DT001
DateTime now = DateTime.Now;
DateTime created = new DateTime();
List<DateTime> timestamps = new();

// ✅ After applying the code fix
DateTimeOffset now = DateTimeOffset.Now;
DateTimeOffset created = new DateTimeOffset();
List<DateTimeOffset> timestamps = new();
```

## What gets detected

The analyzer flags any reference to `System.DateTime` including:

- **Variable declarations**: `DateTime x = ...`
- **Parameters**: `void Method(DateTime value)`
- **Return types**: `DateTime GetTime()`
- **Properties and fields**: `DateTime Created { get; set; }`
- **Static member access**: `DateTime.Now`, `DateTime.UtcNow`
- **Object creation**: `new DateTime()`
- **Generic type arguments**: `List<DateTime>`

Custom types named `DateTime` in other namespaces are **not** flagged — only `System.DateTime`.

## Limitations

- Constructor argument signatures differ between `DateTime` and `DateTimeOffset` (e.g., `new DateTime(2024, 1, 1)` has no direct `DateTimeOffset` equivalent without an offset parameter). The analyzer will flag these, but the code fix may produce code that needs manual adjustment.
- Some `DateTime`-specific members (e.g., `DateTime.Today`) have no `DateTimeOffset` equivalent. The fix replaces the type name but you may need to adjust member access.

## Suppressing the diagnostic

If you intentionally need `DateTime` (e.g., for interop), suppress per-site:

```csharp
#pragma warning disable DT001
DateTime required = SomeLegacyApi();
#pragma warning restore DT001
```

Or in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.DT001.severity = none
```
