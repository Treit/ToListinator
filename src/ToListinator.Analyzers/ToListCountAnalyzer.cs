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

        context.RegisterCompilationStartAction(startContext =>
        {
            var enumerableType = startContext.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            if (enumerableType is null)
            {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                analysisContext => AnalyzeBinaryExpression(analysisContext, enumerableType),
                SyntaxKind.GreaterThanExpression,
                SyntaxKind.GreaterThanOrEqualExpression,
                SyntaxKind.NotEqualsExpression,
                SyntaxKind.LessThanExpression,
                SyntaxKind.LessThanOrEqualExpression,
                SyntaxKind.EqualsExpression);
        });
    }

    private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context, ITypeSymbol enumerableType)
    {
        var binaryExpression = (BinaryExpressionSyntax)context.Node;

        if (IsToListCountComparisonForAny(binaryExpression, context, enumerableType))
        {
            var diagnostic = Diagnostic.Create(Rule, binaryExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsToListCountComparisonForAny(
        BinaryExpressionSyntax binaryExpression,
        SyntaxNodeAnalysisContext context,
        ITypeSymbol enumerableType)
    {
        if (TryGetToListInvocation(binaryExpression.Left, out var leftInvocation) &&
            IsZeroOrOneConstant(binaryExpression.Right) &&
            IsLinqToListCall(leftInvocation!, context, enumerableType))
        {
            return IsValidCountComparisonPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Right, isLeftOperand: true);
        }

        if (TryGetToListInvocation(binaryExpression.Right, out var rightInvocation) &&
            IsZeroOrOneConstant(binaryExpression.Left) &&
            IsLinqToListCall(rightInvocation!, context, enumerableType))
        {
            return IsValidCountComparisonPattern(binaryExpression.OperatorToken.Kind(), binaryExpression.Left, isLeftOperand: false);
        }

        return false;
    }

    private static bool TryGetToListInvocation(SyntaxNode expression, out InvocationExpressionSyntax? toListInvocation)
    {
        toListInvocation = null;

        if (expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Count",
                Expression: InvocationExpressionSyntax invocation
            } &&
            invocation.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "ToList",
                Expression: not null
            })
        {
            toListInvocation = invocation;
            return true;
        }

        return false;
    }

    private static bool IsLinqToListCall(
        InvocationExpressionSyntax invocation,
        SyntaxNodeAnalysisContext context,
        ITypeSymbol enumerableType)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        return symbolInfo.Symbol is IMethodSymbol methodSymbol &&
               SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, enumerableType);
    }

    private static bool IsZeroOrOneConstant(SyntaxNode expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.Token.ValueText is "0" or "1";
    }

    private static bool IsValidCountComparisonPattern(SyntaxKind operatorKind, SyntaxNode constantNode, bool isLeftOperand)
    {
        if (constantNode is not LiteralExpressionSyntax literal)
        {
            return false;
        }

        var value = literal.Token.ValueText;

        // For left operand (collection.ToList().Count <op> constant):
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

        // For right operand (constant <op> collection.ToList().Count):
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
