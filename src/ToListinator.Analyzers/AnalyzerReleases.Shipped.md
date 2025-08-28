## Release 0.0.6

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TL006   | Performance | Warning | Use Count(predicate) instead of Where(predicate).Count() for better performance
TL007   | Performance | Warning | Avoid unnecessary ToList() or ToArray() in method chains that create intermediate collections
TL010   | Performance | Warning | Remove unnecessary ToList() call on already materialized collection

## Release 0.0.5

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TL003   | Performance | Warning | Replace ToList().Count comparisons with Any() to avoid unnecessary allocation
TL004   | Performance | Warning | Avoid foreach with null coalescing to empty collection
TL005   | Performance | Warning | Avoid static property expression bodies that create new instances

## Release 0.0.4

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TL002   | Performance | Warning | Detects useless Select(x => x) identity operations

## Release 0.0.3

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TL001   | Performance | Warning | Detects unnecessary ToList() calls
