using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using ToListinator.Analyzers.Utils;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ToArrayLengthAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL011";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Replace ToArray().Length comparison with Any()",
        messageFormat: "Avoid using ToArray().Length for existence checks. Use Any() to avoid unnecessary allocation.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            var enumerableType = startContext.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            if (enumerableType is null)
            {
                return;
            }

            startContext.RegisterOperationAction(
                analysisContext => AnalyzeBinaryOperation(analysisContext, enumerableType),
                OperationKind.Binary);
        });
    }

    private static void AnalyzeBinaryOperation(OperationAnalysisContext context, INamedTypeSymbol enumerableType)
    {
        var binary = (IBinaryOperation)context.Operation;

        if (ConversionComparisonHelper.TryMatchConversionPropertyComparison(
                binary.LeftOperand, binary.RightOperand, binary.OperatorKind,
                isPropertyOnLeft: true, "Length", "ToArray", enumerableType)
            || ConversionComparisonHelper.TryMatchConversionPropertyComparison(
                binary.RightOperand, binary.LeftOperand, binary.OperatorKind,
                isPropertyOnLeft: false, "Length", "ToArray", enumerableType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, binary.Syntax.GetLocation()));
        }
    }
}
