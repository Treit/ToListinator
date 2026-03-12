# Copilot Instructions for ToListinator

## Build, Test, and Lint

```powershell
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test by name
dotnet test --filter "FullyQualifiedName~ToListForEachAnalyzerTests.DetectsToListForEach"

# Verify analyzers against real code (clears NuGet cache; initial failures are expected)
.\test\ToListinator.TestApp\RebuildTestApp.ps1
```

CI runs `dotnet build --configuration Release` and `dotnet test --configuration Release` on .NET 9. `TreatWarningsAsErrors` is enabled globally.

## Architecture

This is a Roslyn analyzer NuGet package that detects unnecessary LINQ allocations in C# code. Each analyzer has a matching code fix provider.

- **`src/ToListinator.Analyzers/`** — `DiagnosticAnalyzer` implementations (one per `TL###` rule)
- **`src/ToListinator.CodeFixes/`** — `CodeFixProvider` implementations (one per analyzer)
- **`src/ToListinator.Utils/`** — Shared utilities: `FluentChainAligner`, `TriviaHelper`, `BlankLineFormatter`, `CodeFixHelper`
- **`src/ToListinator/`** — NuGet packaging project that bundles Analyzers + CodeFixes + Utils into a single package
- **`test/ToListinator.Tests/`** — xUnit tests using `Microsoft.CodeAnalysis.CSharp.CodeFix.Testing`

Analyzers and code fixes target **netstandard2.0** with `<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>`. The test project targets **net8.0**.

## Key Conventions

### Analyzer Implementation

- Use `RegisterCompilationStartAction` to resolve type symbols once, then `RegisterOperationAction` for semantic analysis via `IOperation` APIs (not raw syntax trees).
- Always use `DiagnosticSeverity.Warning`.
- Diagnostic IDs follow the `TL###` pattern (three zero-padded digits). Check `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md` for the next available ID.
- Use modern C# pattern matching extensively — property patterns, `or`/`and` combinators, switch expressions. Use `Name` over `MetadataName`, `Arity` over manual generic param counting, `not null` over `!= null`.

### Code Fix Implementation

- Find nodes via `root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()` (not `FindNode`).
- Always return `WellKnownFixAllProviders.BatchFixer` from `GetFixAllProvider()`.
- Use shared helpers from `ToListinator.Utils`: `CodeFixHelper` for node finding and code action creation, `TriviaHelper` for comment/trivia preservation.
- **Trivia handling** — extract leading/trailing trivia before transformations, call `WithoutTrivia()` on relocated expressions, attach trivia to the outermost new construct, and always annotate with `Formatter.Annotation`. Never use `NormalizeWhitespace()`.
- Always call `FluentChainAligner.AlignFluentChains(newRoot)` as the final step for fixes that modify method chains.

### Tests

- Use `TestHelper.CreateCodeFixTest<TAnalyzer, TCodeFixProvider>(testCode, fixedCode)` and `TestHelper.CreateAnalyzerTest<TAnalyzer>(source)` from the `Helpers/` folder.
- Use raw string literals (`"""`) for test code and mark expected diagnostics with `{|TL###:code|}` or `{|#0:code|}` with `TestHelper.CreateDiagnostic("TL###").WithLocation(0)`.
- Cover positive cases, negative cases, edge cases, and trivia/formatting preservation.

### Adding a New Analyzer (Checklist)

1. Create analyzer in `src/ToListinator.Analyzers/`
2. Create code fix provider in `src/ToListinator.CodeFixes/`
3. Add tests (both analyzer and code fix) in `test/ToListinator.Tests/`
4. Update `AnalyzerReleases.Unshipped.md`
5. Update `README.md` with the new rule's documentation
6. Run `dotnet build && dotnet test`
