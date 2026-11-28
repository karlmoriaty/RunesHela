using System.ComponentModel;
using System.Reflection;

namespace RunesHela;

/// <summary>
/// Entry point for generated RunesHela applications.
/// </summary>
public static class RunesHelaApp
{
    private static Func<string[], IServiceProvider?, CancellationToken, ValueTask<int>>? s_entry;

    /// <summary>
    /// Registers the generated entry point. Called by generated module initializers.
    /// </summary>
    /// <param name="entry">The generated entry delegate.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterEntry(Func<string[], IServiceProvider?, CancellationToken, ValueTask<int>> entry)
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
        return RunProtected(args, services: null, CancellationToken.None);
    }

    /// <summary>
    /// Runs the application synchronously with a service provider for constructor injection.
    /// </summary>
    /// <param name="args">Command-line arguments, excluding the executable name.</param>
    /// <param name="services">The service provider used to resolve constructor dependencies.</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return RunProtected(args, services, CancellationToken.None);
    }

    /// <summary>
    /// Runs the application asynchronously.
    /// </summary>
    /// <param name="args">Command-line arguments, excluding the executable name.</param>
    /// <param name="cancellationToken">A token used to cancel the command.</param>
    /// <returns>The process exit code.</returns>
    public static ValueTask<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        return RunProtectedAsync(args, services: null, cancellationToken);
    }

    /// <summary>
    /// Runs the application asynchronously with a service provider for constructor injection.
    /// </summary>
    /// <param name="args">Command-line arguments, excluding the executable name.</param>
    /// <param name="services">The service provider used to resolve constructor dependencies.</param>
    /// <param name="cancellationToken">A token used to cancel the command.</param>
    /// <returns>The process exit code.</returns>
    public static ValueTask<int> RunAsync(
        string[] args,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        return RunProtectedAsync(args, services, cancellationToken);
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

    private static ValueTask<int> RunProtectedAsync(
        string[] args,
        IServiceProvider? services,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (s_entry is null)
        {
            return new ValueTask<int>(WriteMissingEntry());
        }

        try
        {
            return s_entry(args, services, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<int>(RunesHelaExitCode.Interrupted);
        }
        catch (IOException)
        {
            return new ValueTask<int>(RunesHelaExitCode.BrokenPipe);
        }
        catch (Exception exception)
        {
            return new ValueTask<int>(WriteUnexpected(exception, args));
        }
    }

    private static int RunProtected(string[] args, IServiceProvider? services, CancellationToken cancellationToken)
    {
        var task = RunProtectedAsync(args, services, cancellationToken);

        return task.IsCompletedSuccessfully
            ? task.Result
            : task.AsTask().GetAwaiter().GetResult();
    }

    private static int WriteMissingEntry()
    {
        RunesHelaConsole.Current.Error.WriteLine(
            "RunesHela: no generated entry point was found. Add [Command] to a class and reference the RunesHela generator.");
        return RunesHelaExitCode.Failure;
    }

    private static int WriteUnexpected(Exception exception, string[] args)
    {
        try
        {
            return RunesHelaErrors.Unexpected(
                exception,
                RunesHelaOutputOptions.WantsVerbose(args),
                RunesHelaOutputOptions.WantsJson(args));
        }
        catch (IOException)
        {
            return RunesHelaExitCode.BrokenPipe;
        }
    }
}
