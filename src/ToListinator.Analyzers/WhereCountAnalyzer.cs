using System.Collections.Generic;
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

        // Check if we have Where() calls before Count()
        var whereChain = CollectWhereChain(countMemberAccess.Expression);
        if (whereChain.Count == 0)
        {
            return;
        }

        // Ensure all Where() calls have valid predicate arguments
        foreach (var whereInvocation in whereChain)
        {
            if (whereInvocation.ArgumentList.Arguments.Count != 1)
            {
                return;
            }

            var whereArgument = whereInvocation.ArgumentList.Arguments[0];
            if (whereArgument.Expression is null || !IsValidPredicate(whereArgument.Expression))
            {
                return;
            }
        }

        // Report the diagnostic on the entire Count() invocation
        var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static List<InvocationExpressionSyntax> CollectWhereChain(ExpressionSyntax expression)
    {
        var whereChain = new List<InvocationExpressionSyntax>();
        var current = expression;

        // Walk back through the chain collecting Where() calls
        while (current is InvocationExpressionSyntax invocation &&
               invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Name.Identifier.ValueText == "Where")
        {
            whereChain.Add(invocation);
            current = memberAccess.Expression;
        }

        // Reverse to get them in forward order (first to last)
        whereChain.Reverse();
        return whereChain;
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
