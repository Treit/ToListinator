using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ToListToArrayMethodChainAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL007";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Avoid unnecessary ToList() or ToArray() in method chains",
        messageFormat: "Remove unnecessary {0}() call in method chain - it creates an intermediate collection that gets immediately enumerated",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ToList() and ToArray() calls in the middle of method chains create unnecessary intermediate allocations. Remove them unless the materialization is specifically needed.");

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
        var invocationExpression = (InvocationExpressionSyntax)context.Node;

        // Check if this is a ToList() or ToArray() call
        if (!IsToListOrToArrayCall(invocationExpression, out var methodName))
            return;

        // Check if this ToList/ToArray is in the middle of a method chain
        if (IsUnnecessaryInMethodChain(invocationExpression, context.SemanticModel))
        {
            // Report diagnostic on the method call itself (e.g., "ToList()"), not the entire expression
            var memberAccess = (MemberAccessExpressionSyntax)invocationExpression.Expression;
            var methodLocation = Location.Create(
                invocationExpression.SyntaxTree,
                Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(
                    memberAccess.Name.SpanStart,
                    invocationExpression.Span.End));

            var diagnostic = Diagnostic.Create(Rule, methodLocation, methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsToListOrToArrayCall(InvocationExpressionSyntax invocation, out string methodName)
    {
        methodName = string.Empty;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.ValueText is "ToList" or "ToArray" &&
            invocation.ArgumentList.Arguments.Count == 0) // No arguments - we're looking for parameterless ToList()/ToArray()
        {
            methodName = memberAccess.Name.Identifier.ValueText;
            return true;
        }

        return false;
    }

    private static bool IsUnnecessaryInMethodChain(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        // Look at the parent node to see if this ToList/ToArray result is immediately used in another method call
        var parent = invocation.Parent;

        // Walk up to find the member access expression that uses this invocation
        if (parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == invocation)
        {
            // Check if the member access is part of another invocation
            if (memberAccess.Parent is InvocationExpressionSyntax parentInvocation)
            {
                // This is a chain like items.ToList().Select(...) where:
                // - invocation is "items.ToList()"
                // - memberAccess is "items.ToList().Select"
                // - parentInvocation is "items.ToList().Select(...)"

                var chainedMethodName = memberAccess.Name.Identifier.ValueText;
                return IsMethodThatCanWorkWithoutMaterialization(chainedMethodName);
            }
        }

        // Check if this is used as an argument to a method that accepts IEnumerable<T>
        if (parent is ArgumentSyntax argument)
        {
            // Walk up to find the invocation that contains this argument
            var argumentParent = argument.Parent;
            while (argumentParent != null && argumentParent is not InvocationExpressionSyntax)
            {
                argumentParent = argumentParent.Parent;
            }

            if (argumentParent is InvocationExpressionSyntax parentInvocation)
            {
                // Use semantic analysis to determine if the parameter accepts IEnumerable<T>
                return CanParameterAcceptIEnumerable(parentInvocation, argument, invocation, semanticModel);
            }
        }

        return false;
    }

    private static readonly ImmutableHashSet<string> MethodsWorkingWithEnumerable = ImmutableHashSet.Create(
        // LINQ query methods
        "Select", "Where", "SelectMany", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending",
        "GroupBy", "Join", "GroupJoin", "Concat", "Union", "Intersect", "Except", "Distinct",
        "Skip", "Take", "SkipWhile", "TakeWhile", "Reverse", "Cast", "OfType", "Zip",

        // LINQ terminal methods
        "Contains", "Any", "All", "First", "FirstOrDefault", "Last", "LastOrDefault",
        "Single", "SingleOrDefault", "ElementAt", "ElementAtOrDefault", "Count", "LongCount",
        "Sum", "Min", "Max", "Average", "Aggregate",

        // Enumerable conversion methods
        "ToList", "ToArray", "ToDictionary", "ToLookup", "ToHashSet"
    );

    private static bool IsMethodThatCanWorkWithoutMaterialization(string methodName)
    {
        return MethodsWorkingWithEnumerable.Contains(methodName);
    }

    private static bool CanParameterAcceptIEnumerable(
        InvocationExpressionSyntax parentInvocation,
        ArgumentSyntax argument,
        InvocationExpressionSyntax toListOrToArrayCall,
        SemanticModel semanticModel)
    {
        // Get the symbol information for the method being called
        var symbolInfo = semanticModel.GetSymbolInfo(parentInvocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        // Find which parameter position this argument corresponds to
        var argumentList = argument.Parent as ArgumentListSyntax;
        if (argumentList == null)
            return false;

        var argumentIndex = argumentList.Arguments.IndexOf(argument);
        if (argumentIndex < 0 || argumentIndex >= methodSymbol.Parameters.Length)
            return false;

        var parameter = methodSymbol.Parameters[argumentIndex];
        var parameterType = parameter.Type;

        // Get the element type from the ToList/ToArray call
        var toListSymbolInfo = semanticModel.GetSymbolInfo(toListOrToArrayCall);
        if (toListSymbolInfo.Symbol is not IMethodSymbol toListMethod)
            return false;

        if (toListMethod.ReturnType is not INamedTypeSymbol returnType || 
            returnType.TypeArguments.Length != 1)
            return false;

        var elementType = returnType.TypeArguments[0];

        // Check if the parameter can accept IEnumerable<elementType>
        return CanTypeAcceptIEnumerable(parameterType, elementType, semanticModel.Compilation);
    }

    private static bool CanTypeAcceptIEnumerable(ITypeSymbol parameterType, ITypeSymbol elementType, Compilation compilation)
    {
        // Get IEnumerable<T> type symbol
        var iEnumerableType = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        if (iEnumerableType == null)
            return false;

        // Construct IEnumerable<elementType>
        var constructedIEnumerable = iEnumerableType.Construct(elementType);

        // Check if parameter type is assignable from IEnumerable<elementType>
        // This handles inheritance, interface implementation, and variance
        var conversion = compilation.HasImplicitConversion(constructedIEnumerable, parameterType);
        
        return conversion;
    }
}
