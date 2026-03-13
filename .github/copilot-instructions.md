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

### IOperation Pitfalls with Extension Methods

- In IOperation, LINQ extension methods called with **dot syntax** (`items.ToList().First()`) and **static form** (`Enumerable.First(items.ToList())`) are **indistinguishable**: both have `Instance = null` with the receiver in `Arguments[0]`. `ReducedFrom` is also null in both cases.
- When an analyzer needs to distinguish dot syntax from static form, **drop to syntax**: check whether `invocation.Syntax` is an `InvocationExpressionSyntax` whose `Expression` is a `MemberAccessExpressionSyntax` with an `InvocationExpressionSyntax` receiver (dot syntax) vs. an `IdentifierNameSyntax` or `QualifiedNameSyntax` (static form).
- Only diagnose patterns the code fix can actually rewrite. If the static form can't be fixed, exclude it in the analyzer rather than producing a no-op fix.

### Exception Semantics

- Never suggest a fix that changes the exception type thrown on error. For example, array indexers throw `IndexOutOfRangeException` while `List<T>` indexers and `ElementAt` throw `ArgumentOutOfRangeException` — rewriting `ToArray()[i]` to `ElementAt(i)` would change observable behavior.

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

## GitHub CLI (gh) Usage

When creating or editing PRs/issues with `gh`, **write the body to a temp file** and pass it with `--body-file` instead of using inline `--body` strings. PowerShell string interpolation and shell escaping corrupt backticks into backslashes.

```powershell
$bodyFile = [System.IO.Path]::GetTempFileName()
@"
## Summary
Adds the `Foo` analyzer...
"@ | Set-Content -Path $bodyFile -Encoding utf8
gh pr create --title "Add Foo analyzer" --body-file $bodyFile
Remove-Item $bodyFile
```

## Skill Emoji Convention

When performing tasks, tag each skill used with its emoji inline and provide a summary of skills used at the end of the task.

| Emoji | Skill |
|-------|-------|
| 📁 | Project Layout |
| 🔍 | Writing Analyzers |
| 🔧 | Code Fix Providers |
| 🎀 | Trivia Handling |
| 🛠️ | Utility Usage |
| 🎯 | Pattern Matching |
| 🧪 | Writing Tests |
| 🏗️ | Testing with Test App |
| 🌳 | Roslyn Syntax Tree Navigation |
| ✅ | Adding New Analyzer E2E |

Example: When writing an analyzer, tag the work with 🔍 inline, and at the end summarize: **Skills used:** 🔍 Writing Analyzers, 🎯 Pattern Matching, 🧪 Writing Tests
