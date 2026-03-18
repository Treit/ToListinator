using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
    {
        var propertySyntax = (PropertyDeclarationSyntax)context.Node;

        if (!propertySyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
            || propertySyntax.ExpressionBody is null)
        {
            return;
        }

        var operation = context.SemanticModel.GetOperation(
            propertySyntax.ExpressionBody.Expression, context.CancellationToken);

        if (operation is not null && ContainsPotentialAllocation(operation))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                propertySyntax.Identifier.GetLocation(),
                propertySyntax.Identifier.ValueText));
        }
    }

    private static bool ContainsPotentialAllocation(IOperation operation)
    {
        return operation switch
        {
            // Object/array creation
            IObjectCreationOperation => true,
            ITypeParameterObjectCreationOperation => true,
            IArrayCreationOperation => true,

            // Collection expressions (unless targeting Span<T>/ReadOnlySpan<T>)
            ICollectionExpressionOperation collExpr => !IsSpanType(collExpr.Type),

            // Allocating method calls
            IInvocationOperation invocation => IsAllocatingMethodCall(invocation),

            // Assignments — check the value (but skip null-coalescing assignment ??= which is lazy init)
            ISimpleAssignmentOperation assignment => ContainsPotentialAllocation(assignment.Value),

            // Member access — check the instance expression
            IPropertyReferenceOperation propRef =>
                propRef.Instance is not null && ContainsPotentialAllocation(propRef.Instance),

            // Conditional (ternary) — check both branches unless it's lazy initialization
            IConditionalOperation conditional =>
                !IsLazyInitializationWithAssignment(conditional) &&
                (ContainsPotentialAllocation(conditional.WhenTrue) ||
                 (conditional.WhenFalse is not null && ContainsPotentialAllocation(conditional.WhenFalse))),

            // Binary operations — check for string concatenation and recurse
            IBinaryOperation binary =>
                IsStringConcatenation(binary) ||
                ContainsPotentialAllocation(binary.LeftOperand) ||
                ContainsPotentialAllocation(binary.RightOperand),

            // Null coalescing — check both sides
            ICoalesceOperation coalesce =>
                ContainsPotentialAllocation(coalesce.Value) ||
                ContainsPotentialAllocation(coalesce.WhenNull),

            // Conversions (casts) — check the operand
            IConversionOperation conv => ContainsPotentialAllocation(conv.Operand),

            // Return statements — check the returned value
            IReturnOperation ret =>
                ret.ReturnedValue is not null && ContainsPotentialAllocation(ret.ReturnedValue),

            // Block — check all statements
            IBlockOperation block => block.Operations.Any(ContainsPotentialAllocation),

            // Expression statements — check the expression
            IExpressionStatementOperation expr => ContainsPotentialAllocation(expr.Operation),

            // Parenthesized — check inner
            IParenthesizedOperation paren => ContainsPotentialAllocation(paren.Operand),

            // Literals, identifiers, field references, etc. don't allocate
            _ => false
        };
    }

    private static bool IsAllocatingMethodCall(IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        // Environment method calls are likely allocating
        if (method.ContainingType is
            {
                Name: "Environment",
                ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true }
            })
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
        if (method.ContainingType is
            {
                Name: "Enumerable",
                ContainingNamespace: { Name: "Linq", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } }
            })
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

    private static bool IsStringConcatenation(IBinaryOperation binary)
    {
        return binary.OperatorKind == BinaryOperatorKind.Add &&
               (binary.LeftOperand.Type?.SpecialType == SpecialType.System_String ||
                binary.RightOperand.Type?.SpecialType == SpecialType.System_String);
    }

    private static bool IsSpanType(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol
        {
            Arity: 1,
            ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true },
            Name: "Span" or "ReadOnlySpan"
        };
    }

    private static bool IsLazyInitializationWithAssignment(IConditionalOperation conditional)
    {
        if (conditional.WhenFalse is null)
        {
            return false;
        }

        var whenTrue = UnwrapImplicitConversions(conditional.WhenTrue);
        var whenFalse = UnwrapImplicitConversions(conditional.WhenFalse);

        // Check if one branch is an assignment and the other returns the same symbol
        if (whenTrue is ISimpleAssignmentOperation trueAssignment)
        {
            var assignedSymbol = GetReferencedSymbol(trueAssignment.Target);
            if (assignedSymbol is not null && IsSymbolReference(whenFalse, assignedSymbol))
            {
                return true;
            }
        }

        if (whenFalse is ISimpleAssignmentOperation falseAssignment)
        {
            var assignedSymbol = GetReferencedSymbol(falseAssignment.Target);
            if (assignedSymbol is not null && IsSymbolReference(whenTrue, assignedSymbol))
            {
                return true;
            }
        }

        return false;
    }

    private static IOperation UnwrapImplicitConversions(IOperation operation)
    {
        // Roslyn may wrap a value in one or more implicit conversions
        // (e.g. covariance, interface adaptation). Peel them all off.
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static ISymbol? GetReferencedSymbol(IOperation operation)
    {
        var unwrapped = UnwrapImplicitConversions(operation);
        return unwrapped switch
        {
            ILocalReferenceOperation local => local.Local,
            IFieldReferenceOperation field => field.Field,
            _ => null
        };
    }

    private static bool IsSymbolReference(IOperation operation, ISymbol symbol)
    {
        return GetReferencedSymbol(operation) is { } refSymbol &&
               SymbolEqualityComparer.Default.Equals(refSymbol, symbol);
    }
}
