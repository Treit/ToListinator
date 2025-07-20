using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ToListinator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ToListForEachAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TL001";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: "TL001",
            title: "Avoid ToList().ForEach",
            messageFormat: "Avoid using ToList().ForEach, which allocates a List unnecessarily. Use a regular foreach loop instead.",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => [Rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Check if this matches the pattern someExpression.ToList().ForEach(...)
            if (invocation.Expression is not MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ForEach",
                    Expression: InvocationExpressionSyntax
                    {
                        Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ToList" }
                    } toListInvocation
                })
            {
                return;
            }

            // Verify it's actually List<T> and not something custom.
            var toListSymbol = context.SemanticModel.GetSymbolInfo(toListInvocation).Symbol as IMethodSymbol;
            var forEachSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (IsToListForEachPattern(toListSymbol, forEachSymbol))
            {
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsToListForEachPattern(IMethodSymbol? toListSymbol, IMethodSymbol? forEachSymbol)
        {
            return toListSymbol is { Name: "ToList" } &&
                   toListSymbol.ReturnType is INamedTypeSymbol toListReturnType &&
                   toListReturnType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>" &&
                   forEachSymbol is { Name: "ForEach" } &&
                   SymbolEqualityComparer.Default.Equals(forEachSymbol.ContainingType, toListReturnType);
        }
    }
}
