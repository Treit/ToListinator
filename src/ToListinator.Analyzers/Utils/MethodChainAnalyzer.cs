using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ToListinator.Analyzers.Utils;

/// <summary>
/// Provides utilities for analyzing method chains, particularly LINQ method chains
/// like Where().Count(), Select().ToList(), etc.
/// </summary>
public static class MethodChainAnalyzer
{
    /// <summary>
    /// Collects all invocations in a chain for a specific method name.
    /// For example, given "items.Where(x => x > 0).Where(x => x < 10).Count()",
    /// this would collect both Where() invocations.
    /// </summary>
    /// <param name="expression">The expression to start from (typically the end of the chain)</param>
    /// <param name="methodName">The method name to collect (e.g., "Where")</param>
    /// <returns>A list of invocations in forward order (first to last)</returns>
    public static List<InvocationExpressionSyntax> CollectMethodChain(
        ExpressionSyntax expression,
        string methodName)
    {
        var chain = new List<InvocationExpressionSyntax>();
        var current = expression;

        // Walk back through the chain collecting method calls with the specified name
        while (current is InvocationExpressionSyntax invocation &&
               invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Name.Identifier.ValueText == methodName)
        {
            chain.Add(invocation);
            current = memberAccess.Expression;
        }

        // Reverse to get them in forward order (first to last)
        chain.Reverse();
        return chain;
    }

    /// <summary>
    /// Collects all invocations in a chain for any of the specified method names.
    /// </summary>
    /// <param name="expression">The expression to start from</param>
    /// <param name="methodNames">The method names to collect</param>
    /// <returns>A list of matching invocations in forward order</returns>
    public static List<InvocationExpressionSyntax> CollectMethodChain(
        ExpressionSyntax expression,
        params string[] methodNames)
    {
        var methodSet = new HashSet<string>(methodNames);
        var chain = new List<InvocationExpressionSyntax>();
        var current = expression;

        // Walk back through the chain collecting method calls with any of the specified names
        while (current is InvocationExpressionSyntax invocation &&
               invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               methodSet.Contains(memberAccess.Name.Identifier.ValueText))
        {
            chain.Add(invocation);
            current = memberAccess.Expression;
        }

        // Reverse to get them in forward order (first to last)
        chain.Reverse();
        return chain;
    }

    /// <summary>
    /// Gets the root expression of a method chain (the expression before all the method calls).
    /// For example, given "items.Select(x => x * 2).Where(x => x > 0).ToList()",
    /// this would return "items".
    /// </summary>
    /// <param name="invocation">Any invocation in the chain</param>
    /// <returns>The root expression, or null if not found</returns>
    public static ExpressionSyntax? GetChainRoot(InvocationExpressionSyntax invocation)
    {
        var current = invocation;

        // Walk back to find the beginning of the chain
        while (current.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression is InvocationExpressionSyntax parentInvocation)
            {
                current = parentInvocation;
            }
            else
            {
                // Found the root expression
                return memberAccess.Expression;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if an invocation expression is a call to a specific method name.
    /// </summary>
    /// <param name="invocation">The invocation to check</param>
    /// <param name="methodName">The method name to look for</param>
    /// <returns>True if the invocation calls the specified method</returns>
    public static bool IsMethodCall(InvocationExpressionSyntax invocation, string methodName)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Name.Identifier.ValueText == methodName;
    }

    /// <summary>
    /// Checks if an invocation expression is a call to any of the specified method names.
    /// </summary>
    /// <param name="invocation">The invocation to check</param>
    /// <param name="methodNames">The method names to look for</param>
    /// <returns>True if the invocation calls any of the specified methods</returns>
    public static bool IsMethodCall(InvocationExpressionSyntax invocation, params string[] methodNames)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var methodName = memberAccess.Name.Identifier.ValueText;
        return methodNames.Contains(methodName);
    }

    /// <summary>
    /// Extracts the method name from an invocation expression.
    /// </summary>
    /// <param name="invocation">The invocation expression</param>
    /// <returns>The method name, or null if not a member access</returns>
    public static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.Identifier.ValueText
            : null;
    }

    /// <summary>
    /// Builds the complete method chain as a list of method names.
    /// For example, "items.Select(x => x * 2).Where(x => x > 0).ToList()"
    /// would return ["Select", "Where", "ToList"].
    /// </summary>
    /// <param name="invocation">The final invocation in the chain</param>
    /// <returns>A list of method names in the chain</returns>
    public static List<string> GetMethodChainNames(InvocationExpressionSyntax invocation)
    {
        var methodNames = new List<string>();
        var current = invocation;

        // Walk back through the entire chain
        while (current.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            methodNames.Add(memberAccess.Name.Identifier.ValueText);

            if (memberAccess.Expression is InvocationExpressionSyntax parentInvocation)
            {
                current = parentInvocation;
            }
            else
            {
                break;
            }
        }

        // Reverse to get forward order
        methodNames.Reverse();
        return methodNames;
    }

    /// <summary>
    /// Checks if a method chain contains a specific method call.
    /// </summary>
    /// <param name="invocation">Any invocation in the chain</param>
    /// <param name="methodName">The method name to look for</param>
    /// <returns>True if the chain contains the specified method</returns>
    public static bool ChainContainsMethod(InvocationExpressionSyntax invocation, string methodName)
    {
        var methodNames = GetMethodChainNames(invocation);
        return methodNames.Contains(methodName);
    }

    /// <summary>
    /// Finds the first invocation in a chain that calls a specific method.
    /// </summary>
    /// <param name="startInvocation">The invocation to start searching from</param>
    /// <param name="methodName">The method name to find</param>
    /// <returns>The invocation that calls the specified method, or null if not found</returns>
    public static InvocationExpressionSyntax? FindMethodInChain(
        InvocationExpressionSyntax startInvocation,
        string methodName)
    {
        var current = startInvocation;

        // Check the current invocation first
        if (IsMethodCall(current, methodName))
        {
            return current;
        }

        // Walk back through the chain
        while (current.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression is InvocationExpressionSyntax parentInvocation)
            {
                if (IsMethodCall(parentInvocation, methodName))
                {
                    return parentInvocation;
                }
                current = parentInvocation;
            }
            else
            {
                break;
            }
        }

        return null;
    }
}
