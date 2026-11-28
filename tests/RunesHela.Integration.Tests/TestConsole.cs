using RunesHela;

namespace RunesHela.Integration.Tests;

internal sealed class TestConsole : IInteractiveConsole
{
    private readonly TextWriter _out;
    private readonly TextWriter _error;
    private readonly TextReader _in;

    public TestConsole(TextWriter output, TextWriter error, TextReader? input = null)
    {
        _out = output;
        _error = error;
        _in = input ?? TextReader.Null;
    }

    public TextWriter Out => _out;

    public TextWriter Error => _error;

    public TextReader In => _in;

    public bool IsOutputRedirected => true;

    public bool IsInputRedirected { get; init; } = true;

    public bool IsAnsiSupported { get; init; }
}

internal sealed record CliResult(int ExitCode, string Out, string Error)
{
    public string NormalizedOut => Out.Replace("\r\n", "\n");

    public string NormalizedError => Error.Replace("\r\n", "\n");
}

internal static class Cli
{
    public static async Task<CliResult> RunAsync(
        string[] args,
        IServiceProvider? services = null,
        bool ansi = false,
        string? input = null,
        bool inputRedirected = true)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var console = new TestConsole(output, error, input is null ? null : new StringReader(input))
        {
            IsAnsiSupported = ansi,
            IsInputRedirected = inputRedirected,
        };

        var exitCode = await RunWithConsoleAsync(args, console, services).ConfigureAwait(false);
        return new CliResult(exitCode, output.ToString(), error.ToString());
    }

    public static Task<int> RunWithOutputAsync(string[] args, TextWriter output)
    {
        var console = new TestConsole(output, new StringWriter());
        return RunWithConsoleAsync(args, console, services: null);
    }

    private static async Task<int> RunWithConsoleAsync(
        string[] args,
        TestConsole console,
        IServiceProvider? services)
    {
        var previousConsole = RunesHelaConsole.Current;
        var previousInteractive = RunesHelaConsole.Interactive;
        var previousOut = Console.Out;
        var previousError = Console.Error;

        RunesHelaConsole.Current = console;
        RunesHelaConsole.Interactive = console;
        Console.SetOut(console.Out);
        Console.SetError(console.Error);

        try
        {
            return services is null
                ? await RunesHelaApp.RunAsync(args).ConfigureAwait(false)
                : await RunesHelaApp.RunAsync(args, services).ConfigureAwait(false);
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
            RunesHelaConsole.Current = previousConsole;
            RunesHelaConsole.Interactive = previousInteractive;
        }
    }
}

internal sealed class SimpleServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public object? GetService(Type serviceType)
    {
        return _services.TryGetValue(serviceType, out var value) ? value : null;
    }

    public void Add(Type serviceType, object instance)
    {
        _services[serviceType] = instance;
    }
}

internal sealed class ThrowingWriter : TextWriter
{
    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

    public override void Write(char value)
    {
        throw new IOException("The pipe is closed.");
    }

    public override void Write(string? value)
    {
        throw new IOException("The pipe is closed.");
    }

    public override void WriteLine(string? value)
    {
        throw new IOException("The pipe is closed.");
    }
}
