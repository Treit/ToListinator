using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using ToListinator.Analyzers.Utils;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ToListToArrayMethodChainAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL007";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Avoid unnecessary ToList() or ToArray() in method chains",
        messageFormat: "Remove unnecessary {0}() call in method chain - it creates an intermediate collection that gets immediately enumerated",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ToList() and ToArray() calls in the middle of method chains create unnecessary intermediate allocations. Remove them unless the materialization is specifically needed.");

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
                ctx => AnalyzeInvocation(ctx, enumerableType),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol enumerableType)
    {
        var invocation = (IInvocationOperation)context.Operation;

        // Check if this is a parameterless ToList() or ToArray() from System.Linq.Enumerable
        if (invocation.TargetMethod.Name is not ("ToList" or "ToArray")
            || invocation.TargetMethod.Parameters.Length != 1
            || !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, enumerableType))
        {
            return;
        }

        if (!IsUnnecessaryInContext(invocation, context.Compilation))
        {
            return;
        }

        // Report diagnostic on the method call subspan (e.g., "ToList()")
        if (invocation.Syntax is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccess
            } invocationSyntax)
        {
            var location = Location.Create(
                invocationSyntax.SyntaxTree,
                TextSpan.FromBounds(memberAccess.Name.SpanStart, invocationSyntax.Span.End));

            context.ReportDiagnostic(Diagnostic.Create(Rule, location, invocation.TargetMethod.Name));
        }
    }

    private static bool IsUnnecessaryInContext(IInvocationOperation toListOrToArray, Compilation compilation)
    {
        // Walk up through implicit conversions to find the consuming operation
        IOperation current = toListOrToArray;

        // Roslyn may wrap the result in one or more implicit conversions
        // (e.g. covariance, interface adaptation). Walk past them all.
        while (current.Parent is IConversionOperation { IsImplicit: true })
        {
            current = current.Parent;
        }

        // Check if used as an argument to another invocation
        if (current.Parent is IArgumentOperation argument
            && argument.Parent is IInvocationOperation parentInvocation)
        {
            // Extension method chain: ToList/ToArray is the 'this' argument (ordinal 0)
            if (parentInvocation.TargetMethod.IsExtensionMethod
                && argument.Parameter is { Ordinal: 0 })
            {
                return IsMethodThatCanWorkWithoutMaterialization(parentInvocation.TargetMethod.Name);
            }

            // Regular argument: check if the parameter accepts IEnumerable<T>
            if (argument.Parameter is not null)
            {
                return CanParameterAcceptIEnumerable(argument.Parameter.Type, toListOrToArray, compilation);
            }
        }

        // Instance method chain: ToList/ToArray result is the receiver of the next call
        if (current.Parent is IInvocationOperation instanceParent
            && instanceParent.Instance == current)
        {
            return IsMethodThatCanWorkWithoutMaterialization(instanceParent.TargetMethod.Name);
        }

        return false;
    }

    private static bool IsMethodThatCanWorkWithoutMaterialization(string methodName)
    {
        return MethodChainHelper.IsLinqMethod(methodName);
    }

    private static bool CanParameterAcceptIEnumerable(
        ITypeSymbol parameterType,
        IInvocationOperation toListOrToArrayCall,
        Compilation compilation)
    {
        if (toListOrToArrayCall.TargetMethod.ReturnType is not INamedTypeSymbol returnType
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
}
