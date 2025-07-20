using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace ToListinator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ToListinatorAnalyzer : DiagnosticAnalyzer
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
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is MemberAccessExpressionSyntax forEachAccess &&
                forEachAccess.Name.Identifier.Text == "ForEach")
            {
                if (forEachAccess.Expression is InvocationExpressionSyntax toListInvocation &&
                    toListInvocation.Expression is MemberAccessExpressionSyntax toListAccess &&
                    toListAccess.Name.Identifier.Text == "ToList")
                {
                    var toListSymbol = context.SemanticModel.GetSymbolInfo(toListInvocation).Symbol as IMethodSymbol;
                    var forEachSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

                    if (toListSymbol?.Name == "ToList" &&
                        toListSymbol.ReturnType is INamedTypeSymbol toListReturnType &&
                        toListReturnType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>" &&
                        forEachSymbol?.Name == "ForEach" &&
                        SymbolEqualityComparer.Default.Equals(forEachSymbol.ContainingType, toListReturnType))
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            invocation.GetLocation()
                        );

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
