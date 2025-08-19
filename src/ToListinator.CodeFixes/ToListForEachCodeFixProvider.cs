using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToListinator.Analyzers;
using ToListinator.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToListinator.CodeFixes;

internal record ForEachInvocationData(
    InvocationExpressionSyntax? ForEachInvocation,
    ExpressionSyntax? OriginalCollection);

internal record ForEachArgumentData(
    ParameterSyntax? Parameter,
    BlockSyntax? Body);

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToListForEachCodeFixProvider)), Shared]
public class ToListForEachCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [ToListForEachAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var invocation = await CodeFixHelper.FindTargetNode<InvocationExpressionSyntax>(
            context,
            ToListForEachAnalyzer.DiagnosticId);

        if (invocation is null)
        {
            return;
        }

        var diagnostic = CodeFixHelper.GetDiagnostic(context, ToListForEachAnalyzer.DiagnosticId);
        if (diagnostic == null)
        {
            return;
        }

        var action = CodeFixHelper.CreateSimpleAction(
            "Replace with foreach loop",
            "ReplaceWithForeachLoop",
            ReplaceWithForeachLoop,
            context,
            invocation);

        context.RegisterCodeFix(action, diagnostic);
    }

    private static async Task<Document> ReplaceWithForeachLoop(
        Document document,
        InvocationExpressionSyntax toListInvocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var invocationData = FindForEachInvocation(toListInvocation);
        var toListAccess = FindToListAccess(invocationData);

        if (toListAccess is null
            || invocationData.ForEachInvocation?.ArgumentList.Arguments is not [{ Expression: var forEachArg }]
            || ParseForEachArgument(forEachArg) is not { } argumentData)
        {
            return document;
        }

        var originalCollection = invocationData.OriginalCollection ?? toListAccess.Expression;
        var expressionStatement = invocationData.ForEachInvocation!.FirstAncestorOrSelf<ExpressionStatementSyntax>();

        if (expressionStatement is null)
        {
            return document;
        }

        // Capture trailing comments from the semicolon token before creating the foreach
        var semicolonTrailingComments = expressionStatement.SemicolonToken.TrailingTrivia
            .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
            .ToList();

        var foreachStatement = CreateForEachStatement(argumentData.Parameter!, originalCollection, argumentData.Body!, semicolonTrailingComments);
        var formattedForeach = ApplyTrivia(foreachStatement, expressionStatement);
        var root = (await document.GetSyntaxRootAsync(cancellationToken))!;
        var newRoot = root.ReplaceNode(expressionStatement, formattedForeach);

        return document.WithSyntaxRoot(newRoot);
    }

    private static MemberAccessExpressionSyntax? FindToListAccess(
        ForEachInvocationData invocationData)
    {
        if (invocationData is
            {
                ForEachInvocation: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name.Identifier.Text: "ForEach",
                        Expression: InvocationExpressionSyntax
                        {
                            Expression: MemberAccessExpressionSyntax
                            {
                                Name.Identifier.Text: "ToList"
                            } toList
                        }
                    }
                }
            })
        {
            return toList;
        }

        return null;
    }


    private static ForEachStatementSyntax CreateForEachStatement(
        ParameterSyntax parameter,
        ExpressionSyntax originalCollection,
        BlockSyntax body,
        IEnumerable<SyntaxTrivia> semicolonTrailingComments)
    {
        // Remove leading trivia from the first token and trailing trivia from the last token
        // but preserve the internal formatting structure and save trailing trivia for later.
        //
        // This is a little bit tricky to ensure we get the correct formatting of the rewritten
        // foreach loop so that, for instance, the closing parenthesis is not floating off on a
        // line by itself.
        var tokens = originalCollection.DescendantTokens().ToArray();
        SyntaxTriviaList savedTrailingTrivia = default;

        if (tokens.Length > 0)
        {
            var updatedTokens = new SyntaxToken[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (i == 0)
                {
                    // First token: remove leading trivia
                    updatedTokens[i] = token.WithLeadingTrivia();
                }
                else if (i == tokens.Length - 1)
                {
                    // Last token: save only comment trivia before removing all trailing trivia
                    var commentTrivia = token.TrailingTrivia.Where(t =>
                        t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                        t.IsKind(SyntaxKind.MultiLineCommentTrivia));
                    savedTrailingTrivia = SyntaxFactory.TriviaList(commentTrivia);
                    updatedTokens[i] = token.WithTrailingTrivia();
                }
                else
                {
                    // Middle tokens: keep as is
                    updatedTokens[i] = token;
                }
            }

            // Replace all tokens
            var tokenIndex = 0;
            originalCollection = originalCollection.ReplaceTokens(tokens, (oldToken, newToken) => updatedTokens[tokenIndex++]);
        }

        var foreachStatement = ForEachStatement(
            attributeLists: default,
            awaitKeyword: default,
            forEachKeyword: Token(SyntaxKind.ForEachKeyword),
            openParenToken: Token(SyntaxKind.OpenParenToken),
            type: IdentifierName("var"),
            identifier: parameter.Identifier,
            inKeyword: Token(SyntaxKind.InKeyword),
            expression: originalCollection,
            closeParenToken: Token(SyntaxKind.CloseParenToken),
            statement: body);

        // Apply trailing comments from either the LINQ chain or the semicolon
        if (savedTrailingTrivia.Any() || semicolonTrailingComments.Any())
        {
            // Combine the trailing comments
            var allTrailingComments = savedTrailingTrivia.Concat(semicolonTrailingComments);
            // Add a space before the comment to separate it from the closing parenthesis
            var triviaWithSpace = SyntaxFactory.TriviaList(SyntaxFactory.Space).AddRange(allTrailingComments);
            foreachStatement = foreachStatement.WithCloseParenToken(
                foreachStatement.CloseParenToken.WithTrailingTrivia(triviaWithSpace));
        }

        return foreachStatement;
    }

    private static ForEachStatementSyntax ApplyTrivia(
        ForEachStatementSyntax foreachStatement,
        ExpressionStatementSyntax expressionStatement)
    {
        var semicolonTrailingTrivia = expressionStatement.SemicolonToken.TrailingTrivia;
        var existingCloseParenTrivia = foreachStatement.CloseParenToken.TrailingTrivia;

        // If we already have trivia on the closing paren (e.g., preserved comments),
        // only add non-comment trivia from the semicolon
        SyntaxTriviaList combinedTrivia;
        if (existingCloseParenTrivia.Count > 0)
        {
            // Filter semicolon trivia to exclude comments to avoid duplication
            var filteredSemicolonTrivia = semicolonTrailingTrivia.Where(t =>
                !t.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                !t.IsKind(SyntaxKind.MultiLineCommentTrivia));
            combinedTrivia = existingCloseParenTrivia.AddRange(filteredSemicolonTrivia);
        }
        else
        {
            // No existing trivia, use all semicolon trivia
            combinedTrivia = semicolonTrailingTrivia;
        }

        return foreachStatement
            .WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
            .WithCloseParenToken(foreachStatement.CloseParenToken.WithTrailingTrivia(combinedTrivia));
    }

    private static ForEachArgumentData? ParseForEachArgument(ExpressionSyntax forEachArg)
    {
        return forEachArg switch
        {
            // Matches list.ToList().ForEach(x => Console.WriteLine(x));
            SimpleLambdaExpressionSyntax simpleLambda => new(
                simpleLambda.Parameter,
                simpleLambda.Body switch
                {
                    BlockSyntax b => b,
                    ExpressionSyntax e => Block(ExpressionStatement(e)),
                    _ => null
                }
            ),

            // Matches list.ToList().ForEach((x) => Console.WriteLine(x));
            ParenthesizedLambdaExpressionSyntax parenLambda when parenLambda.ParameterList.Parameters.Count == 1 => new(
                parenLambda.ParameterList.Parameters[0],
                parenLambda.Body switch
                {
                    BlockSyntax b => b,
                    ExpressionSyntax e => Block(ExpressionStatement(e)),
                    _ => null
                }
            ),

            // Matches list.OrderBy(x => x).ToList().ForEach(Print);
            IdentifierNameSyntax methodGroup => new(
                Parameter(Identifier("x")),
                Block(
                    ExpressionStatement(
                        InvocationExpression(methodGroup)
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(IdentifierName("x")))))))
            ),

            // Matches list.OrderBy(x => x).ToList().ForEach(Console.Write);
            MemberAccessExpressionSyntax memberAccessMethodGroup => new(
                Parameter(Identifier("x")),
                Block(
                    ExpressionStatement(
                        InvocationExpression(memberAccessMethodGroup)
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(IdentifierName("x")))))))
            ),

            // Matches list.Where(x => x > 0).ToList().ForEach(delegate(int item) { Console.WriteLine(item); });
            AnonymousMethodExpressionSyntax anonymousMethod
                when anonymousMethod.ParameterList?.Parameters.Count == 1
                => new(
                    anonymousMethod.ParameterList.Parameters[0],
                    anonymousMethod.Body is BlockSyntax originalBody
                        ? Block(originalBody.Statements)
                        : null
            ),

            _ => null,
        };
    }

    private static ForEachInvocationData FindForEachInvocation(
        InvocationExpressionSyntax toListInvocation)
    {
        if (toListInvocation is
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ToList",
                    Expression: var matchedOriginalCollection,
                },
                Parent: MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ForEach",
                    Parent: InvocationExpressionSyntax
                    {
                        ArgumentList.Arguments: [{ Expression: { } }],
                    } matchedForEachInvocation,
                },
            }
        )
        {
            // It's a direct ToList().ForEach() already.
            return new(matchedForEachInvocation, matchedOriginalCollection);
        }
        else
        {
            // It's a more complex expression, possibly a chained set of calls, so try to
            // find the actual ToList().ForEach() expression by walking the chain.
            var forEachInvocation = TryFindToListForEachChain(toListInvocation);
            return new(forEachInvocation, null);
        }
    }

    // We could have a chain of calls, like Select().Where().OrderBy().Take().ToList().ForEach().
    // This code needs to handle this as well as the simple case of just a plain ToList().ForEach()
    // directly on a collection. This method walks the starting node's parents until it gets to the
    // actual ToList().ForEach() call that we want to replace.
    private static InvocationExpressionSyntax? TryFindToListForEachChain(SyntaxNode node)
    {
        var current = node;

        while (current?.Parent is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Parent is InvocationExpressionSyntax invocation)
        {
            if (memberAccess.Name.Identifier.Text is "ToList" &&
                invocation.Parent is MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ForEach",
                    Parent: InvocationExpressionSyntax forEachInvocation,
                })
            {
                return forEachInvocation;
            }

            current = invocation;
        }

        return null;
    }
}
