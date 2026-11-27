using RunesHela;

namespace RunesHela.Integration.Tests;

internal sealed class TestConsole : IConsole
{
    private readonly StringWriter _out;
    private readonly StringWriter _error;

    public TestConsole(StringWriter output, StringWriter error)
    {
        _out = output;
        _error = error;
    }

    public TextWriter Out => _out;

    public TextWriter Error => _error;

    public bool IsOutputRedirected => true;

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
        bool ansi = false)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var console = new TestConsole(output, error) { IsAnsiSupported = ansi };
        var previousConsole = RunesHelaConsole.Current;
        var previousOut = Console.Out;
        var previousError = Console.Error;

        RunesHelaConsole.Current = console;
        Console.SetOut(output);
        Console.SetError(error);

        try
        {
            var exitCode = services is null
                ? await RunesHelaApp.RunAsync(args)
                : await RunesHelaApp.RunAsync(args, services);

            return new CliResult(exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
            RunesHelaConsole.Current = previousConsole;
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
