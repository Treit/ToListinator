using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using ToListinator.Analyzers.Utils;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnnecessaryToListAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL010";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Remove unnecessary ToList() call on already materialized collection",
        messageFormat: "Avoid calling ToList() on '{0}' which is already materialized. Use the collection directly with LINQ operations.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Calling ToList() on collections that are already materialized (List<T>, Array, etc.) before LINQ operations creates an unnecessary copy and wastes memory and CPU.");

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

            startContext.RegisterOperationAction(analysisContext =>
            {
                AnalyzeInvocation(analysisContext, enumerableType);
            }, OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol enumerableType)
    {
        var invocation = (IInvocationOperation)context.Operation;

        // Must be Enumerable.ToList() with no arguments (only the extension 'this' param)
        if (invocation.TargetMethod.Name is not "ToList"
            || invocation.TargetMethod.Parameters.Length != 1
            || !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, enumerableType))
        {
            return;
        }

        // Get the source type (unwrap implicit conversions to get the actual type)
        var sourceType = GetSourceType(invocation);
        if (sourceType is null || !IsMaterializedCollection(sourceType))
        {
            return;
        }

        // Check if this ToList() result is followed by LINQ operations or passed to IEnumerable param
        if (!IsFollowedByLinqOperations(invocation, context.Compilation))
        {
            return;
        }

        var sourceTypeName = GetFriendlyTypeName(sourceType);
        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), sourceTypeName));
    }

    private static ITypeSymbol? GetSourceType(IInvocationOperation invocation)
    {
        if (invocation.Arguments.Length == 0)
        {
            return null;
        }

        IOperation sourceOp = invocation.Arguments[0].Value;

        // Roslyn may wrap the receiver in one or more implicit conversions
        // (e.g. covariance, interface adaptation). Peel them all off.
        while (sourceOp is IConversionOperation { IsImplicit: true } conversion)
        {
            sourceOp = conversion.Operand;
        }

        return sourceOp.Type;
    }

    private static bool IsMaterializedCollection(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var originalDefinition = namedType.OriginalDefinition.ToDisplayString();
            return originalDefinition switch
            {
                "System.Collections.Generic.List<T>" => true,
                "System.Collections.Generic.IList<T>" => true,
                "System.Collections.Generic.ICollection<T>" => true,
                "System.Collections.ObjectModel.Collection<T>" => true,
                "System.Collections.ObjectModel.ObservableCollection<T>" => true,
                "System.Collections.Generic.HashSet<T>" => true,
                "System.Collections.Generic.SortedSet<T>" => true,
                _ => false
            };
        }

        if (type.TypeKind == TypeKind.Array)
        {
            return true;
        }

        return type.ToDisplayString() switch
        {
            "System.Collections.ArrayList" => true,
            "System.Collections.Queue" => true,
            "System.Collections.Stack" => true,
            _ => false
        };
    }

    private static bool IsFollowedByLinqOperations(IInvocationOperation toListInvocation, Compilation compilation)
    {
        IOperation current = toListInvocation;

        // Roslyn may wrap the result in one or more implicit conversions
        // (e.g. covariance, interface adaptation). Walk past them all.
        while (current.Parent is IConversionOperation { IsImplicit: true })
        {
            current = current.Parent;
        }

        // Extension method chain: ToList is the 'this' argument of the next method
        if (current.Parent is IArgumentOperation argument
            && argument.Parent is IInvocationOperation parentInvocation)
        {
            if (parentInvocation.TargetMethod.IsExtensionMethod
                && argument.Parameter is { Ordinal: 0 })
            {
                var methodName = parentInvocation.TargetMethod.Name;
                return MethodChainHelper.IsLinqMethod(methodName, includeConversionMethods: false)
                       || methodName == "ForEach";
            }

            // Regular argument: check if parameter accepts IEnumerable<T>
            if (argument.Parameter is not null)
            {
                return CanParameterAcceptIEnumerable(argument.Parameter.Type, toListInvocation, compilation);
            }
        }

        // Instance method chain: ToList result is the receiver of the next call
        if (current.Parent is IInvocationOperation instanceParent
            && instanceParent.Instance == current)
        {
            var methodName = instanceParent.TargetMethod.Name;
            return MethodChainHelper.IsLinqMethod(methodName, includeConversionMethods: false)
                   || methodName == "ForEach";
        }

        return false;
    }

    private static bool CanParameterAcceptIEnumerable(
        ITypeSymbol parameterType,
        IInvocationOperation toListCall,
        Compilation compilation)
    {
        if (toListCall.TargetMethod.ReturnType is not INamedTypeSymbol returnType
            || returnType.TypeArguments.Length != 1)
        {
            return false;
        }

        var elementType = returnType.TypeArguments[0];

        var iEnumerableType = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        if (iEnumerableType is null)
        {
            return false;
        }

        var constructedIEnumerable = iEnumerableType.Construct(elementType);
        return compilation.HasImplicitConversion(constructedIEnumerable, parameterType);
    }

    private static string GetFriendlyTypeName(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Array)
        {
            return type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        return type.Name switch
        {
            "List" => $"List<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            "IList" => $"IList<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            "ICollection" => $"ICollection<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            "HashSet" => $"HashSet<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            "SortedSet" => $"SortedSet<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            _ => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
        };
    }
}
