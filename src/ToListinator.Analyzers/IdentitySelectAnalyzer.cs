using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IdentitySelectAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL002";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Avoid identity Select operation",
        messageFormat: "Avoid using Select(x => x) which performs no transformation. Remove the unnecessary Select call.",
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

        if (invocation.TargetMethod.Name is not "Select"
            || !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, enumerableType))
        {
            return;
        }

        // Drop to syntax for identity lambda check and to exclude static form
        if (invocation.Syntax is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccess,
                ArgumentList.Arguments: [{ Expression: { } lambdaExpression }]
            } invocationSyntax)
        {
            return;
        }

        if (!IsIdentityLambda(lambdaExpression))
        {
            return;
        }

        // Determine highlight location based on whether this is a chained call
        var location = invocationSyntax.Parent is MemberAccessExpressionSyntax parentMemberAccess &&
                      parentMemberAccess.Expression == invocationSyntax
            ? CreateChainedLocation(memberAccess, invocationSyntax)
            : invocationSyntax.GetLocation();

        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
    }

    private static Location CreateChainedLocation(MemberAccessExpressionSyntax memberAccess, InvocationExpressionSyntax invocation)
    {
        var startPos = memberAccess.Name.GetLocation().SourceSpan.Start;
        var endPos = invocation.GetLocation().SourceSpan.End;
        var span = new TextSpan(startPos, endPos - startPos);
        return Location.Create(invocation.SyntaxTree, span);
    }

    private static bool IsIdentityLambda(ExpressionSyntax expression)
    {
        return expression switch
        {
            // Simple lambda: x => x
            SimpleLambdaExpressionSyntax simpleLambda when
                simpleLambda.Body is IdentifierNameSyntax bodyIdentifier &&
                bodyIdentifier.Identifier.ValueText == simpleLambda.Parameter.Identifier.ValueText => true,

            // Parenthesized lambda: (x) => x
            ParenthesizedLambdaExpressionSyntax parenLambda when
                parenLambda.ParameterList.Parameters.Count == 1 &&
                parenLambda.Body is IdentifierNameSyntax bodyIdentifier &&
                bodyIdentifier.Identifier.ValueText == parenLambda.ParameterList.Parameters[0].Identifier.ValueText => true,

            _ => false
        };
    }
}
