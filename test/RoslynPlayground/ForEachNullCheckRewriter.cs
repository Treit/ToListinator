using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

class ForEachNullCheckRewriter : CSharpSyntaxRewriter
{
    /*
     Rewrite code like this:

        // Iterate over the items
        foreach (var str in list ?? new List<string>()) // Check for null
        {
            Console.WriteLine(str); // Print it out
        }

     Into this:
        // Check for null
        if (list != null)
        {
            // Iterate over the items
            foreach (var str in list)
            {
                Console.WriteLine(str); // Print it out
            }
        }
    */
    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        if (!IsCoalesceWithEmptyFallback(node))
        {
            return null;
        }

        if (node.Expression is BinaryExpressionSyntax binExpr &&
            binExpr.IsKind(SyntaxKind.CoalesceExpression))
        {
            var listExpr = binExpr.Left;
            var fallbackExpr = binExpr.Right;

            // Extract comments or other trivia
            var originalLeadingTrivia = node.ForEachKeyword.LeadingTrivia;
            var originalTrailingTrivia = node.CloseParenToken.TrailingTrivia;

            var newForEachKeyword = node.ForEachKeyword.WithLeadingTrivia(originalLeadingTrivia);

            var newForeach = node
                .WithExpression(listExpr.WithoutTrivia())
                .WithForEachKeyword(newForEachKeyword)
                .WithCloseParenToken(node.CloseParenToken.WithTrailingTrivia())
                .WithTrailingTrivia();

            var ifStmt = IfStatement(
                BinaryExpression(SyntaxKind.NotEqualsExpression,
                    listExpr.WithoutTrivia(),
                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                Block(SingletonList<StatementSyntax>(newForeach)))
                .WithLeadingTrivia(originalTrailingTrivia);

            ifStmt = ifStmt.NormalizeWhitespace();

            return ifStmt;
        }

        return base.VisitForEachStatement(node);
    }

    public static bool IsCoalesceWithEmptyFallback(ForEachStatementSyntax forEach)
    {
        if (forEach.Expression is not BinaryExpressionSyntax binaryExpr ||
            binaryExpr.Kind() != SyntaxKind.CoalesceExpression)
        {
            return false;
        }

        ExpressionSyntax fallbackExpr = binaryExpr.Right;

        // Case 1: new Something<T>() with no args (e.g. new List<string>())
        if (fallbackExpr is ObjectCreationExpressionSyntax objectCreation &&
            objectCreation.ArgumentList?.Arguments.Count == 0)
        {
            return true;
        }

        // Case 2: Array.Empty<T>()
        if (fallbackExpr is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "Empty")
        {
            return true;
        }

        // Case 3: ImmutableArray<T>.Empty or similar static .Empty property
        if (fallbackExpr is MemberAccessExpressionSyntax propertyAccess &&
            propertyAccess.Name.Identifier.Text == "Empty")
        {
            return true;
        }

        // Case 4: new T[0] or new T[] { }
        if (fallbackExpr is ArrayCreationExpressionSyntax arrayCreation)
        {
            if (arrayCreation.Initializer is not null &&
                arrayCreation.Initializer.Expressions.Count == 0)
            {
                return true;
            }

            if (arrayCreation.Type?.RankSpecifiers.FirstOrDefault() is ArrayRankSpecifierSyntax rank &&
                rank.Sizes.Count == 1 &&
                rank.Sizes[0] is LiteralExpressionSyntax literal &&
                literal.Token.ValueText == "0")
            {
                return true;
            }
        }

        return false;
    }
}
