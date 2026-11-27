namespace RunesHela;

/// <summary>
/// Abstraction over standard output, standard error, and terminal capability detection.
/// </summary>
public interface IConsole
{
    /// <summary>
    /// Gets the writer used for command data.
    /// </summary>
    TextWriter Out { get; }

    /// <summary>
    /// Gets the writer used for diagnostics, errors, and progress.
    /// </summary>
    TextWriter Error { get; }

    /// <summary>
    /// Gets a value indicating whether standard output is redirected.
    /// </summary>
    bool IsOutputRedirected { get; }
}

/// <summary>
/// Provides the ambient console used by generated command entry points.
/// </summary>
public static class RunesHelaConsole
{
    private static IConsole s_current = SystemConsole.Instance;

    /// <summary>
    /// Gets or sets the console used by the runtime. Intended for tests and hosts.
    /// </summary>
    public static IConsole Current
    {
        get => s_current;
        set => s_current = value ?? throw new ArgumentNullException(nameof(value));
    }

    private sealed class SystemConsole : IConsole
    {
        public static readonly SystemConsole Instance = new();

        public TextWriter Out => Console.Out;

        public TextWriter Error => Console.Error;

        public bool IsOutputRedirected => Console.IsOutputRedirected;
    }
}
