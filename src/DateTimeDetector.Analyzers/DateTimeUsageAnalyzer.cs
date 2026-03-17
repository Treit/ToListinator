using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DateTimeDetector.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DateTimeUsageAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DT001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Prefer DateTimeOffset over DateTime",
        messageFormat: "Use DateTimeOffset instead of DateTime to ensure correct time zone handling",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "DateTime does not store time zone information, which can lead to subtle bugs. DateTimeOffset preserves the offset from UTC and is generally preferred.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            var dateTimeType = startContext.Compilation.GetTypeByMetadataName("System.DateTime");
            if (dateTimeType is null)
            {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeIdentifier(nodeContext, dateTimeType),
                SyntaxKind.IdentifierName);
        });
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context, INamedTypeSymbol dateTimeType)
    {
        var identifierName = (IdentifierNameSyntax)context.Node;

        if (identifierName.Identifier.ValueText is not "DateTime")
        {
            return;
        }

        var symbol = GetSymbol(context.SemanticModel, identifierName, context.CancellationToken);

        var typeSymbol = symbol switch
        {
            INamedTypeSymbol namedType => namedType,
            IMethodSymbol { ContainingType: { } containingType } => containingType,
            IPropertySymbol { ContainingType: { } containingType } => containingType,
            IFieldSymbol { ContainingType: { } containingType } => containingType,
            IAliasSymbol { Target: INamedTypeSymbol aliasedType } => aliasedType,
            _ => null
        };

        if (typeSymbol is not null
            && SymbolEqualityComparer.Default.Equals(typeSymbol, dateTimeType))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, identifierName.GetLocation()));
        }
    }

    private static ISymbol? GetSymbol(
        SemanticModel semanticModel,
        IdentifierNameSyntax identifierName,
        CancellationToken cancellationToken)
    {
        SyntaxNode semanticTarget = identifierName.Parent switch
        {
            MemberAccessExpressionSyntax memberAccess when memberAccess.Name == identifierName => memberAccess,
            QualifiedNameSyntax qualifiedName when qualifiedName.Right == identifierName => qualifiedName,
            AliasQualifiedNameSyntax aliasQualifiedName when aliasQualifiedName.Name == identifierName => aliasQualifiedName,
            _ => identifierName
        };

        var symbolInfo = semanticModel.GetSymbolInfo(semanticTarget, cancellationToken);
        return symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
    }
}
