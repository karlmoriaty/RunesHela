namespace RunesHela;

/// <summary>
/// Canonical process exit codes used by generated applications.
/// </summary>
public static class RunesHelaExitCode
{
    /// <summary>The command completed successfully.</summary>
    public const int Success = 0;

    /// <summary>A general failure, including unexpected exceptions.</summary>
    public const int Failure = 1;

    /// <summary>A usage error: unknown command or option, missing or invalid value.</summary>
    public const int Usage = 2;

    /// <summary>A configuration error.</summary>
    public const int Configuration = 3;

    /// <summary>A requested resource was not found.</summary>
    public const int NotFound = 4;

    /// <summary>Authentication or authorization failed.</summary>
    public const int Permission = 5;

    /// <summary>The operation conflicted with the current state.</summary>
    public const int Conflict = 6;

    /// <summary>The operation timed out.</summary>
    public const int Timeout = 7;

    /// <summary>The operation was cancelled by a signal (128 + SIGINT).</summary>
    public const int Interrupted = 130;

    /// <summary>The output pipe was closed by the reader (128 + SIGPIPE).</summary>
    public const int BrokenPipe = 141;
}
