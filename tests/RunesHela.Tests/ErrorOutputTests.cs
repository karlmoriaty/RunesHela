using System.Text.Json;
using RunesHela;
using RunesHela.Parsing;
using Xunit;

namespace RunesHela.Tests;

public sealed class ErrorOutputTests
{
    [Fact]
    public void WritesPlainTextWhenAnsiIsDisabled()
    {
        var console = new CaptureConsole { IsAnsiSupported = false };

        var exitCode = WithConsole(console, () => ErrorOutput.Write(CreateError()));

        Assert.Equal(2, exitCode);
        Assert.Equal(
            "Error: Unknown option '--port'.\nHint: Did you mean '--port-number'?\nNext: Run 'deploy --help'.\n",
            Normalize(console.ErrorText));
        Assert.True(console.ErrorText.IndexOf('\u001b') < 0);
    }

    [Fact]
    public void WritesAnsiWhenEnabled()
    {
        var console = new CaptureConsole { IsAnsiSupported = true };

        WithConsole(console, () => ErrorOutput.Write(CreateError()));

        Assert.Contains("\u001b[31mError:\u001b[0m Unknown option '--port'.", console.ErrorText);
    }

    [Fact]
    public void WritesJsonEnvelope()
    {
        var console = new CaptureConsole();
        var error = new RunesHelaError
        {
            Code = "RHL_USAGE_UNKNOWN_OPTION",
            Message = "Quote \" and backslash \\ must be escaped.",
            Hint = null,
            ExitCode = 2,
            CommandPath = "deploy",
            DocsUrl = "https://example.test/errors",
            NextSteps = new[] { "Run 'deploy --help'." },
        };

        WithConsole(console, () => ErrorOutput.Write(error, json: true));

        using var document = JsonDocument.Parse(console.ErrorText);
        var root = document.RootElement;
        var payload = root.GetProperty("error");

        Assert.False(root.GetProperty("ok").GetBoolean());
        Assert.Equal("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("RHL_USAGE_UNKNOWN_OPTION", payload.GetProperty("code").GetString());
        Assert.Equal("Quote \" and backslash \\ must be escaped.", payload.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("hint").ValueKind);
        Assert.Equal("deploy", payload.GetProperty("command").GetString());
        Assert.Equal(2, payload.GetProperty("exitCode").GetInt32());
        Assert.Equal(1, payload.GetProperty("nextSteps").GetArrayLength());
    }

    [Fact]
    public void ParsesValuesWithInvariantCulture()
    {
        Assert.True(ValueParser.TryParse<int>("42", out var number));
        Assert.Equal(42, number);

        Assert.True(ValueParser.TryParse<double>("1.5", out var real));
        Assert.Equal(1.5, real);

        Assert.True(ValueParser.TryParse<Guid>("6f9619ff-8b86-d011-b42d-00c04fc964ff", out var guid));
        Assert.NotEqual(Guid.Empty, guid);

        Assert.False(ValueParser.TryParse<int>("4x2", out _));
    }

    [Fact]
    public void ReusesWholeArgumentStrings()
    {
        const string text = "hello";

        Assert.Same(text, SpanText.ToString(text.AsMemory()));
        Assert.Equal("ell", SpanText.ToString(text.AsMemory(1, 3)));
    }

    [Fact]
    public void TokenExposesDashCount()
    {
        var longReader = new CommandLineReader(new[] { "--name" });
        Assert.True(longReader.TryRead(out var longToken));
        Assert.Equal(2, longToken.PrefixLength);

        var shortReader = new CommandLineReader(new[] { "-n" });
        Assert.True(shortReader.TryRead(out var shortToken));
        Assert.Equal(1, shortToken.PrefixLength);
    }

    private static RunesHelaError CreateError()
    {
        return new RunesHelaError
        {
            Code = "RHL_USAGE_UNKNOWN_OPTION",
            Message = "Unknown option '--port'.",
            Hint = "Did you mean '--port-number'?",
            ExitCode = 2,
            CommandPath = "deploy",
            NextSteps = new[] { "Run 'deploy --help'." },
        };
    }

    private static int WithConsole(CaptureConsole console, Func<int> action)
    {
        var previous = RunesHelaConsole.Current;
        RunesHelaConsole.Current = console;

        try
        {
            return action();
        }
        finally
        {
            RunesHelaConsole.Current = previous;
        }
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    private sealed class CaptureConsole : IConsole
    {
        private readonly StringWriter _out = new();
        private readonly StringWriter _error = new();

        public TextWriter Out => _out;

        public TextWriter Error => _error;

        public bool IsOutputRedirected => true;

        public bool IsAnsiSupported { get; init; }

        public string ErrorText => _error.ToString();
    }
}
