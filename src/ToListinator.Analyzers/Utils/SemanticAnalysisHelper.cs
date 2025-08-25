using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ToListinator.Analyzers.Utils;

/// <summary>
/// Provides semantic analysis utility methods for working with types and expressions in Roslyn analyzers.
/// </summary>
public static class SemanticAnalysisHelper
{
    /// <summary>
    /// Determines if a collection expression targets a Span&lt;T&gt; or ReadOnlySpan&lt;T&gt; type.
    /// These collection expressions don't heap allocate and compile to efficient stack-allocated data.
    /// </summary>
    /// <param name="collectionExpr">The collection expression to analyze</param>
    /// <param name="semanticModel">The semantic model for type analysis</param>
    /// <returns>True if the collection expression targets a span type; false otherwise</returns>
    public static bool IsSpanCollectionExpression(
        CollectionExpressionSyntax collectionExpr,
        SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(collectionExpr);
        var convertedType = typeInfo.ConvertedType;

        return convertedType is INamedTypeSymbol
        {
            Arity: 1,
            ContainingNamespace:
            {
                Name: "System",
                ContainingNamespace.IsGlobalNamespace: true,
            },
            Name: "Span" or "ReadOnlySpan",
        };
    }
}
