# Issue #15 Implementation Plan: Static Expression Property Analyzer

## Overview
**Issue:** [#15 - Analyzer: Avoid static => new pattern](https://github.com/Treit/ToListinator/issues/15)
**Diagnostic ID:** TL005
**Priority:** Medium
**Estimated Effort:** 4-5 days

## Problem Statement
Create a Roslyn analyzer and code fix to detect static properties using expression body syntax (`=>`) that create new instances on every access, causing unnecessary allocations. The analyzer should suggest converting to getter-only properties with initialization.

## Target Anti-Patterns

### Pattern 1: Method Calls That May Allocate
```csharp
// ❌ BAD - calls method every access
public static string? RoleInstance => Environment.GetEnvironmentVariable("MONITORING_ROLE_INSTANCE");

// ✅ GOOD - calls once, caches result
public static string? RoleInstance { get; } = Environment.GetEnvironmentVariable("MONITORING_ROLE_INSTANCE");
```

### Pattern 2: New Collection Creation
```csharp
// ❌ BAD - allocates new collection every access
public static HashSet<string> Items => new HashSet<string> { "A", "B", "C" };

// ✅ GOOD - creates once, caches
public static HashSet<string> Items { get; } = new HashSet<string> { "A", "B", "C" };
```

### Pattern 3: Complex Expressions That Allocate
```csharp
// ❌ BAD - processes every access
public static string[] Parts => "a,b,c".Split(',');

// ✅ GOOD - processes once
public static string[] Parts { get; } = "a,b,c".Split(',');
```

## Implementation Components

### 1. Analyzer: `StaticExpressionPropertyAnalyzer.cs`
**Location:** `src/ToListinator.Analyzers/StaticExpressionPropertyAnalyzer.cs`

```csharp
// Key implementation details:
public const string DiagnosticId = "TL005";
private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
    id: "TL005",
    title: "Avoid static property expression bodies that create new instances",
    messageFormat: "Static property '{0}' uses expression body syntax that may allocate on every access. Consider using getter-only property initialization instead.",
    category: "Performance",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true);
```

**Analysis Strategy:**
- Register for `SyntaxKind.PropertyDeclaration`
- Use `RegisterCompilationStartAction` for performance
- Check for static properties with expression bodies
- Analyze expressions for allocation patterns

**Detection Logic:**
1. Property must be `static`
2. Property must use expression body syntax (`=>`)
3. Expression must contain potentially allocating operations:
   - `new` expressions
   - Method calls (except pure/cached methods)
   - Collection initializers
   - String operations that allocate

### 2. Code Fix: `StaticExpressionPropertyCodeFixProvider.cs`
**Location:** `src/ToListinator.CodeFixes/StaticExpressionPropertyCodeFixProvider.cs`

**Transformation Logic:**
```csharp
// Transform from:
public static Type Property => expression;

// To:
public static Type Property { get; } = expression;
```

**Implementation Requirements:**
- Preserve all trivia (comments, formatting)
- Handle complex expressions safely
- Support fix-all scenarios
- Maintain semantic equivalence

### 3. Test Suite

#### Analyzer Tests: `StaticExpressionPropertyAnalyzerTests.cs`
**Location:** `test/ToListinator.Tests/StaticExpressionPropertyAnalyzerTests.cs`

**Positive Test Cases (Should Trigger):**
```csharp
// Environment method calls
public static string? EnvVar => Environment.GetEnvironmentVariable("KEY");

// Object creation
public static HashSet<string> Items => new HashSet<string> { "A", "B" };
public static List<int> Numbers => new List<int> { 1, 2, 3 };
public static Dictionary<string, int> Map => new Dictionary<string, int>();

// String operations that allocate
public static string[] Split => "a,b,c".Split(',');
public static string Upper => "hello".ToUpper();

// LINQ operations
public static IEnumerable<int> Filtered => data.Where(x => x > 0);
```

**Negative Test Cases (Should NOT Trigger):**
```csharp
// Non-static properties
public string Instance => "value";

// Already cached properties  
public static string Cached { get; } = "value";

// Simple literals/constants
public static string Literal => "constant";
public static int Number => 42;
public static bool Flag => true;

// Static field references
private static readonly string _field = "value";
public static string Property => _field;

// Pure expressions (no allocation)
public static int Calculate => 5 + 3;
public static string Combine => "a" + "b"; // compiler optimizes
```

#### Code Fix Tests: `StaticExpressionPropertyCodeFixTests.cs`
**Location:** `test/ToListinator.Tests/StaticExpressionPropertyCodeFixTests.cs`

**Test Scenarios:**
1. Basic property transformations
2. Properties with XML documentation
3. Properties with attributes
4. Multi-line expressions
5. Complex formatting preservation
6. Fix-all scenarios (multiple properties)

## Project Updates Required

### 1. Release Notes Update
**File:** `src/ToListinator.Analyzers/AnalyzerReleases.Unshipped.md`

```markdown
### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TL003   | Performance | Warning | Replace ToList().Count comparisons with Any() to avoid unnecessary allocation
TL004   | Performance | Warning | Avoid foreach with null coalescing to empty collection
TL005   | Performance | Warning | Avoid static property expression bodies that create new instances
```

### 2. Test Project References
Ensure new analyzer and code fix are properly referenced in test projects.

## Implementation Phases

### Phase 1: Core Analyzer (Day 1)
- [ ] Create `StaticExpressionPropertyAnalyzer.cs`
- [ ] Implement basic detection for `new` expressions
- [ ] Add diagnostic descriptor with TL005 ID
- [ ] Create basic positive/negative test cases
- [ ] Verify analyzer triggers correctly

### Phase 2: Code Fix Provider (Day 2)
- [ ] Create `StaticExpressionPropertyCodeFixProvider.cs`
- [ ] Implement basic transformation logic
- [ ] Handle syntax tree manipulation safely
- [ ] Create code fix test cases
- [ ] Verify transformations are correct

### Phase 3: Enhanced Detection (Day 3)
- [ ] Add method call analysis for allocation detection
- [ ] Implement collection creation pattern recognition
- [ ] Add LINQ operation detection
- [ ] Enhance edge case handling
- [ ] Add comprehensive test coverage

### Phase 4: Advanced Features (Day 4)
- [ ] Implement fix-all scenarios
- [ ] Add complex formatting preservation
- [ ] Handle properties with attributes/documentation
- [ ] Add performance optimizations
- [ ] Test integration with existing analyzers

### Phase 5: Polish & Integration (Day 5)
- [ ] Update release documentation
- [ ] Run full test suite
- [ ] Performance testing
- [ ] Integration testing with real codebases
- [ ] Final code review and cleanup

## Technical Considerations

### Detection Sophistication
1. **Method Purity Analysis:** Distinguish between pure methods and allocating methods
2. **Type System Integration:** Use semantic model for accurate type analysis
3. **Expression Complexity:** Handle nested expressions and complex syntax trees
4. **False Positive Avoidance:** Carefully tune detection to avoid noise

### Performance Optimizations
1. **Symbol Caching:** Cache type symbols in compilation start action
2. **Efficient Traversal:** Minimize syntax tree walking
3. **Lazy Evaluation:** Only perform expensive analysis when necessary
4. **Memory Management:** Avoid allocations in analyzer hot paths

### Code Fix Robustness
1. **Trivia Preservation:** Maintain all formatting, comments, and whitespace
2. **Syntax Validation:** Ensure generated code compiles correctly
3. **Semantic Preservation:** Maintain exact semantic behavior
4. **Error Recovery:** Handle malformed or unusual syntax gracefully

## Success Criteria

- [ ] **Accuracy:** Analyzer correctly identifies target patterns with <1% false positive rate
- [ ] **Completeness:** Code fix handles all detected patterns correctly
- [ ] **Performance:** Analyzer adds <50ms to compilation time for typical projects  
- [ ] **Integration:** Works seamlessly with existing ToListinator analyzers
- [ ] **Testing:** Achieves >95% code coverage with comprehensive test suite
- [ ] **Documentation:** All changes properly documented following project conventions

## Risk Mitigation

### High Risk: False Positives
- **Mitigation:** Extensive testing with real codebases, conservative detection rules
- **Validation:** Manual review of analyzer results on large projects

### Medium Risk: Complex Code Fix Scenarios
- **Mitigation:** Incremental implementation, robust trivia handling
- **Validation:** Test complex formatting scenarios extensively

### Low Risk: Performance Impact
- **Mitigation:** Performance testing, efficient implementation patterns
- **Validation:** Benchmark against existing analyzers

## Notes for Implementation

1. **Follow Existing Patterns:** Use the same structure as `ToListForEachAnalyzer` and `IdentitySelectAnalyzer`
2. **Test-Driven Development:** Write tests first, then implement to satisfy them
3. **Incremental Approach:** Start simple, add complexity gradually
4. **Code Review:** Follow all patterns from `analyzer.prompt.md` instructions
5. **Integration Testing:** Test with `ToListinator.TestApp` for real-world validation

## Files to Create/Modify

### New Files:
- `src/ToListinator.Analyzers/StaticExpressionPropertyAnalyzer.cs`
- `src/ToListinator.CodeFixes/StaticExpressionPropertyCodeFixProvider.cs`
- `test/ToListinator.Tests/StaticExpressionPropertyAnalyzerTests.cs`
- `test/ToListinator.Tests/StaticExpressionPropertyCodeFixTests.cs`

### Modified Files:
- `src/ToListinator.Analyzers/AnalyzerReleases.Unshipped.md`

### Validation Files:
- Test with existing projects in `test/ToListinator.TestApp/`
- Integration test in `test/RoslynPlayground/` if needed

---

**Plan Created:** August 3, 2025
**Estimated Completion:** 4-5 development days
**Ready for Implementation:** Yes
