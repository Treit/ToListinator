using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ToListinator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnnecessaryToListAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TL010";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Remove unnecessary ToList() call on already materialized collection",
        messageFormat: "Avoid calling ToList() on '{0}' which is already materialized. Use the collection directly with LINQ operations.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Calling ToList() on collections that are already materialized (List<T>, Array, etc.) before LINQ operations creates an unnecessary copy and wastes memory and CPU.");

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

            startContext.RegisterSyntaxNodeAction(analysisContext =>
            {
                AnalyzeInvocation(analysisContext, enumerableType);
            }, SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, ITypeSymbol enumerableType)
    {
        var invocationExpression = (InvocationExpressionSyntax)context.Node;

        // Check if this is a ToList() call
        if (!IsToListCall(invocationExpression))
        {
            return;
        }

        // Get the source expression (what ToList() is being called on)
        var memberAccess = (MemberAccessExpressionSyntax)invocationExpression.Expression;
        var sourceExpression = memberAccess.Expression;

        // Check if the source is already a materialized collection
        var sourceTypeInfo = context.SemanticModel.GetTypeInfo(sourceExpression);
        if (sourceTypeInfo.Type is null || !IsMaterializedCollection(sourceTypeInfo.Type))
        {
            return;
        }

        // Check if this ToList() call is followed by LINQ operations
        if (!IsFollowedByLinqOperations(invocationExpression, context.SemanticModel))
        {
            return;
        }

        // Check if this might be a defensive copy that's actually needed
        if (MightNeedDefensiveCopy(invocationExpression, context.SemanticModel))
        {
            return;
        }

        // Report the diagnostic
        var sourceTypeName = GetFriendlyTypeName(sourceTypeInfo.Type);
        var diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation(), sourceTypeName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsToListCall(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Name.Identifier.ValueText == "ToList" &&
               invocation.ArgumentList.Arguments.Count == 0; // Parameterless ToList()
    }

    private static bool IsMaterializedCollection(ITypeSymbol type)
    {
        // Check for concrete collection types that are already materialized
        var typeName = type.ToDisplayString();

        // Handle generic types by getting the original definition
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var originalDefinition = namedType.OriginalDefinition.ToDisplayString();
            return originalDefinition switch
            {
                "System.Collections.Generic.List<T>" => true,
                "System.Collections.Generic.IList<T>" => true,
                "System.Collections.Generic.ICollection<T>" => true,
                "System.Collections.ObjectModel.Collection<T>" => true,
                "System.Collections.ObjectModel.ObservableCollection<T>" => true,
                "System.Collections.Generic.HashSet<T>" => true,
                "System.Collections.Generic.SortedSet<T>" => true,
                _ => false
            };
        }

        // Handle arrays
        if (type.TypeKind == TypeKind.Array)
        {
            return true;
        }

        // Handle non-generic collections
        return typeName switch
        {
            "System.Collections.ArrayList" => true,
            "System.Collections.Queue" => true,
            "System.Collections.Stack" => true,
            _ => false
        };
    }

    private static bool IsFollowedByLinqOperations(InvocationExpressionSyntax toListCall, SemanticModel semanticModel)
    {
        // Check if the ToList() call is immediately used in a LINQ operation
        var parent = toListCall.Parent;

        // Look for member access that uses this ToList() result
        if (parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == toListCall)
        {
            // Check if the member access is part of another invocation
            if (memberAccess.Parent is InvocationExpressionSyntax parentInvocation)
            {
                var methodName = memberAccess.Name.Identifier.ValueText;
                return IsLinqMethod(methodName);
            }
        }

        // Check if used as argument to a method that accepts IEnumerable<T>
        if (parent is ArgumentSyntax argument)
        {
            return IsPassedToMethodAcceptingIEnumerable(argument, toListCall, semanticModel);
        }

        // Check if assigned to a variable that's later used with LINQ
        if (parent is EqualsValueClauseSyntax equalsValue)
        {
            // For now, we'll be conservative and not flag assignments
            // This could be enhanced in future versions
            return false;
        }

        return false;
    }

    private static readonly ImmutableHashSet<string> LinqMethods = ImmutableHashSet.Create(
        // LINQ query methods
        "Select", "Where", "SelectMany", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending",
        "GroupBy", "Join", "GroupJoin", "Concat", "Union", "Intersect", "Except", "Distinct",
        "Skip", "Take", "SkipWhile", "TakeWhile", "Reverse", "Cast", "OfType", "Zip",

        // LINQ aggregation methods
        "Contains", "Any", "All", "First", "FirstOrDefault", "Last", "LastOrDefault",
        "Single", "SingleOrDefault", "ElementAt", "ElementAtOrDefault", "Count", "LongCount",
        "Sum", "Min", "Max", "Average", "Aggregate",

        // Additional LINQ-like methods
        "ForEach" // List<T>.ForEach, though this is discouraged
    );

    private static bool IsLinqMethod(string methodName)
    {
        return LinqMethods.Contains(methodName);
    }

    private static bool IsPassedToMethodAcceptingIEnumerable(
        ArgumentSyntax argument,
        InvocationExpressionSyntax toListCall,
        SemanticModel semanticModel)
    {
        // Walk up to find the invocation that contains this argument
        var argumentParent = argument.Parent;
        while (argumentParent != null && argumentParent is not InvocationExpressionSyntax)
        {
            argumentParent = argumentParent.Parent;
        }

        if (argumentParent is not InvocationExpressionSyntax parentInvocation)
        {
            return false;
        }

        // Get the symbol information for the method being called
        var symbolInfo = semanticModel.GetSymbolInfo(parentInvocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        // Find which parameter position this argument corresponds to
        if (argument.Parent is not ArgumentListSyntax argumentList)
        {
            return false;
        }

        var argumentIndex = argumentList.Arguments.IndexOf(argument);
        if (argumentIndex < 0 || argumentIndex >= methodSymbol.Parameters.Length)
        {
            return false;
        }

        var parameter = methodSymbol.Parameters[argumentIndex];
        var parameterType = parameter.Type;

        // Get the element type from the ToList call
        var toListSymbolInfo = semanticModel.GetSymbolInfo(toListCall);
        if (toListSymbolInfo.Symbol is not IMethodSymbol toListMethod)
        {
            return false;
        }

        if (toListMethod.ReturnType is not INamedTypeSymbol returnType ||
            returnType.TypeArguments.Length != 1)
        {
            return false;
        }

        var elementType = returnType.TypeArguments[0];

        // Check if the parameter can accept IEnumerable<elementType>
        return CanTypeAcceptIEnumerable(parameterType, elementType, semanticModel.Compilation);
    }

    private static bool CanTypeAcceptIEnumerable(ITypeSymbol parameterType, ITypeSymbol elementType, Compilation compilation)
    {
        // Get IEnumerable<T> type symbol
        var iEnumerableType = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        if (iEnumerableType == null)
        {
            return false;
        }

        // Construct IEnumerable<elementType>
        var constructedIEnumerable = iEnumerableType.Construct(elementType);

        // Check if parameter type is assignable from IEnumerable<elementType>
        var conversion = compilation.HasImplicitConversion(constructedIEnumerable, parameterType);

        return conversion;
    }

    private static bool MightNeedDefensiveCopy(InvocationExpressionSyntax toListCall, SemanticModel semanticModel)
    {
        // For now, we'll be conservative and check for common patterns where defensive copy might be needed

        // Check if the result is passed to methods that might modify the collection
        var parent = toListCall.Parent;

        // If passed as argument, check if the method might store/modify it
        if (parent is ArgumentSyntax argument)
        {
            // For now, be conservative - this could be enhanced with more sophisticated analysis
            // to check if the method parameter is marked with attributes indicating mutation
            return false; // We'll assume most cases don't need defensive copying for this analyzer
        }

        // Check if assigned to a field or property (might be stored for later use)
        var currentNode = toListCall.Parent;
        while (currentNode != null)
        {
            if (currentNode is AssignmentExpressionSyntax assignment)
            {
                // Check if assigning to a field or property
                if (assignment.Left is MemberAccessExpressionSyntax ||
                    assignment.Left is IdentifierNameSyntax identifier)
                {
                    // This might be storing the collection, so defensive copy could be needed
                    // For now, we'll be permissive and allow the optimization
                    return false;
                }
            }
            currentNode = currentNode.Parent;
        }

        return false;
    }

    private static string GetFriendlyTypeName(ITypeSymbol type)
    {
        // Get a user-friendly display name for the type
        if (type.TypeKind == TypeKind.Array)
        {
            return $"{type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}";
        }

        return type.Name switch
        {
            "List" => $"List<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            "IList" => $"IList<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            "ICollection" => $"ICollection<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            "HashSet" => $"HashSet<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            "SortedSet" => $"SortedSet<{((INamedTypeSymbol)type).TypeArguments[0].Name}>",
            _ => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
        };
    }
}
