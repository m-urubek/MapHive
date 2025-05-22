using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CommonAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RepopulatedPropertyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MH0002";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Missing hidden field for [Repopulated] property",
            "Property '{0}' is marked with [Repopulated] but no hidden <input asp-for=\"{0}\" /> was found in any Razor view",
            "MVC",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                ImmutableArray<AdditionalText> razorFiles = compilationContext.Options.AdditionalFiles
                    .Where(file => file.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
                    .ToImmutableArray();

                compilationContext.RegisterSymbolAction(symbolContext =>
                    this.AnalyzeProperty(symbolContext, razorFiles),
                    SymbolKind.Property);
            });
        }

        private void AnalyzeProperty(SymbolAnalysisContext context, ImmutableArray<AdditionalText> razorFiles)
        {
            IPropertySymbol property = (IPropertySymbol)context.Symbol;
            // Collect all [Repopulated] annotations on this property
            ImmutableArray<AttributeData> repopAttrs = property.GetAttributes()
                .Where(attr => attr.AttributeClass?.Name == "RepopulatedAttribute")
                .ToImmutableArray();

            if (repopAttrs.Length == 0)
            {
                return;
            }

            foreach (AttributeData repopAttr in repopAttrs)
            {
                // The user-specified view path without extension
                string viewPath = repopAttr.ConstructorArguments.FirstOrDefault().Value as string;
                if (string.IsNullOrEmpty(viewPath))
                {
                    // Report misconfigured attribute (empty view path)
                    context.ReportDiagnostic(Diagnostic.Create(Rule, property.Locations[0], property.Name));
                    continue;
                }

                // Normalize the user-specified view path
                string viewPathNorm = viewPath.Replace('\\', '/').Trim('/');
                // Build search patterns: if the attribute includes 'Views/' prefix, use it directly, otherwise search in Views and Views/Shared
                string[] candidatePatterns = viewPathNorm.StartsWith("Views/", StringComparison.OrdinalIgnoreCase)
                    ? (new[]
                    {
                        viewPathNorm.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
                            ? viewPathNorm
                            : viewPathNorm + ".cshtml"
                    })
                    : (new[]
                    {
                        "Views/" + viewPathNorm + ".cshtml",
                        "Views/Shared/" + viewPathNorm + ".cshtml"
                    });
                // Find matching views under Views, Shared, or in Areas/<Area>/Views
                ImmutableArray<AdditionalText> matchingViews = razorFiles
                    .Where(file =>
                    {
                        string normalized = file.Path.Replace('\\', '/');
                        // Direct matches in Views or Shared
                        foreach (string patt in candidatePatterns)
                        {
                            if (normalized.IndexOf(patt, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                        // Also allow Areas/<Area>/Views/{viewPathNorm}.cshtml
                        string areaSuffix = "/Views/" + viewPathNorm + ".cshtml";
                        return normalized.IndexOf("/Areas/", StringComparison.OrdinalIgnoreCase) >= 0
                            && normalized.EndsWith(areaSuffix, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToImmutableArray();
                // If no view is found, emit a diagnostic
                if (matchingViews.Length == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, property.Locations[0], property.Name));
                    continue;
                }

                // Check each view for a hidden input field
                bool found = false;
                foreach (AdditionalText file in matchingViews)
                {
                    string text = file.GetText(context.CancellationToken)?.ToString();
                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    if (text.IndexOf($"asp-for=\"{property.Name}\"", StringComparison.OrdinalIgnoreCase) >= 0
                        && text.IndexOf("type=\"hidden\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, property.Locations[0], property.Name));
                }
            }
        }
    }
}