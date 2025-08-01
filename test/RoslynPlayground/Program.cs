using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ToListinator.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
Console.WriteLine();

Console.WriteLine("-----------------");

syntax = GetCodeToRefactor();
Console.WriteLine(syntax.ToFullString());

Console.WriteLine("-----------------");
var updatedSyntax = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(syntax);
Console.WriteLine(updatedSyntax.ToFullString());

var code =
    """
    List<string>? list = null;
    // Check list and iterate
    if (list != null)
    {
        foreach (var item in list) // Comment after
        {
            Console.WriteLine(item);
        }
    }
    """;

Console.WriteLine("----------");
var syntaxTree = CSharpSyntaxTree.ParseText(code);
var root = syntaxTree.GetRoot();
var updatedRoot = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);
Console.WriteLine(updatedRoot.ToFullString());
var formattedCode = updatedRoot.NormalizeWhitespace();

Console.WriteLine(formattedCode.ToFullString());

Console.WriteLine("*****");
formattedCode = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(formattedCode);
Console.WriteLine(formattedCode.ToFullString());

code = """
    class TestClass
    {
        void TestMethod()
        {
            int x = 1;
            if (x > 0)
            {
                Console.WriteLine("Outer if");
                if (x < 10)
                {
                    Console.WriteLine("Inner if");
                }
            }
        }
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
root = tree.GetRoot();

var result = BlankLineFormatter.EnsureBlankLineBeforeIfStatements(root);
Console.WriteLine(result.ToFullString());

static CompilationUnitSyntax GetCodeToRefactor()
{
    return CompilationUnit()
    .WithMembers(
        List<MemberDeclarationSyntax>(
            new MemberDeclarationSyntax[]{
            GlobalStatement(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        NullableType(
                            GenericName(
                                Identifier("List"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        PredefinedType(
                                            Token(SyntaxKind.StringKeyword)))))))
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier("list"))
                            .WithInitializer(
                                EqualsValueClause(
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression))))))),
            GlobalStatement(
                IfStatement(
                    BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        IdentifierName("list"),
                        LiteralExpression(
                            SyntaxKind.NullLiteralExpression)),
                    Block(
                        SingletonList<StatementSyntax>(
                            ForEachStatement(
                                IdentifierName(
                                    Identifier(
                                        TriviaList(),
                                        SyntaxKind.VarKeyword,
                                        "var",
                                        "var",
                                        TriviaList())),
                                Identifier("item"),
                                IdentifierName("list"),
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
                                                            IdentifierName("item")))))))))
                            .WithCloseParenToken(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.CloseParenToken,
                                    TriviaList(
                                        Comment("// Comment after")))))))
                .WithIfKeyword(
                    Token(
                        TriviaList(
                            Comment("// Check list and iterate")),
                        SyntaxKind.IfKeyword,
                        TriviaList())))}))
        .NormalizeWhitespace();
}

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

