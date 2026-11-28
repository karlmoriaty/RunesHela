using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RunesHela.Generator;
using Xunit;

namespace RunesHela.Generator.Tests;

public sealed class GeneratorTests
{
    [Fact]
    public void GeneratesEntryAndRoutesCommand()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello", Description = "Say hello.")]
            public sealed class HelloCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("case \"hello\":", result.GeneratedSource);
        Assert.Contains("RunNode0Async", result.GeneratedSource);
        Assert.Contains("RunesHelaApp.RegisterEntry", result.GeneratedSource);
    }

    [Fact]
    public void GeneratesNestedRouting()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("remote", Description = "Manage remotes.")]
            public sealed class RemoteCommand
            {
                [Command("add", Description = "Add a remote.")]
                public sealed class AddCommand
                {
                    public void Run()
                    {
                    }
                }

                [Command("list", Description = "List remotes.")]
                public sealed class ListCommand
                {
                    public int Run()
                    {
                        return 0;
                    }
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Assert.Contains("case \"remote\":", result.GeneratedSource);
        Assert.Contains("case \"add\":", result.GeneratedSource);
        Assert.Contains("case \"list\":", result.GeneratedSource);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL023");
    }

    [Fact]
    public void ReportsNestedParentWithoutCommand()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            public sealed class NotACommand
            {
                [Command("inner")]
                public sealed class InnerCommand
                {
                    public void Run()
                    {
                    }
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL023");
    }

    [Fact]
    public void ReportsDuplicateCommandNames()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("dup", Description = "First.")]
            public sealed class FirstCommand
            {
                public void Run()
                {
                }
            }

            [Command("dup", Description = "Second.")]
            public sealed class SecondCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL001");
    }

    [Fact]
    public void ReportsMissingHandlerAsWarningAndGeneratesStub()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("noop", Description = "No operation.")]
            public sealed class NoopCommand
            {
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Id == "RHL006" && diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.Contains("NotImplemented", result.GeneratedSource);
    }

    [Fact]
    public void ReportsUnsupportedHandlerParameters()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello", Description = "Say hello.")]
            public sealed class HelloCommand
            {
                public void Run(string name)
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL004");
    }

    [Fact]
    public void GeneratesAsyncHandlerWrapper()
    {
        const string source = """
            using System.Threading.Tasks;
            using RunesHela;

            namespace Demo;

            [Command("info", Description = "Show info.")]
            public sealed class InfoCommand
            {
                public Task RunAsync()
                {
                    return Task.CompletedTask;
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Assert.Contains("AwaitTaskAsync", result.GeneratedSource);
    }

    [Fact]
    public void ReportsMultipleHandlers()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("multi", Description = "Multiple handlers.")]
            public sealed class MultiCommand
            {
                public void Run()
                {
                }

                public int Execute()
                {
                    return 0;
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL007");
    }

    [Fact]
    public void ReportsInvalidCommandName()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("Hello", Description = "Uppercase name.")]
            public sealed class HelloCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL013");
    }

    [Fact]
    public void ReportsMissingDescriptionInfo()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello")]
            public sealed class HelloCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Id == "RHL005" && diagnostic.Severity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void SupportsAliases()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello", Aliases = new[] { "hi" }, Description = "Say hello.")]
            public sealed class HelloCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Assert.Contains("case \"hi\":", result.GeneratedSource);
    }

    [Fact]
    public void CachesOutputsForUnchangedCompilation()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello", Description = "Say hello.")]
            public sealed class HelloCommand
            {
                public void Run()
                {
                }
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CreateCompilation(source, parseOptions);
        var driver = CSharpGeneratorDriver.Create(
            new[] { new RunesHelaGenerator().AsSourceGenerator() },
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var result = driver.GetRunResult();
        var reasons = result.Results[0].TrackedOutputSteps
            .SelectMany(static step => step.Value)
            .SelectMany(static step => step.Outputs)
            .Select(static output => output.Reason)
            .ToArray();

        Assert.Contains(reasons, static reason =>
            reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged);
    }

    [Fact]
    public void DiagnosticCatalogHasUniqueIds()
    {
        var ids = Diagnostics.All.Select(static descriptor => descriptor.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, static id => Assert.StartsWith("RHL", id));
    }

    [Fact]
    public void ReportsAliasConflicts()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello", Aliases = new[] { "info" }, Description = "Say hello.")]
            public sealed class HelloCommand
            {
                public void Run()
                {
                }
            }

            [Command("info", Description = "Show info.")]
            public sealed class InfoCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL028");
    }

    [Fact]
    public void ReportsAmbiguousConstructors()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("needs", Description = "Needs a dependency.")]
            public sealed class NeedsCommand
            {
                public NeedsCommand(string dependency)
                {
                }

                public NeedsCommand(int dependency)
                {
                }

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL020");
    }

    [Fact]
    public void GeneratesConstructorInjection()
    {
        const string source = """
            using System;
            using RunesHela;

            namespace Demo;

            [Command("di", Description = "Uses a service.")]
            public sealed class DiCommand
            {
                public DiCommand(IDisposable dependency)
                {
                }

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Assert.Contains("GetService(typeof(global::System.IDisposable))", result.GeneratedSource);
        Assert.Contains("ServiceMissing", result.GeneratedSource);
    }

    [Fact]
    public void GeneratesOptionAndArgumentBinding()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("deploy", Description = "Deploy a target.")]
            public sealed class DeployCommand
            {
                [Argument(0, Description = "Target.")]
                public string Target { get; init; } = "all";

                [Option(Short = 'e', Description = "Environment.", Env = "APP_ENV")]
                public string Environment { get; init; } = "dev";

                [Option(Description = "Force the deployment.")]
                public bool Force { get; init; }

                public int Run()
                {
                    return 0;
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Assert.Contains("GetEnvironmentVariable(\"APP_ENV\")", result.GeneratedSource);
        Assert.Contains("name.Equals(\"no-force\", global::System.StringComparison.OrdinalIgnoreCase)", result.GeneratedSource);
        Assert.Contains("Environment = o", result.GeneratedSource);
        Assert.Contains("Target = a", result.GeneratedSource);
    }

    [Fact]
    public void ReportsDuplicateOptionNames()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("dup", Description = "Duplicate options.")]
            public sealed class DupCommand
            {
                [Option("target", Description = "First.")]
                public string First { get; init; } = "";

                [Option("target", Description = "Second.")]
                public string Second { get; init; } = "";

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL002");
    }

    [Fact]
    public void ReportsRequiredOptionWithDefault()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("conflict", Description = "Required and default.")]
            public sealed class ConflictCommand
            {
                [Option(Description = "Count.", Required = true)]
                public int Count { get; init; } = 3;

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL003");
    }

    [Fact]
    public void ReportsNonConstantDefault()
    {
        const string source = """
            using System;
            using RunesHela;

            namespace Demo;

            [Command("dynamic", Description = "Dynamic default.")]
            public sealed class DynamicCommand
            {
                [Option(Description = "Name.")]
                public string Name { get; init; } = Environment.MachineName;

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL032");
    }

    [Fact]
    public void ReportsInvalidShortName()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("short", Description = "Bad short name.")]
            public sealed class ShortCommand
            {
                [Option(Short = '!', Description = "Value.")]
                public string Value { get; init; } = "";

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL033");
    }

    [Fact]
    public void ReportsFieldBinding()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("field", Description = "Field binding.")]
            public sealed class FieldCommand
            {
                [Option(Description = "Value.")]
                public string Value = "";

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL031");
    }

    [Fact]
    public void ReportsUnsupportedOptionType()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("unsupported", Description = "Unsupported option type.")]
            public sealed class UnsupportedCommand
            {
                [Option(Description = "Value.")]
                public object Value { get; init; } = new();

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL029");
    }

    [Fact]
    public void ReportsArrayArgumentThatIsNotLast()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("arrays", Description = "Array argument.")]
            public sealed class ArraysCommand
            {
                [Argument(0, Description = "Items.")]
                public string[] Items { get; init; } = System.Array.Empty<string>();

                [Argument(1, Description = "Tail.")]
                public string Tail { get; init; } = "";

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL011");
    }

    [Fact]
    public void ReportsOptionalArgumentBeforeRequired()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("order", Description = "Argument order.")]
            public sealed class OrderCommand
            {
                [Argument(0, Description = "Optional.")]
                public string? Optional { get; init; }

                [Argument(1, Description = "Required.")]
                public string Required2 { get; init; }

                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL009");
    }

    [Fact]
    public void ReportsUnsupportedReturnType()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("bad", Description = "Bad return type.")]
            public sealed class BadCommand
            {
                public string Run()
                {
                    return string.Empty;
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL021");
    }

    [Fact]
    public void ReportsInvalidFilterType()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("bad", Description = "Bad filter.", Filters = new[] { "System.Int32" })]
            public sealed class BadFilterCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL037");
    }

    [Fact]
    public void ReportsFilterContractViolation()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            public sealed class NotAFilter
            {
            }

            [Command("bad", Description = "Bad filter.", Filters = new[] { nameof(NotAFilter) })]
            public sealed class BadFilterCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL038");
    }

    [Fact]
    public void ReportsFilterWithoutParameterlessConstructor()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            using RunesHela;

            namespace Demo;

            public sealed class NeedsDependency : IRunesHelaFilter
            {
                public NeedsDependency(string dependency)
                {
                }

                public ValueTask<int> InvokeAsync(CommandContext context, Func<CommandContext, ValueTask<int>> next)
                {
                    return next(context);
                }
            }

            [Command("bad", Description = "Bad filter.", Filters = new[] { nameof(NeedsDependency) })]
            public sealed class BadFilterCommand
            {
                public void Run()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "RHL039");
    }

    internal static GeneratorResult RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CreateCompilation(source, parseOptions);

        var driver = CSharpGeneratorDriver.Create(
            new[] { new RunesHelaGenerator().AsSourceGenerator() },
            parseOptions: parseOptions);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var driverDiagnostics);

        var runResult = driver.GetRunResult();

        var generatedSource = string.Join(
            "\n",
            runResult.Results
                .SelectMany(static result => result.GeneratedSources)
                .Select(static result => result.SourceText.ToString()));

        var diagnostics = driverDiagnostics
            .Concat(runResult.Diagnostics)
            .ToImmutableArray();

        var compileErrors = outputCompilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        return new GeneratorResult(generatedSource, diagnostics, compileErrors);
    }

    internal static CSharpCompilation CreateCompilation(string source, CSharpParseOptions parseOptions)
    {
        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source, parseOptions) },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is not available.");

        var references = trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(RunesHela.CommandAttribute).Assembly.Location));

        return references.ToImmutableArray();
    }

    internal sealed record GeneratorResult(
        string GeneratedSource,
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableArray<Diagnostic> CompileErrors);
}
