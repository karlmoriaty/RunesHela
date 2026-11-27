using System.Text.Json;
using Xunit;

namespace RunesHela.Integration.Tests;

[CollectionDefinition("console", DisableParallelization = true)]
public sealed class ConsoleCollection
{
}

[Collection("console")]
public sealed class CliBehaviorTests
{
    [Fact]
    public async Task BindsArgumentAndOptions()
    {
        var result = await Cli.RunAsync(new[] { "greet", "Ada", "--count", "2", "--loud" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("HELLO, ADA!\nHELLO, ADA!\n", result.NormalizedOut);
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public async Task AppliesDefaultsWhenNothingIsSupplied()
    {
        var result = await Cli.RunAsync(new[] { "greet" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("Hello, world!\n", result.NormalizedOut);
    }

    [Fact]
    public async Task SupportsInlineShortAndCaseInsensitiveSyntax()
    {
        var result = await Cli.RunAsync(new[] { "greet", "-c=2", "--COUNT", "3", "--mode", "safe" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("Hello, world!\nHello, world!\nHello, world!\nmode=safe\n", result.NormalizedOut);
    }

    [Fact]
    public async Task SupportsRepeatedAndCommaSeparatedArrays()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--tags", "a,b", "-t", "c" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("tags=a,b,c", result.NormalizedOut);
    }

    [Fact]
    public async Task SupportsParamArrayArguments()
    {
        var result = await Cli.RunAsync(new[] { "sum", "1", "2", "3" });
        Assert.Equal("sum=6\n", result.NormalizedOut);

        var negatives = await Cli.RunAsync(new[] { "sum", "-1", "-2" });
        Assert.Equal("sum=-3\n", negatives.NormalizedOut);
    }

    [Fact]
    public async Task SupportsTerminatorForOptionLikeArguments()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--", "--count" });

        Assert.Equal("Hello, --count!\n", result.NormalizedOut);
    }

    [Fact]
    public async Task ReadsEnvironmentVariables()
    {
        var previous = Environment.GetEnvironmentVariable("RHL_GREET_COUNT");
        Environment.SetEnvironmentVariable("RHL_GREET_COUNT", "3");

        try
        {
            var result = await Cli.RunAsync(new[] { "greet" });
            Assert.Equal(3, result.NormalizedOut.Split("Hello, world!").Length - 1);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RHL_GREET_COUNT", previous);
        }
    }

    [Fact]
    public async Task CommandLineOverridesEnvironmentVariables()
    {
        var previous = Environment.GetEnvironmentVariable("RHL_GREET_COUNT");
        Environment.SetEnvironmentVariable("RHL_GREET_COUNT", "4");

        try
        {
            var result = await Cli.RunAsync(new[] { "greet", "--count", "2" });
            Assert.Equal(2, result.NormalizedOut.Split("Hello, world!").Length - 1);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RHL_GREET_COUNT", previous);
        }
    }

    [Fact]
    public async Task ReportsMissingRequiredOption()
    {
        var result = await Cli.RunAsync(new[] { "deploy" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Missing required option '--environment'.", result.NormalizedError);
        Assert.Equal(string.Empty, result.Out);
    }

    [Fact]
    public async Task BindsRequiredOptionAndArgument()
    {
        var result = await Cli.RunAsync(new[] { "deploy", "prod", "-e", "live" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("deploy prod -> live\n", result.NormalizedOut);
    }

    [Fact]
    public async Task ReportsMissingRequiredArgument()
    {
        var result = await Cli.RunAsync(new[] { "copy" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Missing required argument '<SOURCE>'.", result.NormalizedError);
    }

    [Fact]
    public async Task ReportsValidationFailure()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--count", "9" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Validation failed for 'count'.", result.NormalizedError);
        Assert.Contains("must be between 1 and 5", result.NormalizedError);
    }

    [Fact]
    public async Task ReportsUnknownOption()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--nope" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Unknown option '--nope'.", result.NormalizedError);
    }

    [Fact]
    public async Task ReportsUnknownCommand()
    {
        var result = await Cli.RunAsync(new[] { "bogus" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Unknown command 'bogus'.", result.NormalizedError);
        Assert.Contains("RunesHela.Integration.Tests --help", result.NormalizedError);
    }

    [Fact]
    public async Task ReportsUnknownSubcommand()
    {
        var result = await Cli.RunAsync(new[] { "parent", "bogus" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Unknown command 'parent bogus'.", result.NormalizedError);
    }

    [Fact]
    public async Task ReportsMissingOptionValue()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--count" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Option '--count' requires a value.", result.NormalizedError);
    }

    [Fact]
    public async Task ReportsInvalidEnumValueWithCandidates()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--mode", "turbo" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Invalid value 'turbo' for option '--mode'.", result.NormalizedError);
        Assert.Contains("one of: Fast, Safe", result.NormalizedError);
    }

    [Fact]
    public async Task SupportsCommandAliases()
    {
        var result = await Cli.RunAsync(new[] { "hi", "Ada" });

        Assert.Equal("Hello, Ada!\n", result.NormalizedOut);
    }

    [Fact]
    public async Task HelpListsArgumentsOptionsDefaultsAndExamples()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--help" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage: RunesHela.Integration.Tests greet [<NAME>] [options]", result.NormalizedOut);
        Assert.Contains("Arguments:", result.NormalizedOut);
        Assert.Contains("[<NAME>]", result.NormalizedOut);
        Assert.Contains("--count <NUMBER>", result.NormalizedOut);
        Assert.Contains("[default: 1]", result.NormalizedOut);
        Assert.Contains("[env: RHL_GREET_COUNT]", result.NormalizedOut);
        Assert.Contains("[secret]", result.NormalizedOut);
        Assert.Contains("Examples:", result.NormalizedOut);
        Assert.Contains("Exit codes:", result.NormalizedOut);
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public async Task NestedCommandHelpUsesFullPath()
    {
        var result = await Cli.RunAsync(new[] { "parent", "child", "leaf", "--help" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "Usage: RunesHela.Integration.Tests parent child leaf [options]",
            result.NormalizedOut);
        Assert.Contains("parent child leaf --value 3", result.NormalizedOut);
    }

    [Fact]
    public async Task JsonHelpExposesMachineReadableModel()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--help", "--json" });

        Assert.Equal(0, result.ExitCode);

        using var document = JsonDocument.Parse(result.Out);
        var root = document.RootElement;

        Assert.Equal("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("command", root.GetProperty("kind").GetString());
        Assert.Equal("greet", root.GetProperty("name").GetString());
        Assert.Equal("greet", root.GetProperty("path").GetString());

        var argument = root.GetProperty("arguments")[0];
        Assert.Equal("NAME", argument.GetProperty("name").GetString());
        Assert.Equal(0, argument.GetProperty("position").GetInt32());
        Assert.False(argument.GetProperty("required").GetBoolean());

        var count = FindOption(root, "count");
        Assert.Equal("NUMBER", count.GetProperty("valueName").GetString());
        Assert.True(count.GetProperty("hasDefault").GetBoolean());
        Assert.Equal(1, count.GetProperty("default").GetInt32());
        Assert.Equal("RHL_GREET_COUNT", count.GetProperty("env").GetString());

        var token = FindOption(root, "token");
        Assert.True(token.GetProperty("secret").GetBoolean());
        Assert.False(token.GetProperty("hasDefault").GetBoolean());
        Assert.Equal(JsonValueKind.Null, token.GetProperty("default").ValueKind);

        Assert.Equal(3, root.GetProperty("exitCodes").GetArrayLength());
    }

    [Fact]
    public async Task RootJsonHelpListsCommands()
    {
        var result = await Cli.RunAsync(new[] { "--help", "--json" });

        using var document = JsonDocument.Parse(result.Out);
        var root = document.RootElement;

        Assert.Equal("application", root.GetProperty("kind").GetString());
        Assert.Contains(
            root.GetProperty("commands").EnumerateArray(),
            static command => command.GetProperty("name").GetString() == "greet");
    }

    [Fact]
    public async Task JsonErrorsUseStableEnvelope()
    {
        var result = await Cli.RunAsync(new[] { "greet", "--nope", "--json" });

        Assert.Equal(2, result.ExitCode);

        using var document = JsonDocument.Parse(result.Error);
        var error = document.RootElement.GetProperty("error");

        Assert.False(document.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("RHL_USAGE_UNKNOWN_OPTION", error.GetProperty("code").GetString());
        Assert.Equal(2, error.GetProperty("exitCode").GetInt32());
        Assert.Contains(
            error.GetProperty("nextSteps").EnumerateArray(),
            static step => step.GetString()!.Contains("--help", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SecretValuesAreRedacted()
    {
        var help = await Cli.RunAsync(new[] { "greet", "--help" });
        Assert.Contains("[secret]", help.NormalizedOut);

        var invalid = await Cli.RunAsync(new[] { "greet", "--token", "supersecret", "--count", "9" });
        Assert.Equal(2, invalid.ExitCode);
        Assert.DoesNotContain("supersecret", invalid.NormalizedError);

        var invalidSecret = await Cli.RunAsync(new[] { "greet", "--pin", "not-a-number" });
        Assert.Equal(2, invalidSecret.ExitCode);
        Assert.Contains("***", invalidSecret.NormalizedError);
        Assert.DoesNotContain("not-a-number", invalidSecret.NormalizedError);
    }

    [Fact]
    public async Task AnsiEscapesAreOnlyWrittenWhenEnabled()
    {
        var plain = await Cli.RunAsync(new[] { "greet", "--nope" });
        Assert.True(plain.Error.IndexOf('\u001b') < 0);

        var colored = await Cli.RunAsync(new[] { "greet", "--nope" }, ansi: true);
        Assert.Contains("\u001b[31mError:\u001b[0m", colored.Error);
    }

    [Fact]
    public async Task HelpNeverContainsAnsiEscapes()
    {
        var result = await Cli.RunAsync(new[] { "--help" }, ansi: true);

        Assert.True(result.Out.IndexOf('\u001b') < 0);
    }

    [Fact]
    public async Task PropagatesHandlerExitCodes()
    {
        var result = await Cli.RunAsync(new[] { "fail" });
        Assert.Equal(7, result.ExitCode);
    }

    [Fact]
    public async Task PropagatesAsyncHandlerExitCodes()
    {
        var result = await Cli.RunAsync(new[] { "count" });

        Assert.Equal(4, result.ExitCode);
        Assert.Equal("counted\n", result.NormalizedOut);
    }

    [Fact]
    public async Task ReportsCommandsWithoutHandlers()
    {
        var result = await Cli.RunAsync(new[] { "pending" });

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("is not implemented", result.NormalizedError);
    }

    [Fact]
    public async Task ResolvesConstructorDependencies()
    {
        var services = new SimpleServiceProvider();
        services.Add(typeof(IGreeter), new Greeter());

        var result = await Cli.RunAsync(new[] { "di" }, services);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hi di\n", result.NormalizedOut);
    }

    [Fact]
    public async Task ReportsMissingServiceProvider()
    {
        var result = await Cli.RunAsync(new[] { "di" });

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("requires a service provider", result.NormalizedError);
    }

    private static JsonElement FindOption(JsonElement root, string name)
    {
        foreach (var option in root.GetProperty("options").EnumerateArray())
        {
            if (option.GetProperty("name").GetString() == name)
            {
                return option;
            }
        }

        throw new InvalidOperationException("Option '" + name + "' was not found.");
    }

    private sealed class Greeter : IGreeter
    {
        public string Greet(string name)
        {
            return "hi " + name;
        }
    }
}
