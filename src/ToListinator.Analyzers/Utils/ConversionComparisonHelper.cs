using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ToListinator.Analyzers.Utils;

public static class ConversionComparisonHelper
{
    public static bool TryMatchConversionPropertyComparison(
        IOperation propertySide,
        IOperation constantSide,
        BinaryOperatorKind operatorKind,
        bool isPropertyOnLeft,
        string propertyName,
        string conversionMethodName,
        INamedTypeSymbol enumerableType)
    {
        if (propertySide is not IPropertyReferenceOperation
            {
                Instance: IInvocationOperation { } invocation
            } propertyRef
            || propertyRef.Property.Name != propertyName
            || invocation.TargetMethod.Name != conversionMethodName
            || !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, enumerableType))
        {
            return false;
        }

        if (constantSide is not ILiteralOperation { ConstantValue: { HasValue: true, Value: int constantValue } }
            || constantValue is not (0 or 1))
        {
            return false;
        }

        return IsValidComparisonPattern(operatorKind, constantValue, isPropertyOnLeft);
    }

    public static bool IsValidComparisonPattern(
        BinaryOperatorKind operatorKind,
        int constantValue,
        bool isPropertyOnLeft)
    {
        if (isPropertyOnLeft)
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
