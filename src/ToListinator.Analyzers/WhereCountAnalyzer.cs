using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WhereCountAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL006";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Use Count(predicate) instead of Where(predicate).Count()",
        messageFormat: "Use Count(predicate) instead of Where(predicate).Count() for better performance",
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
                ctx => AnalyzeInvocation(ctx, enumerableType),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol enumerableType)
    {
        var invocation = (IInvocationOperation)context.Operation;

        // Must be Enumerable.Count() with no predicate parameter (only the extension 'this' param)
        if (invocation.TargetMethod.Name is not "Count"
            || invocation.TargetMethod.Parameters.Length != 1
            || !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, enumerableType))
        {
            return;
        }

        // Collect Where() calls in the receiver chain
        var whereChain = CollectWhereChain(invocation, enumerableType);
        if (whereChain.Count == 0)
        {
            return;
        }

        // Validate all Where() calls have valid predicates
        foreach (var whereInvocation in whereChain)
        {
            if (!HasValidPredicate(whereInvocation))
            {
                return;
            }
        }

        var diagnostic = Diagnostic.Create(Rule, invocation.Syntax.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static List<IInvocationOperation> CollectWhereChain(
        IInvocationOperation startInvocation,
        INamedTypeSymbol enumerableType)
    {
        var whereChain = new List<IInvocationOperation>();
        var current = GetReceiverInvocation(startInvocation);

        while (current is not null
               && current.TargetMethod.Name is "Where"
               && SymbolEqualityComparer.Default.Equals(current.TargetMethod.ContainingType, enumerableType))
        {
            whereChain.Add(current);
            current = GetReceiverInvocation(current);
        }

        return whereChain;
    }

    private static bool HasValidPredicate(IInvocationOperation whereInvocation)
    {
        // Validate via syntax: dot-syntax Where() must have exactly 1 visible argument
        if (whereInvocation.Syntax is not InvocationExpressionSyntax
            {
                ArgumentList.Arguments: [{ Expression: { } predicateExpression }]
            })
        {
            return false;
        }

        return IsValidPredicate(predicateExpression);
    }

    private static IInvocationOperation? GetReceiverInvocation(IInvocationOperation invocation)
    {
        if (invocation.Instance is IInvocationOperation instanceInvocation)
        {
            return instanceInvocation;
        }

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

    private static bool IsValidPredicate(ExpressionSyntax expression)
    {
        return expression switch
        {
            SimpleLambdaExpressionSyntax => true,
            ParenthesizedLambdaExpressionSyntax => true,
            IdentifierNameSyntax => true,
            MemberAccessExpressionSyntax => true,
            AnonymousMethodExpressionSyntax => true,
            _ => false
        };
    }
}
