### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TL003   | Performance | Warning | Replace ToList().Count comparisons with Any() to avoid unnecessary allocation
TL004   | Performance | Warning | Avoid foreach with null coalescing to empty collection
TL005   | Performance | Warning | Avoid static property expression bodies that create new instances
TL006   | Performance | Warning | Use Count(predicate) instead of Where(predicate).Count() for better performance
TL009   | Performance | Warning | Replace Count() comparison with Any() to avoid enumerating the entire sequence