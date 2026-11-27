using System.ComponentModel;
using System.Reflection;

namespace RunesHela;

/// <summary>
/// Entry point for generated RunesHela applications.
/// </summary>
public static class RunesHelaApp
{
    private static Func<string[], CancellationToken, ValueTask<int>>? s_entry;

    /// <summary>
    /// Registers the generated entry point. Called by generated module initializers.
    /// </summary>
    /// <param name="entry">The generated entry delegate.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterEntry(Func<string[], CancellationToken, ValueTask<int>> entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        s_entry = entry;
    }

    /// <summary>
    /// Runs the application using the process command-line arguments.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public static int Run()
    {
        return Run(Environment.GetCommandLineArgs()[1..]);
    }

    /// <summary>
    /// Runs the application synchronously.
    /// </summary>
    /// <param name="args">Command-line arguments, excluding the executable name.</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args)
    {
        return RunAsync(args).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs the application asynchronously.
    /// </summary>
    /// <param name="args">Command-line arguments, excluding the executable name.</param>
    /// <param name="cancellationToken">A token used to cancel the command.</param>
    /// <returns>The process exit code.</returns>
    public static ValueTask<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (s_entry is null)
        {
            RunesHelaConsole.Current.Error.WriteLine(
                "RunesHela: no generated entry point was found. Add [Command] to a class and reference the RunesHela generator.");
            return new ValueTask<int>(2);
        }

        return s_entry(args, cancellationToken);
    }

    /// <summary>
    /// Gets the informational version of the entry assembly.
    /// </summary>
    /// <returns>A version string such as <c>1.2.3</c>.</returns>
    public static string GetVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null ? "0.0.0" : version.ToString(3);
    }
}
