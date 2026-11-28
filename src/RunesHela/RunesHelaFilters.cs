namespace RunesHela;

/// <summary>
/// Describes the command being invoked for filter pipelines.
/// </summary>
public sealed class CommandContext
{
    /// <summary>Gets the command path, such as <c>remote add</c>.</summary>
    public required string CommandPath { get; init; }

    /// <summary>Gets the service provider, or <see langword="null"/> when the app was run without one.</summary>
    public IServiceProvider? Services { get; init; }

    /// <summary>Gets the cancellation token supplied to the application.</summary>
    public CancellationToken CancellationToken { get; init; }
}

/// <summary>
/// Wraps command invocation to add timing, logging, authentication, or retries.
/// </summary>
public interface IRunesHelaFilter
{
    /// <summary>
    /// Invokes the next stage in the pipeline.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="next">The continuation that runs the handler.</param>
    /// <returns>The process exit code.</returns>
    ValueTask<int> InvokeAsync(CommandContext context, Func<CommandContext, ValueTask<int>> next);
}
