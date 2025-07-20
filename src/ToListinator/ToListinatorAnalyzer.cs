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
