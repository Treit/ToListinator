using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullCoalescingForeachAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL004";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: "TL004",
        title: "Avoid foreach with null coalescing to empty collection",
        messageFormat: "Avoid using null coalescing operator (??) with empty collection in foreach. Check for null instead to avoid unnecessary allocations and for better performance.",
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
            var arrayType = startContext.Compilation.GetTypeByMetadataName("System.Array");

            startContext.RegisterOperationAction(
                operationContext => AnalyzeForEachLoop(operationContext, enumerableType, arrayType),
                OperationKind.Loop);
        });
    }

    private static void AnalyzeForEachLoop(OperationAnalysisContext context, INamedTypeSymbol? enumerableType, INamedTypeSymbol? arrayType)
    {
        if (context.Operation is not IForEachLoopOperation forEachLoop)
        {
            return;
        }

        // Unwrap implicit conversions on the collection expression
        IOperation collection = forEachLoop.Collection;

        // Roslyn may wrap the collection in one or more implicit conversions
        // (e.g. covariance, interface adaptation). Peel them all off.
        while (collection is IConversionOperation { IsImplicit: true } conversion)
        {
            collection = conversion.Operand;
        }

        if (collection is not ICoalesceOperation coalesce)
        {
            return;
        }

        // Check if the fallback (WhenNull) is an empty collection
        IOperation fallback = coalesce.WhenNull;

        // Roslyn may wrap the fallback in one or more implicit conversions.
        while (fallback is IConversionOperation { IsImplicit: true } fallbackConversion)
        {
            fallback = fallbackConversion.Operand;
        }

        if (!IsEmptyCollectionOperation(fallback, enumerableType, arrayType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, coalesce.Syntax.GetLocation()));
    }

    private static bool IsEmptyCollectionOperation(IOperation operation, INamedTypeSymbol? enumerableType, INamedTypeSymbol? arrayType)
    {
        return operation switch
        {
            IObjectCreationOperation { Arguments.Length: 0 } => true,

            IInvocationOperation { TargetMethod: { Name: "Empty" } method } =>
                (enumerableType is not null && SymbolEqualityComparer.Default.Equals(method.ContainingType, enumerableType))
                || (arrayType is not null && SymbolEqualityComparer.Default.Equals(method.ContainingType, arrayType)),

            IPropertyReferenceOperation { Property: { Name: "Empty", IsStatic: true } } => true,

            // new T[0] or new T[] { }
            IArrayCreationOperation arrayCreation => IsEmptyArrayCreation(arrayCreation),

            _ => false
        };
    }

    private static bool IsEmptyArrayCreation(IArrayCreationOperation arrayCreation)
    {
        // new T[] { } — empty initializer
        if (arrayCreation.Initializer is { ElementValues.Length: 0 })
        {
            return true;
        }

        // new T[0] — zero-length array
        if (arrayCreation.DimensionSizes.Length == 1
            && arrayCreation.DimensionSizes[0] is ILiteralOperation
               {
                   ConstantValue: { HasValue: true, Value: 0 }
               })
        {
            return true;
        }

        return false;
    }
}
