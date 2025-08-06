using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Look for Count() calls
        if (invocation is not
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "Count"
                } countMemberAccess,
                ArgumentList.Arguments.Count: 0 // Count() with no arguments
            })
        {
            return;
        }

        // Check if the expression before Count() is a Where() call
        if (countMemberAccess.Expression is not InvocationExpressionSyntax whereInvocation)
        {
            return;
        }

        if (whereInvocation is not
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "Where"
                } whereMemberAccess,
                ArgumentList.Arguments.Count: 1 // Where() with exactly one argument (the predicate)
            })
        {
            return;
        }

        // Ensure the Where() call has a valid predicate argument
        var whereArgument = whereInvocation.ArgumentList.Arguments[0];
        if (whereArgument.Expression is null)
        {
            return;
        }

        // Valid patterns:
        // - Lambda expressions: x => x.Property > 0
        // - Method groups: SomeMethod
        // - Anonymous methods: delegate(Type x) { return x.Property > 0; }
        if (!IsValidPredicate(whereArgument.Expression))
        {
            return;
        }

        // Report the diagnostic on the entire Count() invocation
        var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsValidPredicate(ExpressionSyntax expression)
    {
        return expression switch
        {
            // Lambda expressions: x => condition
            SimpleLambdaExpressionSyntax => true,
            ParenthesizedLambdaExpressionSyntax => true,
            
            // Method groups: SomeMethod
            IdentifierNameSyntax => true,
            MemberAccessExpressionSyntax => true,
            
            // Anonymous methods: delegate(Type x) { return condition; }
            AnonymousMethodExpressionSyntax => true,
            
            _ => false
        };
    }
}
