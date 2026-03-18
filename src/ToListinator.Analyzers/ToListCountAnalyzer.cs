using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ToListCountAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL003";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Replace ToList().Count comparison with Any()",
        messageFormat: "Avoid using ToList().Count for existence checks. Use Any() to avoid unnecessary allocation.",
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

        if (TryMatchToListCountComparison(binary.LeftOperand, binary.RightOperand, binary.OperatorKind, isCountOnLeft: true, enumerableType)
            || TryMatchToListCountComparison(binary.RightOperand, binary.LeftOperand, binary.OperatorKind, isCountOnLeft: false, enumerableType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, binary.Syntax.GetLocation()));
        }
    }

    private static bool TryMatchToListCountComparison(
        IOperation countSide,
        IOperation constantSide,
        BinaryOperatorKind operatorKind,
        bool isCountOnLeft,
        INamedTypeSymbol enumerableType)
    {
        if (countSide is not IPropertyReferenceOperation
            {
                Property.Name: "Count",
                Instance: IInvocationOperation { TargetMethod: { Name: "ToList" } toListMethod }
            }
            || !SymbolEqualityComparer.Default.Equals(toListMethod.ContainingType, enumerableType))
        {
            return false;
        }

        if (constantSide is not ILiteralOperation { ConstantValue: { HasValue: true, Value: int constantValue } }
            || constantValue is not (0 or 1))
        {
            return false;
        }

        return IsValidCountComparisonPattern(operatorKind, constantValue, isCountOnLeft);
    }

    private static bool IsValidCountComparisonPattern(
        BinaryOperatorKind operatorKind,
        int constantValue,
        bool isCountOnLeft)
    {
        if (isCountOnLeft)
        {
            return operatorKind switch
            {
                // Existence patterns: > 0, >= 1, != 0
                BinaryOperatorKind.GreaterThan when constantValue == 0 => true,
                BinaryOperatorKind.GreaterThanOrEqual when constantValue == 1 => true,
                BinaryOperatorKind.NotEquals when constantValue == 0 => true,
                // Non-existence patterns: == 0, <= 0, < 1
                BinaryOperatorKind.Equals when constantValue == 0 => true,
                BinaryOperatorKind.LessThanOrEqual when constantValue == 0 => true,
                BinaryOperatorKind.LessThan when constantValue == 1 => true,
                _ => false
            };
        }

        return operatorKind switch
        {
            // Existence patterns: 0 <, 1 <=, 0 !=
            BinaryOperatorKind.LessThan when constantValue == 0 => true,
            BinaryOperatorKind.LessThanOrEqual when constantValue == 1 => true,
            BinaryOperatorKind.NotEquals when constantValue == 0 => true,
            // Non-existence patterns: 0 ==, 0 >=, 1 >
            BinaryOperatorKind.Equals when constantValue == 0 => true,
            BinaryOperatorKind.GreaterThanOrEqual when constantValue == 0 => true,
            BinaryOperatorKind.GreaterThan when constantValue == 1 => true,
            _ => false
        };
    }
}
