# ToListinator
[The tragedy of ToList](https://mtreit.com/programming,/.net/2024/07/30/ToList.html)

As the blog post discusses, it is very common for C# programmers to waste resources by allocating lists unnecessarily. The canonical example is using `ToList().ForEeach(...)` solely because the `List<T>` type happens to have a `ForEach` method on it.

ToListinator is a Roslyn code analyzer designed to track down and help eliminate these kinds of unnecessary ToList calls with extreme prejudice, among other things.

## Available Analyzers

### TL001 - ToList().ForEach Detection
**Category:** Performance | **Severity:** Warning

Detects the pattern `collection.ToList().ForEach(action)` which unnecessarily allocates a List just to call the ForEach method. This pattern should be replaced with a regular foreach loop.

**Example:**
```csharp
// ❌ Bad - unnecessary List allocation
items.Where(x => x.IsValid).ToList().ForEach(ProcessItem);

// ✅ Good - use foreach loop instead
foreach (var item in items.Where(x => x.IsValid))
{
    ProcessItem(item);
}
```

### TL002 - Identity Select Detection
**Category:** Performance | **Severity:** Warning

Detects useless `Select(x => x)` identity operations that perform no transformation and should be removed.

**Example:**
```csharp
// ❌ Bad - pointless identity transformation
var result = items.Where(x => x.IsValid).Select(x => x).ToList();

// ✅ Good - remove unnecessary Select
var result = items.Where(x => x.IsValid).ToList();
```

### TL003 - ToList().Count Comparison Detection
**Category:** Performance | **Severity:** Warning

Detects `ToList().Count` comparisons used for existence checks and suggests using `Any()` instead to avoid unnecessary allocation.

**Example:**
```csharp
// ❌ Bad - unnecessary List allocation for existence check
if (items.Where(x => x.IsValid).ToList().Count > 0)

// ✅ Good - use Any() for existence checks
if (items.Where(x => x.IsValid).Any())
```

### TL004 - Null Coalescing Foreach Detection
**Category:** Performance | **Severity:** Warning

Detects the pattern `foreach (var item in collection ?? new Collection())` which creates unnecessary allocations. Suggests using null checks instead.

**Example:**
```csharp
// ❌ Bad - creates empty collection on every iteration when null
foreach (var item in items ?? new List<string>())

// ✅ Good - check for null to avoid allocation
if (items != null)
{
    foreach (var item in items)
    {
        // process item
    }
}
```

### TL005 - Static Property Expression Body Detection
**Category:** Performance | **Severity:** Warning

Detects static properties with expression bodies that may allocate new instances on every access. Suggests using getter-only properties with initializers instead.

**Example:**
```csharp
// ❌ Bad - allocates new array on every access
public static string[] Parts => "a,b,c".Split(',');

// ✅ Good - allocated once and cached
public static string[] Parts { get; } = "a,b,c".Split(',');
```

### TL006 - Where().Count() Detection
**Category:** Performance | **Severity:** Warning

Detects `Where(predicate).Count()` patterns and suggests using `Count(predicate)` for better performance.

**Example:**
```csharp
// ❌ Bad - two-pass operation
var count = items.Where(x => x.IsValid).Count();

// ✅ Good - single-pass operation
var count = items.Count(x => x.IsValid);
```

### TL007 - Unnecessary ToList/ToArray in Method Chains
**Category:** Performance | **Severity:** Warning

Detects unnecessary `ToList()` or `ToArray()` calls in the middle of method chains that create intermediate collections only to be immediately enumerated again.

**Example:**
```csharp
// ❌ Bad - unnecessary intermediate collection
var result = items.Where(x => x.IsValid).ToList().Select(x => x.Name).ToArray();

// ✅ Good - direct enumeration without intermediate collection
var result = items.Where(x => x.IsValid).Select(x => x.Name).ToArray();
```

### TL010 - Unnecessary ToList on Materialized Collections
**Category:** Performance | **Severity:** Warning

Detects `ToList()` calls on collections that are already materialized (like `List<T>`, arrays, etc.) which creates unnecessary copies.

**Example:**
```csharp
List<string> names = GetNames();

// ❌ Bad - creates unnecessary copy of already materialized collection
var filtered = names.ToList().Where(x => x.Length > 3).ToList();

// ✅ Good - use the materialized collection directly
var filtered = names.Where(x => x.Length > 3).ToList();
```
