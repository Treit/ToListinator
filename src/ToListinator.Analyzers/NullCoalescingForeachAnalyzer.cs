using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullCoalescingForeachAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL004";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: "TL004",
        title: "Avoid foreach with null coalescing to empty collection",
        messageFormat: "Avoid using null coalescing operator (??) with empty collection in foreach. Check for null instead to avoid unnecessary allocation.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeForeachStatement, SyntaxKind.ForEachStatement);
    }

    private static void AnalyzeForeachStatement(SyntaxNodeAnalysisContext context)
    {
        var foreachStatement = (ForEachStatementSyntax)context.Node;

        if (foreachStatement.Expression is not BinaryExpressionSyntax binaryExpr ||
            !binaryExpr.IsKind(SyntaxKind.CoalesceExpression))
        {
            return;
        }

        var fallbackExpression = binaryExpr.Right;

        if (!IsEmptyCollectionExpression(fallbackExpression))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, binaryExpr.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsEmptyCollectionExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            // Case 1: new Something<T>() with no args (e.g. new List<string>())
            ObjectCreationExpressionSyntax objectCreation
                when objectCreation.ArgumentList?.Arguments.Count == 0 => true,

            // Case 2: Array.Empty<T>()
            InvocationExpressionSyntax invocation
                when invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                     memberAccess.Name.Identifier.Text == "Empty" => true,

            // Case 3: ImmutableArray<T>.Empty or similar static .Empty property
            MemberAccessExpressionSyntax propertyAccess
                when propertyAccess.Name.Identifier.Text == "Empty" => true,

            // Case 4: new T[0] or new T[] { }
            ArrayCreationExpressionSyntax arrayCreation
                when IsEmptyArray(arrayCreation) => true,

            // Case 5: Enumerable.Empty<T>()
            InvocationExpressionSyntax enumerableEmpty
                when enumerableEmpty.Expression is MemberAccessExpressionSyntax enumAccess &&
                     enumAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Enumerable" } &&
                     enumAccess.Name.Identifier.Text == "Empty" => true,

            _ => false
        };
    }

    private static bool IsEmptyArray(ArrayCreationExpressionSyntax arrayCreation)
    {
        // Case: new T[] { } (empty initializer)
        if (arrayCreation.Initializer is not null &&
            arrayCreation.Initializer.Expressions.Count == 0)
        {
            return true;
        }

        // Case: new T[0] (zero-length array)
        if (arrayCreation.Type?.RankSpecifiers.FirstOrDefault() is ArrayRankSpecifierSyntax rank &&
            rank.Sizes.Count == 1 &&
            rank.Sizes[0] is LiteralExpressionSyntax literal &&
            literal.Token.ValueText == "0")
        {
            return true;
        }

        return false;
    }
}
