using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ArrayEmptyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL006";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Replace empty array creation with Array.Empty()",
        messageFormat: "Replace 'new {0}' with 'Array.Empty<{1}>()', which avoids unnecessary allocation and clearly expresses intent.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeArrayCreation, SyntaxKind.ArrayCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitArrayCreation, SyntaxKind.ImplicitArrayCreationExpression);
    }

    private static void AnalyzeArrayCreation(SyntaxNodeAnalysisContext context)
    {
        var arrayCreation = (ArrayCreationExpressionSyntax)context.Node;

        if (!IsEmptyArrayCreation(arrayCreation))
        {
            return;
        }

        var elementType = GetElementTypeDisplayString(arrayCreation.Type, context.SemanticModel);
        if (elementType == null)
        {
            return;
        }

        var currentCode = arrayCreation.ToString();
        var diagnostic = Diagnostic.Create(Rule, arrayCreation.GetLocation(), currentCode, elementType);
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeImplicitArrayCreation(SyntaxNodeAnalysisContext context)
    {
        var implicitArrayCreation = (ImplicitArrayCreationExpressionSyntax)context.Node;

        if (!IsEmptyImplicitArrayCreation(implicitArrayCreation))
        {
            return;
        }

        // For implicit array creation, we need to get the type from semantic model
        var typeInfo = context.SemanticModel.GetTypeInfo(implicitArrayCreation);
        if (typeInfo.Type is not IArrayTypeSymbol arrayType)
        {
            // Can't determine the type - this might be a compiler error scenario like new[] {}
            return;
        }

        var elementType = arrayType.ElementType.ToDisplayString();
        var currentCode = implicitArrayCreation.ToString();
        var diagnostic = Diagnostic.Create(Rule, implicitArrayCreation.GetLocation(), currentCode, elementType);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsEmptyArrayCreation(ArrayCreationExpressionSyntax arrayCreation)
    {
        // Check for new T[0] pattern
        if (arrayCreation.Type.RankSpecifiers.Count == 1)
        {
            var rankSpecifier = arrayCreation.Type.RankSpecifiers[0];
            if (rankSpecifier.Sizes.Count == 1 && 
                rankSpecifier.Sizes[0] is LiteralExpressionSyntax literal &&
                literal.Token.ValueText == "0")
            {
                return true;
            }
        }

        // Check for new T[] {} pattern  
        if (arrayCreation.Initializer != null && 
            arrayCreation.Initializer.Expressions.Count == 0)
        {
            return true;
        }

        return false;
    }

    private static bool IsEmptyImplicitArrayCreation(ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
    {
        // Check for new[] {} pattern
        return implicitArrayCreation.Initializer != null && 
               implicitArrayCreation.Initializer.Expressions.Count == 0;
    }

    private static string? GetElementTypeDisplayString(ArrayTypeSyntax arrayType, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(arrayType.ElementType);
        return typeInfo.Type?.ToDisplayString();
    }
}