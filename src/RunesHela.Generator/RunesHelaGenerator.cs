using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RunesHela.Generator;

/// <summary>
/// Incremental generator that discovers <c>[Command]</c> classes and emits the application entry point.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class RunesHelaGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            "RunesHela.CommandAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (attributeContext, cancellationToken) =>
                CommandModelFactory.Create(attributeContext, cancellationToken));

        context.RegisterSourceOutput(
            candidates.Collect(),
            static (sourceContext, items) => Emit(sourceContext, items));
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<CommandCandidate> candidates)
    {
        var models = new List<CommandModel>();

        foreach (var candidate in candidates)
        {
            foreach (var diagnostic in candidate.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            if (candidate.Model is not null)
            {
                models.Add(candidate.Model);
            }
        }

        var unique = new List<CommandModel>();
        var seen = new HashSet<string>(System.StringComparer.Ordinal);

        foreach (var model in models)
        {
            if (seen.Add(model.Name))
            {
                unique.Add(model);
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateCommandName,
                    model.Location,
                    model.Name));
            }
        }

        var appName = unique.Count > 0 ? unique[0].AppName : "app";
        var source = Emitter.Emit(appName, unique);

        context.AddSource("RunesHela.Entry.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}
