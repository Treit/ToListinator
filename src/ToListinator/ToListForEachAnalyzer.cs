using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
            context.RegisterCompilationStartAction(startContext =>
            {
                var listType = startContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
                var enumerableType = startContext.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
                if (listType is null || enumerableType is null)
                {
                    return;
                }

                startContext.RegisterOperationAction(operationContext =>
                {
                    AnalyzeInvocation(operationContext, listType, enumerableType);
                }, OperationKind.Invocation);
            });
        }

        private void AnalyzeInvocation(OperationAnalysisContext context, ITypeSymbol listType, ITypeSymbol enumerableType)
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (invocation is not
                {
                    TargetMethod: { Name: "ForEach" } forEachMethod,
                    Instance: IInvocationOperation { TargetMethod: { Name: "ToList" } toListMethod }
                }
                || !SymbolEqualityComparer.Default.Equals(forEachMethod.ContainingType.OriginalDefinition, listType)
                || !SymbolEqualityComparer.Default.Equals(toListMethod.ContainingType, enumerableType))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, invocation.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
