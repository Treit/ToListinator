using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

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

        if (TryMatchToArrayLengthComparison(binary.LeftOperand, binary.RightOperand, binary.OperatorKind, isLengthOnLeft: true, enumerableType)
            || TryMatchToArrayLengthComparison(binary.RightOperand, binary.LeftOperand, binary.OperatorKind, isLengthOnLeft: false, enumerableType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, binary.Syntax.GetLocation()));
        }
    }

    private static bool TryMatchToArrayLengthComparison(
        IOperation lengthSide,
        IOperation constantSide,
        BinaryOperatorKind operatorKind,
        bool isLengthOnLeft,
        INamedTypeSymbol enumerableType)
    {
        if (lengthSide is not IPropertyReferenceOperation
            {
                Property.Name: "Length",
                Instance: IInvocationOperation { TargetMethod: { Name: "ToArray" } toArrayMethod }
            }
            || !SymbolEqualityComparer.Default.Equals(toArrayMethod.ContainingType, enumerableType))
        {
            return false;
        }

        if (constantSide is not ILiteralOperation { ConstantValue: { HasValue: true, Value: int constantValue } }
            || constantValue is not (0 or 1))
        {
            return false;
        }

        return IsValidLengthComparisonPattern(operatorKind, constantValue, isLengthOnLeft);
    }

    private static bool IsValidLengthComparisonPattern(
        BinaryOperatorKind operatorKind,
        int constantValue,
        bool isLengthOnLeft)
    {
        if (isLengthOnLeft)
        {
            return operatorKind switch
            {
                BinaryOperatorKind.GreaterThan when constantValue == 0 => true,
                BinaryOperatorKind.GreaterThanOrEqual when constantValue == 1 => true,
                BinaryOperatorKind.NotEquals when constantValue == 0 => true,
                BinaryOperatorKind.Equals when constantValue == 0 => true,
                BinaryOperatorKind.LessThanOrEqual when constantValue == 0 => true,
                BinaryOperatorKind.LessThan when constantValue == 1 => true,
                _ => false
            };
        }

        return operatorKind switch
        {
            BinaryOperatorKind.LessThan when constantValue == 0 => true,
            BinaryOperatorKind.LessThanOrEqual when constantValue == 1 => true,
            BinaryOperatorKind.NotEquals when constantValue == 0 => true,
            BinaryOperatorKind.Equals when constantValue == 0 => true,
            BinaryOperatorKind.GreaterThanOrEqual when constantValue == 0 => true,
            BinaryOperatorKind.GreaterThan when constantValue == 1 => true,
            _ => false
        };
    }
}
