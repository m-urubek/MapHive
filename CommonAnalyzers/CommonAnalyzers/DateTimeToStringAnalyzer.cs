using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CommonAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DateTimeToStringAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MH0001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Use IFormatProvider with DateTime.ToString",
            "Replace parameterless 'DateTime.ToString()' with 'ToString(format, IFormatProvider)'",
            "Globalization",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation.ArgumentList.Arguments.Count != 0)
            {
                return;
            }

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            if (memberAccess.Name.Identifier.Text != nameof(ToString))
            {
                return;
            }

            if (!(context.SemanticModel.GetSymbolInfo(memberAccess).Symbol is IMethodSymbol symbol))
            {
                return;
            }

            if (symbol.ContainingType.SpecialType != SpecialType.System_DateTime)
            {
                return;
            }

            if (symbol.Parameters.Length != 0)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation()));
        }
    }
}