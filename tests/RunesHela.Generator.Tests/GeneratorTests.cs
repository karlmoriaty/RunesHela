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
        Assert.Contains("RunCommand0Async", result.GeneratedSource);
        Assert.Contains("RunesHelaApp.RegisterEntry", result.GeneratedSource);
    }

    [Fact]
    public void ReportsDuplicateCommandNames()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("dup")]
            public sealed class FirstCommand
            {
                public void Run()
                {
                }
            }

            [Command("dup")]
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

            [Command("noop")]
            public sealed class NoopCommand
            {
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Id == "RHL006" && diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.Contains("is not implemented.", result.GeneratedSource);
    }

    [Fact]
    public void ReportsNestedCommands()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("outer")]
            public sealed class OuterCommand
            {
                public void Run()
                {
                }

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
    public void ReportsUnsupportedHandlerParameters()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello")]
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

            [Command("info")]
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

    private static GeneratorResult RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

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

    private sealed record GeneratorResult(
        string GeneratedSource,
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableArray<Diagnostic> CompileErrors);
}
