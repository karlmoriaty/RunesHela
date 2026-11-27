using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RunesHela.Generator;
using Xunit;
using Xunit.Abstractions;

namespace RunesHela.Generator.Tests;

public sealed class GeneratorPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public GeneratorPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(200)]
    public void NoOpRunStaysWithinBudget(int commandCount)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = GeneratorTests.CreateCompilation(BuildCommands(commandCount), parseOptions);

        var driver = CSharpGeneratorDriver.Create(
            new[] { new RunesHelaGenerator().AsSourceGenerator() },
            parseOptions: parseOptions);

        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var best = double.MaxValue;

        for (var round = 0; round < 3; round++)
        {
            var stopwatch = Stopwatch.StartNew();
            driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
            stopwatch.Stop();

            if (stopwatch.Elapsed.TotalMilliseconds < best)
            {
                best = stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        _output.WriteLine($"{commandCount} commands: {best:F2} ms (no-op)");

        Assert.True(best < 50.0, $"{commandCount} commands took {best:F2} ms");
    }

    private static string BuildCommands(int count)
    {
        var builder = new StringBuilder(count * 160);
        builder.AppendLine("using RunesHela;");
        builder.AppendLine();
        builder.AppendLine("namespace Demo;");
        builder.AppendLine();

        for (var i = 0; i < count; i++)
        {
            builder.Append("[Command(\"cmd").Append(i).Append("\", Description = \"Command ").Append(i).AppendLine(".\")]");
            builder.Append("public sealed class Cmd").Append(i).AppendLine(" { public void Run() { } }");
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
