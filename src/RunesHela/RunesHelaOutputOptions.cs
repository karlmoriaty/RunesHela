namespace RunesHela;

/// <summary>
/// Scans command-line arguments for the global options shared by every generated application.
/// </summary>
public static class RunesHelaOutputOptions
{
    /// <summary>
    /// Gets a value indicating whether JSON output was requested.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see langword="true"/> for <c>--json</c> or <c>--format json</c>.</returns>
    public static bool WantsJson(ReadOnlySpan<string> args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg == "--")
            {
                break;
            }

            if (arg == "--json")
            {
                return true;
            }

            if (arg == "--format")
            {
                if (i + 1 < args.Length && args[i + 1].Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                i++;
                continue;
            }

            if (arg.StartsWith("--format=", StringComparison.Ordinal))
            {
                return IsSupportedFormat(arg.AsSpan(9)) && arg.AsSpan(9).Equals("json", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether help was requested.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see langword="true"/> for <c>-h</c> or <c>--help</c>.</returns>
    public static bool WantsHelp(ReadOnlySpan<string> args)
    {
        foreach (var arg in args)
        {
            if (arg == "--")
            {
                break;
            }

            if (arg == "-h" || arg == "--help")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether verbose diagnostics were requested.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see langword="true"/> for <c>--verbose</c> or <c>-v</c>.</returns>
    public static bool WantsVerbose(ReadOnlySpan<string> args)
    {
        return HasFlag(args, "--verbose", "-v");
    }

    /// <summary>
    /// Gets a value indicating whether interactive prompts were disabled.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see langword="true"/> for <c>--no-input</c>.</returns>
    public static bool WantsNoInput(ReadOnlySpan<string> args)
    {
        return HasFlag(args, "--no-input", null);
    }

    /// <summary>
    /// Gets a value indicating whether destructive operations were pre-confirmed.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see langword="true"/> for <c>--yes</c> or <c>-y</c>.</returns>
    public static bool WantsYes(ReadOnlySpan<string> args)
    {
        return HasFlag(args, "--yes", "-y");
    }

    /// <summary>
    /// Gets a value indicating whether the name is a global option that does not consume a value.
    /// </summary>
    /// <param name="name">The option name without leading dashes.</param>
    /// <returns><see langword="true"/> for global flags.</returns>
    public static bool IsGlobalFlag(ReadOnlySpan<char> name)
    {
        return name.Equals("json", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("plain", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("no-color", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("quiet", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("verbose", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("no-input", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a value indicating whether a format value is supported.
    /// </summary>
    /// <param name="value">The format value.</param>
    /// <returns><see langword="true"/> for <c>text</c>, <c>plain</c>, or <c>json</c>.</returns>
    public static bool IsSupportedFormat(ReadOnlySpan<char> value)
    {
        return value.Equals("text", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("plain", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasFlag(ReadOnlySpan<string> args, string longName, string? shortName)
    {
        foreach (var arg in args)
        {
            if (arg == "--")
            {
                break;
            }

            if (arg == longName || (shortName is not null && arg == shortName))
            {
                return true;
            }
        }

        return false;
    }
}
