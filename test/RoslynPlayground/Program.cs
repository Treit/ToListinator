using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

var list = new List<string>();
list.Add("A");
list.Add("B");
list.Add("C");

// Iterate over the items
foreach (var str in list ?? new List<string>()) // Check for null
{
    Console.WriteLine(str); // Print it out
}

var syntax = GetForEachLoopWithNullCoalescingCheck();

var text = syntax.ToFullString();
Console.WriteLine(text);

Console.WriteLine($"-----------------{Environment.NewLine}");

var forEach = syntax
    .DescendantNodes()
    .OfType<ForEachStatementSyntax>()
    .FirstOrDefault();

if (forEach is null)
{
    Console.WriteLine("Wrong type!");
    return;
}

var rewriter = new ForEachNullCheckRewriter();
var rewritten = rewriter.VisitForEachStatement(forEach);

Console.WriteLine(rewritten?.ToFullString());

static CompilationUnitSyntax GetForEachLoopWithNullCoalescingCheck()
{
    var result = CompilationUnit()
    .WithMembers(
        SingletonList<MemberDeclarationSyntax>(
            GlobalStatement(
                ForEachStatement(
                    IdentifierName(
                        Identifier(
                            TriviaList(),
                            SyntaxKind.VarKeyword,
                            "var",
                            "var",
                            TriviaList())),
                    Identifier("str"),
                    BinaryExpression(
                        SyntaxKind.CoalesceExpression,
                        IdentifierName("list"),
                        ObjectCreationExpression(
                            GenericName(
                                Identifier("List"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        PredefinedType(
                                            Token(SyntaxKind.StringKeyword))))))
                        .WithArgumentList(
                            ArgumentList())),
                    Block(
                        SingletonList<StatementSyntax>(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("Console"),
                                        IdentifierName("WriteLine")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                IdentifierName("str"))))))
                            .WithSemicolonToken(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.SemicolonToken,
                                    TriviaList(
                                        Comment("// Print it out")))))))
            .WithForEachKeyword(
                Token(
                    TriviaList(
                        [
                            Comment("// Iterate over the items"),
                            Comment("// Here is another comment"),
                            Comment("// And some more comments")
                        ]),
                    SyntaxKind.ForEachKeyword,
                    TriviaList()))
                .WithCloseParenToken(
                    Token(
                        TriviaList(),
                        SyntaxKind.CloseParenToken,
                        TriviaList(
                            Comment("// Check for null")))))))
    .NormalizeWhitespace();

    return result;
}

