# Variables
$solutionName = "ToListinator"
$srcProjName = "ToListinator"
$testProjName = "ToListinator.Tests"
$srcPath = "src/$srcProjName"
$testPath = "test/$testProjName"

# Create folders
New-Item -Type Directory -Force -Path "src" | Out-Null
New-Item -Type Directory -Force -Path "test" | Out-Null

# Create solution with slnx format
dotnet new sln --name $solutionName --output . --format slnx

# Create the Analyzer project (netstandard2.0)
dotnet new classlib -n $srcProjName -f netstandard2.0 -o $srcPath
Remove-Item "$srcPath/Class1.cs"

# Create stub analyzer
@"
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace ToListinator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ToListinatorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TL001";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Unnecessary ToList call",
            "ToList() call is unnecessary",
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Register analysis actions to find unnecessary ToList() calls
        }
    }
}
"@ | Set-Content -Path "$srcPath/ToListinatorAnalyzer.cs" -Encoding UTF8

# Add Roslyn packages
Push-Location $srcPath
dotnet add package Microsoft.CodeAnalysis.CSharp
dotnet add package Microsoft.CodeAnalysis.Analyzers
dotnet add package Microsoft.CodeAnalysis.CSharp.Workspaces
Pop-Location

# Update csproj: Mark PackageReferences as PrivateAssets
$srcProjFile = "$srcPath/$srcProjName.csproj"
(Get-Content $srcProjFile) `
  -replace '<PackageReference Include="Microsoft.CodeAnalysis.CSharp"', '<PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all"' `
  -replace '<PackageReference Include="Microsoft.CodeAnalysis.Analyzers"', '<PackageReference Include="Microsoft.CodeAnalysis.Analyzers" PrivateAssets="all"' `
  -replace '<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces"', '<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" PrivateAssets="all"' `
  | Set-Content $srcProjFile

# Inject analyzer packing config using literal here-string to avoid MSBuild property evaluation
$csprojText = Get-Content $srcProjFile -Raw
$csprojText = $csprojText -replace '</PropertyGroup>', @'
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>
'@

$csprojText = $csprojText -replace '</Project>', @'
  <ItemGroup>
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
</Project>
'@

$csprojText | Set-Content $srcProjFile

# Create the Test project (xUnit targeting net8.0)
dotnet new xunit -n $testProjName -f net8.0 -o $testPath
Remove-Item "$testPath/UnitTest1.cs"

# Reference analyzer project
dotnet add "$testPath/$testProjName.csproj" reference "$srcPath/$srcProjName.csproj"

# Add xUnit + Roslyn testing packages
Push-Location $testPath
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.CodeAnalysis.CSharp.Analyzer.Testing
dotnet add package Microsoft.CodeAnalysis.CSharp.Workspaces
Pop-Location

# Create test file
@"
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Xunit;
using ToListinator;

public class ToListinatorAnalyzerTests
{
    [Fact]
    public async Task NoDiagnosticsForEmptyCode()
    {
        var test = new CSharpAnalyzerTest<ToListinatorAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = ""
        };

        await test.RunAsync();
    }
}
"@ | Set-Content -Path "$testPath/ToListinatorAnalyzerTests.cs" -Encoding UTF8

# Add projects to solution
dotnet sln "$solutionName.slnx" add "$srcPath/$srcProjName.csproj"
dotnet sln "$solutionName.slnx" add "$testPath/$testProjName.csproj"
