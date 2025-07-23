using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation is not
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "Select"
                } memberAccess,
                ArgumentList.Arguments.Count: 1
            })
        {
            return;
        }

        var lambdaExpression = invocation.ArgumentList.Arguments[0].Expression;
        if (!IsIdentityLambda(lambdaExpression))
        {
            return;
        }

        // Verify this is System.Linq.Enumerable.Select
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is not IMethodSymbol method ||
            method.ContainingType?.ToDisplayString() != "System.Linq.Enumerable")
        {
            return;
        }

        // Determine highlight location based on whether this is a chained call
        var location = invocation.Parent is MemberAccessExpressionSyntax parentMemberAccess &&
                      parentMemberAccess.Expression == invocation
            ? CreateChainedLocation(memberAccess, invocation)
            : invocation.GetLocation();

        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
    }

    private static Location CreateChainedLocation(MemberAccessExpressionSyntax memberAccess, InvocationExpressionSyntax invocation)
    {
        var startPos = memberAccess.Name.GetLocation().SourceSpan.Start;
        var endPos = invocation.GetLocation().SourceSpan.End;
        var span = new Microsoft.CodeAnalysis.Text.TextSpan(startPos, endPos - startPos);
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
