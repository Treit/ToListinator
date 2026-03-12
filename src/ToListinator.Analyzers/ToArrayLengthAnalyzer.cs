using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ToArrayLengthAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL011";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Replace ToArray().Length comparison with Any()",
        messageFormat: "Avoid using ToArray().Length for existence checks. Use Any() to avoid unnecessary allocation.",
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
        if (context.Node is not BinaryExpressionSyntax binaryExpression)
        {
            return;
        }

        if (IsToArrayLengthComparison(binaryExpression, context, enumerableType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, binaryExpression.GetLocation()));
        }
    }

    private static bool IsToArrayLengthComparison(
        BinaryExpressionSyntax binaryExpression,
        SyntaxNodeAnalysisContext context,
        ITypeSymbol enumerableType)
    {
        return IsLengthComparisonAgainstZeroOrOne(binaryExpression.Left, binaryExpression.Right, binaryExpression.OperatorToken.Kind(), isLeftOperand: true, context, enumerableType) ||
               IsLengthComparisonAgainstZeroOrOne(binaryExpression.Right, binaryExpression.Left, binaryExpression.OperatorToken.Kind(), isLeftOperand: false, context, enumerableType);
    }

    private static bool IsLengthComparisonAgainstZeroOrOne(
        SyntaxNode potentialLengthExpression,
        SyntaxNode otherOperand,
        SyntaxKind operatorKind,
        bool isLeftOperand,
        SyntaxNodeAnalysisContext context,
        ITypeSymbol enumerableType)
    {
        if (!TryGetToArrayInvocation(potentialLengthExpression, out var toArrayInvocation))
        {
            return false;
        }

        if (!IsLinqToArrayCall(toArrayInvocation!, context, enumerableType))
        {
            return false;
        }

        return otherOperand is LiteralExpressionSyntax { Token.ValueText: "0" or "1" } literal &&
               IsValidLengthComparisonPattern(operatorKind, literal.Token.ValueText, isLeftOperand);
    }

    private static bool TryGetToArrayInvocation(SyntaxNode expression, out InvocationExpressionSyntax? toArrayInvocation)
    {
        toArrayInvocation = null;

        if (expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Length",
                Expression: InvocationExpressionSyntax invocation
            } &&
            invocation.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "ToArray",
                Expression: not null
            })
        {
            toArrayInvocation = invocation;
            return true;
        }

        return false;
    }

    private static bool IsLinqToArrayCall(
        InvocationExpressionSyntax invocation,
        SyntaxNodeAnalysisContext context,
        ITypeSymbol enumerableType)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        return symbolInfo.Symbol is IMethodSymbol methodSymbol &&
               SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, enumerableType);
    }

    private static bool IsValidLengthComparisonPattern(SyntaxKind operatorKind, string constantValue, bool isLeftOperand)
    {
        if (constantValue is not ("0" or "1"))
        {
            return false;
        }

        return isLeftOperand
            ? operatorKind switch
            {
                SyntaxKind.GreaterThanToken when constantValue == "0" => true,
                SyntaxKind.GreaterThanEqualsToken when constantValue == "1" => true,
                SyntaxKind.ExclamationEqualsToken when constantValue == "0" => true,
                SyntaxKind.EqualsEqualsToken when constantValue == "0" => true,
                SyntaxKind.LessThanEqualsToken when constantValue == "0" => true,
                SyntaxKind.LessThanToken when constantValue == "1" => true,
                _ => false
            }
            : operatorKind switch
            {
                SyntaxKind.LessThanToken when constantValue == "0" => true,
                SyntaxKind.LessThanEqualsToken when constantValue == "1" => true,
                SyntaxKind.ExclamationEqualsToken when constantValue == "0" => true,
                SyntaxKind.EqualsEqualsToken when constantValue == "0" => true,
                SyntaxKind.GreaterThanEqualsToken when constantValue == "0" => true,
                SyntaxKind.GreaterThanToken when constantValue == "1" => true,
                _ => false
            };
    }
}
