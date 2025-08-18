using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CountComparisonAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL009";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Replace Count() comparison with Any()",
        messageFormat: "Avoid using Count() for existence checks. Use Any() to avoid enumerating the entire sequence.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.GreaterThanExpression);
        context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.GreaterThanOrEqualExpression);
        context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.NotEqualsExpression);
        context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.LessThanExpression);
        context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.LessThanOrEqualExpression);
        context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.EqualsExpression);
    }

    private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
    {
        var binaryExpression = (BinaryExpressionSyntax)context.Node;

        // Check if this is a Count() comparison pattern that can be replaced with Any()
        if (IsCountComparisonForAny(binaryExpression, out var countInvocation))
        {
            var diagnostic = Diagnostic.Create(Rule, binaryExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsCountComparisonForAny(BinaryExpressionSyntax binaryExpression, out InvocationExpressionSyntax? countInvocation)
    {
        countInvocation = null;

        // Look for patterns like:
        // collection.Count() > 0
        // collection.Count() >= 1
        // collection.Count() != 0
        // 0 < collection.Count()
        // 1 <= collection.Count()
        // 0 != collection.Count()
        // collection.Count() == 0
        // collection.Count() <= 0
        // collection.Count() < 1
        // 0 == collection.Count()
        // 0 >= collection.Count()
        // 1 > collection.Count()

        var leftIsCount = IsCountInvocation(binaryExpression.Left);
        var rightIsCount = IsCountInvocation(binaryExpression.Right);

        if (leftIsCount && IsZeroOrOneConstant(binaryExpression.Right))
        {
            countInvocation = (InvocationExpressionSyntax)binaryExpression.Left;
            return IsValidCountComparisonPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Right, isLeftOperand: true);
        }

        if (rightIsCount && IsZeroOrOneConstant(binaryExpression.Left))
        {
            countInvocation = (InvocationExpressionSyntax)binaryExpression.Right;
            return IsValidCountComparisonPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Left, isLeftOperand: false);
        }

        return false;
    }

    private static bool IsCountInvocation(SyntaxNode expression)
    {
        return expression is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Count"
            },
            ArgumentList.Arguments.Count: 0 // Only parameterless Count() - Count(predicate) is handled by WhereCountAnalyzer (TL006)
        };
    }

    private static bool IsZeroOrOneConstant(SyntaxNode expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.Token.ValueText is "0" or "1";
    }

    private static bool IsValidCountComparisonPattern(SyntaxKind operatorKind, SyntaxNode constantNode, bool isLeftOperand)
    {
        if (constantNode is not LiteralExpressionSyntax literal)
            return false;

        var value = literal.Token.ValueText;

        // For left operand (collection.Count() <op> constant):
        if (isLeftOperand)
        {
            return operatorKind switch
            {
                // Existence patterns: > 0, >= 1, != 0 all mean "has any elements"
                SyntaxKind.GreaterThanToken when value == "0" => true,
                SyntaxKind.GreaterThanEqualsToken when value == "1" => true,
                SyntaxKind.ExclamationEqualsToken when value == "0" => true,
                // Non-existence patterns: == 0, <= 0, < 1 all mean "has no elements"
                SyntaxKind.EqualsEqualsToken when value == "0" => true,
                SyntaxKind.LessThanEqualsToken when value == "0" => true,
                SyntaxKind.LessThanToken when value == "1" => true,
                _ => false
            };
        }

        // For right operand (constant <op> collection.Count()):
        return operatorKind switch
        {
            // Existence patterns: 0 <, 1 <=, 0 != all mean "has any elements"
            SyntaxKind.LessThanToken when value == "0" => true,
            SyntaxKind.LessThanEqualsToken when value == "1" => true,
            SyntaxKind.ExclamationEqualsToken when value == "0" => true,
            // Non-existence patterns: 0 ==, 0 >=, 1 > all mean "has no elements"
            SyntaxKind.EqualsEqualsToken when value == "0" => true,
            SyntaxKind.GreaterThanEqualsToken when value == "0" => true,
            SyntaxKind.GreaterThanToken when value == "1" => true,
            _ => false
        };
    }
}
