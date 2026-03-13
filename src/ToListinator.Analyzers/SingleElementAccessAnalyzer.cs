using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SingleElementAccessAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL008";

    public const string AccessKindProperty = "AccessKind";
    public const string AccessKindMethod = "Method";
    public const string AccessKindIndexer = "Indexer";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Avoid ToList/ToArray for single element access",
        messageFormat: "Avoid using ToList()/ToArray() before single element access. The entire collection is materialized unnecessarily.",
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
                ctx => AnalyzeInvocation(ctx, enumerableType),
                OperationKind.Invocation);

            startContext.RegisterOperationAction(
                ctx => AnalyzePropertyReference(ctx, enumerableType),
                OperationKind.PropertyReference);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol enumerableType)
    {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name is not ("First" or "Last" or "FirstOrDefault" or "LastOrDefault"
            or "Single" or "SingleOrDefault" or "ElementAt" or "ElementAtOrDefault"))
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, enumerableType))
        {
            return;
        }

        var innerInvocation = GetReceiverInvocation(invocation);

        if (innerInvocation is null
            || innerInvocation.TargetMethod.Name is not ("ToList" or "ToArray")
            || !SymbolEqualityComparer.Default.Equals(innerInvocation.TargetMethod.ContainingType, enumerableType))
        {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(AccessKindProperty, AccessKindMethod);

        context.ReportDiagnostic(
            Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), properties.ToImmutable()));
    }

    private static IInvocationOperation? GetReceiverInvocation(IInvocationOperation invocation)
    {
        if (invocation.Instance is IInvocationOperation instanceInvocation)
        {
            return instanceInvocation;
        }

        // For extension methods called with dot syntax (e.g., items.ToList().First()),
        // the receiver is passed as the first argument, potentially wrapped in a conversion.
        // We exclude the static form (Enumerable.First(items.ToList())) by verifying
        // the syntax receiver is an invocation, not a type name.
        if (invocation.TargetMethod.IsExtensionMethod
            && invocation.Arguments.Length > 0
            && invocation.Syntax is InvocationExpressionSyntax
               {
                   Expression: MemberAccessExpressionSyntax
                   {
                       Expression: InvocationExpressionSyntax
                   }
               })
        {
            IOperation argValue = invocation.Arguments[0].Value;

            // Roslyn may wrap the receiver in one or more implicit conversions
            // (e.g. covariance, interface adaptation). Peel them all off.
            while (argValue is IConversionOperation { IsImplicit: true } conversion)
            {
                argValue = conversion.Operand;
            }

            return argValue as IInvocationOperation;
        }

        return null;
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, INamedTypeSymbol enumerableType)
    {
        var propRef = (IPropertyReferenceOperation)context.Operation;

        // Pattern: source.ToList()[index]
        if (propRef is not
            {
                Property.IsIndexer: true,
                Instance: IInvocationOperation
                {
                    TargetMethod: { Name: "ToList" } toListMethod
                }
            }
            || !SymbolEqualityComparer.Default.Equals(toListMethod.ContainingType, enumerableType))
        {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(AccessKindProperty, AccessKindIndexer);

        context.ReportDiagnostic(
            Diagnostic.Create(Rule, propRef.Syntax.GetLocation(), properties.ToImmutable()));
    }

}
