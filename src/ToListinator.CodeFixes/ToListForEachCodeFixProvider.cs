using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ToListinator.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToListForEachCodeFixProvider)), Shared]
    public class ToListForEachCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ToListForEachAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First(diag => diag.Id == ToListForEachAnalyzer.DiagnosticId);
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the invocation expression that triggered the diagnostic
            var invocation = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            if (invocation == null)
                return;

            // Register the code fix
            var action = CodeAction.Create(
                title: "Replace with foreach loop",
                createChangedDocument: c => ReplaceWithForeachLoop(context.Document, invocation, c),
                equivalenceKey: "ReplaceWithForeachLoop");

            context.RegisterCodeFix(action, diagnostic);
        }

        private static async Task<Document> ReplaceWithForeachLoop(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            // The invocation should be the ForEach call
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Name.Identifier.ValueText != "ForEach")
            {
                return document;
            }

            // Get the ToList() invocation from the ForEach expression
            if (memberAccess.Expression is not InvocationExpressionSyntax toListInvocation ||
                toListInvocation.Expression is not MemberAccessExpressionSyntax toListMemberAccess ||
                toListMemberAccess.Name.Identifier.ValueText != "ToList")
            {
                return document;
            }

            // Get the original collection (before ToList())
            var originalCollection = toListMemberAccess.Expression;
            
            // Get the lambda expression from ForEach
            var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
            if (argument?.Expression is not LambdaExpressionSyntax lambda)
            {
                return document;
            }

            // Create the foreach statement
            var foreachStatement = CreateForeachStatement(originalCollection, lambda);
            
            // Find the statement containing the invocation
            var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
            if (statement != null)
            {
                var newRoot = root.ReplaceNode(statement, foreachStatement);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        private static ForEachStatementSyntax CreateForeachStatement(
            ExpressionSyntax collection,
            LambdaExpressionSyntax lambda)
        {
            // Extract parameter name from lambda
            var parameter = lambda switch
            {
                SimpleLambdaExpressionSyntax simple => simple.Parameter,
                ParenthesizedLambdaExpressionSyntax paren => paren.ParameterList.Parameters.FirstOrDefault(),
                _ => null
            };

            var parameterName = parameter?.Identifier.ValueText ?? "item";

            // Get the lambda body
            var body = lambda.Body switch
            {
                BlockSyntax block => block,
                ExpressionSyntax expr => SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(expr)),
                _ => SyntaxFactory.Block()
            };

            // Create the foreach statement
            return SyntaxFactory.ForEachStatement(
                type: SyntaxFactory.IdentifierName("var"),
                identifier: SyntaxFactory.Identifier(parameterName),
                expression: collection,
                statement: body);
        }
    }
}
