namespace RunesHela;

/// <summary>
/// Describes a structured error that is safe for humans, scripts, and AI agents to consume.
/// </summary>
public sealed class RunesHelaError
{
    /// <summary>
    /// Gets the stable machine-readable error code, such as <c>RHL_USAGE_UNKNOWN_OPTION</c>.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets an optional hint that suggests the likely fix.
    /// </summary>
    public string? Hint { get; init; }

    /// <summary>
    /// Gets the ordered list of next steps a caller can take to recover.
    /// </summary>
    public IReadOnlyList<string> NextSteps { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets an optional documentation URL for the error code.
    /// </summary>
    public string? DocsUrl { get; init; }

    /// <summary>
    /// Gets the process exit code that represents the error.
    /// </summary>
    public int ExitCode { get; init; } = 1;

    /// <summary>
    /// Gets the command path that produced the error, or <see langword="null"/> for the application root.
    /// </summary>
    public string? CommandPath { get; init; }
}
