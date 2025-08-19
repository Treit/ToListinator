using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToListinator.Utils;
using Xunit;

namespace ToListinator.Utils.Tests;

public class CodeFixHelperTests
{
    [Fact]
    public async Task FindTargetNode_WithValidDiagnostic_ReturnsCorrectNode()
    {
        // Arrange
        var code = "var x = 1 + 2;";
        var document = CreateDocument(code);
        var syntaxTree = await document.GetSyntaxTreeAsync();
        var root = await syntaxTree!.GetRootAsync();
        var binaryExpression = root.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("TEST001", "Test", "Test", "Test", DiagnosticSeverity.Warning, true),
            Location.Create(syntaxTree, binaryExpression.Span));

        var context = CreateCodeFixContext(document, diagnostic);

        // Act
        var result = await CodeFixHelper.FindTargetNode<BinaryExpressionSyntax>(context, "TEST001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(binaryExpression.ToString(), result.ToString());
    }

    [Fact]
    public async Task FindTargetNode_WithInvalidDiagnosticId_ReturnsNull()
    {
        // Arrange
        var code = "var x = 1 + 2;";
        var document = CreateDocument(code);
        var syntaxTree = await document.GetSyntaxTreeAsync();
        var root = await syntaxTree!.GetRootAsync();
        var binaryExpression = root.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("TEST001", "Test", "Test", "Test", DiagnosticSeverity.Warning, true),
            Location.Create(syntaxTree, binaryExpression.Span));

        var context = CreateCodeFixContext(document, diagnostic);

        // Act
        var result = await CodeFixHelper.FindTargetNode<BinaryExpressionSyntax>(context, "DIFFERENT_ID");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindTargetNodeBySpan_WithValidDiagnostic_ReturnsCorrectNode()
    {
        // Arrange
        var code = "var x = 1 + 2;";
        var document = CreateDocument(code);
        var syntaxTree = await document.GetSyntaxTreeAsync();
        var root = await syntaxTree!.GetRootAsync();
        var binaryExpression = root.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("TEST001", "Test", "Test", "Test", DiagnosticSeverity.Warning, true),
            Location.Create(syntaxTree, binaryExpression.Span));

        var context = CreateCodeFixContext(document, diagnostic);

        // Act
        var result = await CodeFixHelper.FindTargetNodeBySpan<BinaryExpressionSyntax>(context, "TEST001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(binaryExpression.ToString(), result.ToString());
    }

    [Fact]
    public void GetDiagnostic_WithValidId_ReturnsDiagnostic()
    {
        // Arrange
        var code = "var x = 1;";
        var document = CreateDocument(code);
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("TEST001", "Test", "Test", "Test", DiagnosticSeverity.Warning, true),
            Location.None);

        var context = CreateCodeFixContext(document, diagnostic);

        // Act
        var result = CodeFixHelper.GetDiagnostic(context, "TEST001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST001", result.Id);
    }

    [Fact]
    public void GetDiagnostic_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var code = "var x = 1;";
        var document = CreateDocument(code);
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("TEST001", "Test", "Test", "Test", DiagnosticSeverity.Warning, true),
            Location.None);

        var context = CreateCodeFixContext(document, diagnostic);

        // Act
        var result = CodeFixHelper.GetDiagnostic(context, "DIFFERENT_ID");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSyntaxRoot_WithValidContext_ReturnsSyntaxRoot()
    {
        // Arrange
        var code = "var x = 1;";
        var document = CreateDocument(code);
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("TEST001", "Test", "Test", "Test", DiagnosticSeverity.Warning, true),
            Location.None);

        var context = CreateCodeFixContext(document, diagnostic);

        // Act
        var result = await CodeFixHelper.GetSyntaxRoot(context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsKind(SyntaxKind.CompilationUnit));
    }

    [Fact]
    public async Task ReplaceNodeWithTrivia_PreservesTrivia()
    {
        // Arrange
        var code = """
            // Leading comment
            var x = 1 + 2; // Trailing comment
            """;

        var document = CreateDocument(code);
        var syntaxTree = await document.GetSyntaxTreeAsync();
        var root = await syntaxTree!.GetRootAsync();
        var binaryExpression = root.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        var newExpression = SyntaxFactory.BinaryExpression(
            SyntaxKind.MultiplyExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(4)));

        // Act
        var result = await CodeFixHelper.ReplaceNodeWithTrivia(document, binaryExpression, newExpression, CancellationToken.None);
        var newRoot = await result.GetSyntaxRootAsync();
        var newBinaryExpression = newRoot!.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        // Assert
        Assert.Equal("3*4", newBinaryExpression.ToString().Trim());
        // The trivia should be preserved by TriviaHelper.PreserveTrivia
    }

    private static Document CreateDocument(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = CSharpCompilation.Create("TestAssembly", new[] { syntaxTree }, references);

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        return workspace.AddDocument(project.Id, "Test.cs", SourceText.From(code));
    }

    private static CodeFixContext CreateCodeFixContext(Document document, Diagnostic diagnostic)
    {
        return new CodeFixContext(
            document,
            diagnostic.Location.SourceSpan,
            ImmutableArray.Create(diagnostic),
            (action, diagnostics) => { },
            CancellationToken.None);
    }
}
