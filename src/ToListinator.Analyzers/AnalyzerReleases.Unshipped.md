### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TL003   | Performance | Warning | Replace ToList().Count comparisons with Any() to avoid unnecessary allocation
TL004   | Performance | Warning | Avoid foreach with null coalescing to empty collection