using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StaticExpressionPropertyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL005";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: "TL005",
        title: "Avoid static property expression bodies that create new instances",
        messageFormat: "Static property '{0}' uses expression body syntax that may allocate on every access. Use a getter-only property with an initializer, or convert to a method call.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        // Must be static
        if (!propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return;
        }

        // Must have expression body (=>)
        if (propertyDeclaration.ExpressionBody == null)
        {
            return;
        }

        // Analyze the expression for potentially allocating operations
        var expression = propertyDeclaration.ExpressionBody.Expression;
        if (ContainsPotentialAllocation(expression, context.SemanticModel))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                propertyDeclaration.Identifier.GetLocation(),
                propertyDeclaration.Identifier.ValueText);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool ContainsPotentialAllocation(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        return expression switch
        {
            // Object creation expressions: new Foo(), new[] { ... }, new List<T> { ... }
            ObjectCreationExpressionSyntax => true,
            ImplicitObjectCreationExpressionSyntax => true,
            ArrayCreationExpressionSyntax => true,
            ImplicitArrayCreationExpressionSyntax => true,
            CollectionExpressionSyntax => true,

            // Method invocations that may allocate
            InvocationExpressionSyntax invocation => IsAllocatingMethodCall(invocation, semanticModel),

            // Member access expressions - check the expression being accessed
            MemberAccessExpressionSyntax memberAccess => ContainsPotentialAllocation(memberAccess.Expression, semanticModel),

            // Conditional expressions - check both branches
            ConditionalExpressionSyntax conditional =>
                ContainsPotentialAllocation(conditional.WhenTrue, semanticModel) ||
                ContainsPotentialAllocation(conditional.WhenFalse, semanticModel),

            // Binary expressions - check both operands (for string concatenation, null coalescing, etc.)
            BinaryExpressionSyntax binary =>
                (binary.IsKind(SyntaxKind.AddExpression) && IsStringConcatenation(binary, semanticModel)) ||
                binary.IsKind(SyntaxKind.CoalesceExpression) && ContainsPotentialAllocation(binary.Left, semanticModel) ||
                ContainsPotentialAllocation(binary.Left, semanticModel) ||
                ContainsPotentialAllocation(binary.Right, semanticModel),

            // Parenthesized expressions - check the inner expression
            ParenthesizedExpressionSyntax parenthesized =>
                ContainsPotentialAllocation(parenthesized.Expression, semanticModel),

            // Cast expressions - check the expression being cast
            CastExpressionSyntax cast =>
                ContainsPotentialAllocation(cast.Expression, semanticModel),

            // Literals, identifiers, and other simple expressions don't allocate
            _ => false
        };
    }

    private static bool IsAllocatingMethodCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol method)
        {
            return false;
        }

        // Environment method calls are likely allocating
        if (method.ContainingType.Name == "Environment" && 
            method.ContainingType.ContainingNamespace.ToDisplayString() == "System")
        {
            return true;
        }

        // String methods that allocate
        if (method.ContainingType.SpecialType == SpecialType.System_String)
        {
            return method.Name is "Split" or "ToUpper" or "ToLower" or "Trim" or "TrimStart" or "TrimEnd" 
                or "Substring" or "Replace" or "Remove" or "Insert" or "PadLeft" or "PadRight";
        }

        // LINQ methods typically allocate
        if (method.ContainingType.Name == "Enumerable" && 
            method.ContainingType.ContainingNamespace.ToDisplayString() == "System.Linq")
        {
            return true;
        }

        // Collection methods that may allocate
        if (method.Name is "ToArray" or "ToList" or "ToDictionary" or "ToHashSet" or "ToFrozenSet"
            or "ToLookup" or "ToFrozenDictionary" or "ToImmutableArray" or "ToImmutableList"
            or "ToImmutableHashSet" or "ToImmutableDictionary" or "ToImmutableSortedSet"
            or "ToImmutableSortedDictionary")
        {
            return true;
        }

        return false;
    }

    private static bool IsStringConcatenation(BinaryExpressionSyntax binary, SemanticModel semanticModel)
    {
        var leftType = semanticModel.GetTypeInfo(binary.Left).Type;
        var rightType = semanticModel.GetTypeInfo(binary.Right).Type;
        
        return leftType?.SpecialType == SpecialType.System_String ||
               rightType?.SpecialType == SpecialType.System_String;
    }
}
