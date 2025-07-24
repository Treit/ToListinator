using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ToListCountAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL003";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Replace ToList().Count comparison with Any()",
        messageFormat: "Avoid using ToList().Count for existence checks. Use Any() to avoid unnecessary allocation.",
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

        // Check if this is a ToList().Count comparison pattern that can be replaced with Any()
        if (IsToListCountComparisonForAny(binaryExpression, out var toListCountExpression))
        {
            var diagnostic = Diagnostic.Create(Rule, binaryExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsToListCountComparisonForAny(BinaryExpressionSyntax binaryExpression, out MemberAccessExpressionSyntax? toListCountExpression)
    {
        toListCountExpression = null;

        // Look for patterns like:
        // collection.ToList().Count > 0
        // collection.ToList().Count >= 1
        // collection.ToList().Count != 0
        // 0 < collection.ToList().Count
        // 1 <= collection.ToList().Count
        // 0 != collection.ToList().Count

        var leftIsToListCount = IsToListCountExpression(binaryExpression.Left);
        var rightIsToListCount = IsToListCountExpression(binaryExpression.Right);

        if (leftIsToListCount && IsZeroOrOneConstant(binaryExpression.Right))
        {
            toListCountExpression = (MemberAccessExpressionSyntax)binaryExpression.Left;
            return IsExistenceCheckPattern(binaryExpression.OperatorToken.Kind(), isLeftOperand: true);
        }

        if (rightIsToListCount && IsZeroOrOneConstant(binaryExpression.Left))
        {
            toListCountExpression = (MemberAccessExpressionSyntax)binaryExpression.Right;
            return IsExistenceCheckPattern(binaryExpression.OperatorToken.Kind(), isLeftOperand: false);
        }

        return false;
    }

    private static bool IsToListCountExpression(SyntaxNode expression)
    {
        return expression is MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "Count",
            Expression: InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "ToList"
                }
            }
        };
    }

    private static bool IsZeroOrOneConstant(SyntaxNode expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.Token.ValueText is "0" or "1";
    }

    private static bool IsExistenceCheckPattern(SyntaxKind operatorKind, bool isLeftOperand)
    {
        // For left operand (collection.ToList().Count <op> constant):
        // > 0, >= 1, != 0 all mean "has any elements"
        if (isLeftOperand)
        {
            return operatorKind is SyntaxKind.GreaterThanToken or
                                  SyntaxKind.GreaterThanEqualsToken or
                                  SyntaxKind.ExclamationEqualsToken;
        }

        // For right operand (constant <op> collection.ToList().Count):
        // 0 <, 1 <=, 0 != all mean "has any elements"
        return operatorKind is SyntaxKind.LessThanToken or
                              SyntaxKind.LessThanEqualsToken or
                              SyntaxKind.ExclamationEqualsToken;
    }
}
