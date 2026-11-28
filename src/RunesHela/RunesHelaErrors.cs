namespace RunesHela;

/// <summary>
/// Creates the structured errors emitted by generated command binders.
/// </summary>
public static class RunesHelaErrors
{
    private const string DocsBase = "https://github.com/karlmoriaty/RunesHela/blob/main/docs/errors.md#";

    private const string UnknownCommandDocs = DocsBase + "rhl_usage_unknown_command";
    private const string UnknownOptionDocs = DocsBase + "rhl_usage_unknown_option";
    private const string MissingValueDocs = DocsBase + "rhl_usage_missing_value";
    private const string InvalidValueDocs = DocsBase + "rhl_usage_invalid_value";
    private const string MissingRequiredDocs = DocsBase + "rhl_usage_missing_required";
    private const string UnexpectedArgumentDocs = DocsBase + "rhl_usage_unexpected_argument";
    private const string ValidationFailedDocs = DocsBase + "rhl_usage_validation_failed";
    private const string ConfigNotFoundDocs = DocsBase + "rhl_config_not_found";
    private const string ConfigInvalidDocs = DocsBase + "rhl_config_invalid";
    private const string ConfigInvalidValueDocs = DocsBase + "rhl_config_invalid_value";
    private const string ServicesMissingDocs = DocsBase + "rhl_di_services_missing";
    private const string ServiceMissingDocs = DocsBase + "rhl_di_service_missing";
    private const string NotImplementedDocs = DocsBase + "rhl_internal_not_implemented";
    private const string ConfirmationRequiredDocs = DocsBase + "rhl_usage_confirmation_required";
    private const string AbortedDocs = DocsBase + "rhl_usage_aborted";
    private const string UnsupportedFormatDocs = DocsBase + "rhl_usage_unsupported_format";
    private const string UnknownShellDocs = DocsBase + "rhl_usage_unknown_shell";
    private const string UnexpectedErrorDocs = DocsBase + "rhl_internal_unexpected";

    /// <summary>
    /// Writes an unknown command error.
    /// </summary>
    /// <param name="command">The unrecognized command name.</param>
    /// <param name="appName">The application name used in guidance.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <param name="candidates">Known command names used for a did-you-mean hint.</param>
    /// <returns>The exit code.</returns>
    public static int UnknownCommand(string command, string appName, bool json, string[]? candidates = null)
    {
        var suggestion = DidYouMean.Nearest(command, candidates);

        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnknownCommand,
                Message = "Unknown command '" + command + "'.",
                Hint = suggestion is null ? null : "Did you mean '" + suggestion + "'?",
                ExitCode = RunesHelaExitCode.Usage,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.UnknownCommand),
                NextSteps = new[] { "Run '" + appName + " --help' to list commands." },
            },
            json);
    }

    /// <summary>
    /// Writes an unknown subcommand error.
    /// </summary>
    /// <param name="commandPath">The parent command path.</param>
    /// <param name="command">The unrecognized subcommand name.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <param name="candidates">Known subcommand names used for a did-you-mean hint.</param>
    /// <returns>The exit code.</returns>
    public static int UnknownSubcommand(
        string commandPath,
        string command,
        bool json,
        string[]? candidates = null)
    {
        var suggestion = DidYouMean.Nearest(command, candidates);

        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnknownCommand,
                Message = "Unknown command '" + commandPath + " " + command + "'.",
                Hint = suggestion is null ? null : "Did you mean '" + commandPath + " " + suggestion + "'?",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.UnknownCommand),
                NextSteps = new[] { "Run '" + commandPath + " --help' to list subcommands." },
            },
            json);
    }

    /// <summary>
    /// Writes an unknown option error.
    /// </summary>
    /// <param name="commandPath">The command path, or an empty string for the application root.</param>
    /// <param name="optionName">The unrecognized option name without dashes.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <param name="candidates">Known option names used for a did-you-mean hint.</param>
    /// <returns>The exit code.</returns>
    public static int UnknownOption(
        string commandPath,
        string optionName,
        bool json,
        string[]? candidates = null)
    {
        var suggestion = DidYouMean.Nearest(optionName, candidates);

        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnknownOption,
                Message = "Unknown option '--" + optionName + "'.",
                Hint = suggestion is null ? null : "Did you mean '--" + suggestion + "'?",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.UnknownOption),
                NextSteps = new[] { HelpStep(commandPath), "Use '--' to pass the remaining arguments through." },
            },
            json);
    }

    /// <summary>
    /// Writes a missing option value error.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="optionName">The option name without dashes.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int MissingValue(string commandPath, string optionName, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.MissingValue,
                Message = "Option '--" + optionName + "' requires a value.",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.MissingValue),
                NextSteps = new[] { "Pass the value as '--" + optionName + " <VALUE>'.", HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes an invalid option value error.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="optionName">The option name without dashes.</param>
    /// <param name="value">The supplied value, or <see langword="null"/> when it must be redacted.</param>
    /// <param name="expected">A description of the expected value.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int InvalidValue(string commandPath, string optionName, string? value, string expected, bool json)
    {
        var display = value is null ? "***" : "'" + value + "'";

        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.InvalidValue,
                Message = "Invalid value " + display + " for option '--" + optionName + "'.",
                Hint = "Expected " + expected + ".",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.InvalidValue),
                NextSteps = new[] { HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes an invalid argument value error.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="argumentName">The argument name shown in help.</param>
    /// <param name="value">The supplied value, or <see langword="null"/> when it must be redacted.</param>
    /// <param name="expected">A description of the expected value.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int InvalidArgumentValue(
        string commandPath,
        string argumentName,
        string? value,
        string expected,
        bool json)
    {
        var display = value is null ? "***" : "'" + value + "'";

        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.InvalidValue,
                Message = "Invalid value " + display + " for argument '<" + argumentName + ">'.",
                Hint = "Expected " + expected + ".",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.InvalidValue),
                NextSteps = new[] { HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes a missing required option error.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="optionName">The option name without dashes.</param>
    /// <param name="envVar">The environment variable that can supply the value, or <see langword="null"/>.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int MissingRequiredOption(string commandPath, string optionName, string? envVar, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.MissingRequired,
                Message = "Missing required option '--" + optionName + "'.",
                Hint = envVar is null ? null : "Set '" + envVar + "' or pass the option explicitly.",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.MissingRequired),
                NextSteps = new[] { "Pass '--" + optionName + " <VALUE>'.", HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes a missing required argument error.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="argumentName">The argument name shown in help.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int MissingRequiredArgument(string commandPath, string argumentName, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.MissingRequired,
                Message = "Missing required argument '<" + argumentName + ">'.",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.MissingRequired),
                NextSteps = new[] { HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes an unexpected positional argument error.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="value">The unexpected value.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int UnexpectedArgument(string commandPath, string value, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnexpectedArgument,
                Message = "Unexpected argument '" + value + "'.",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.UnexpectedArgument),
                NextSteps = new[] { HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes a validation failure error.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="name">The option or argument name.</param>
    /// <param name="message">The validation message.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int ValidationFailed(string commandPath, string name, string message, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.ValidationFailed,
                Message = "Validation failed for '" + name + "'.",
                Hint = message,
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.ValidationFailed),
                NextSteps = new[] { HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes an error for a configuration file that was requested but does not exist.
    /// </summary>
    /// <param name="path">The configuration file path.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <param name="command">The command path, or <see langword="null"/>.</param>
    /// <returns>The exit code.</returns>
    public static int ConfigNotFound(string path, bool json, string? command = null)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.ConfigNotFound,
                Message = "Configuration file '" + path + "' was not found.",
                ExitCode = RunesHelaExitCode.Configuration,
                CommandPath = command,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.ConfigNotFound),
                NextSteps = new[]
                {
                    "Pass '--config <PATH>' with an existing file.",
                    "Unset 'RHL_CONFIG' to use an implicit path.",
                },
            },
            json);
    }

    /// <summary>
    /// Writes an error for a configuration file that cannot be read or parsed.
    /// </summary>
    /// <param name="path">The configuration file path.</param>
    /// <param name="detail">A value-free description of the problem.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <param name="command">The command path, or <see langword="null"/>.</param>
    /// <returns>The exit code.</returns>
    public static int ConfigInvalid(string path, string detail, bool json, string? command = null)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.ConfigInvalid,
                Message = "Configuration file '" + path + "' is invalid.",
                Hint = detail,
                ExitCode = RunesHelaExitCode.Configuration,
                CommandPath = command,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.ConfigInvalid),
                NextSteps = new[] { "Fix the file, or pass '--config <PATH>' to use another one." },
            },
            json);
    }

    /// <summary>
    /// Writes an error for a configuration value that cannot be converted to the option type.
    /// The value is never included in the message so secrets stay redacted.
    /// </summary>
    /// <param name="path">The configuration file path.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="expected">A description of the expected value.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <param name="command">The command path, or <see langword="null"/>.</param>
    /// <returns>The exit code.</returns>
    public static int ConfigInvalidValue(
        string path,
        string key,
        string expected,
        bool json,
        string? command = null)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.ConfigInvalidValue,
                Message = "Configuration value for '" + key + "' in '" + path + "' is invalid.",
                Hint = "Expected " + expected + ".",
                ExitCode = RunesHelaExitCode.Configuration,
                CommandPath = command,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.ConfigInvalidValue),
                NextSteps = new[] { "Fix '" + key + "', or pass the option explicitly on the command line." },
            },
            json);
    }

    /// <summary>
    /// Writes an error for a command that needs services but received no service provider.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int ServicesMissing(string commandPath, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.ServicesMissing,
                Message = "Command '" + commandPath + "' requires a service provider.",
                Hint = "Call RunesHelaApp.RunAsync(args, services) to supply dependencies.",
                ExitCode = RunesHelaExitCode.Failure,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.ServicesMissing),
            },
            json);
    }

    /// <summary>
    /// Writes an error for an unresolvable constructor dependency.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="serviceType">The service type name.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int ServiceMissing(string commandPath, string serviceType, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.ServiceMissing,
                Message = "Command '" + commandPath + "' could not resolve service '" + serviceType + "'.",
                ExitCode = RunesHelaExitCode.Failure,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.ServiceMissing),
                NextSteps = new[] { "Register '" + serviceType + "' in the IServiceProvider." },
            },
            json);
    }

    /// <summary>
    /// Writes an error for a command written without a handler method.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int NotImplemented(string commandPath, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.NotImplemented,
                Message = "Command '" + commandPath + "' is not implemented.",
                ExitCode = RunesHelaExitCode.Failure,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.NotImplemented),
                NextSteps = new[] { "Add a public Run, RunAsync, Execute, or ExecuteAsync method." },
            },
            json);
    }

    /// <summary>
    /// Writes an error for a destructive command that was not confirmed.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int ConfirmationRequired(string commandPath, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.ConfirmationRequired,
                Message = "Command '" + commandPath + "' is destructive and requires confirmation.",
                Hint = "Pass '--yes' to confirm and run without prompting.",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.ConfirmationRequired),
                NextSteps = new[] { "Pass '--yes' to confirm.", HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes an error for a declined interactive confirmation.
    /// </summary>
    /// <param name="commandPath">The command path.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int Aborted(string commandPath, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.Aborted,
                Message = "Command '" + commandPath + "' was not confirmed.",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.Aborted),
            },
            json);
    }

    /// <summary>
    /// Writes an unsupported output format error.
    /// </summary>
    /// <param name="commandPath">The command path, or an empty string for the application root.</param>
    /// <param name="format">The supplied format value.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int UnsupportedFormat(string commandPath, string format, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnsupportedFormat,
                Message = "Unsupported output format '" + format + "'.",
                Hint = "Use 'text', 'plain', or 'json'.",
                ExitCode = RunesHelaExitCode.Usage,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.UnsupportedFormat),
                NextSteps = new[] { HelpStep(commandPath) },
            },
            json);
    }

    /// <summary>
    /// Writes an unknown completion shell error.
    /// </summary>
    /// <param name="shell">The supplied shell name.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int UnknownShell(string shell, bool json)
    {
        var suggestion = DidYouMean.Nearest(shell, new[] { "bash", "zsh", "fish", "pwsh" });

        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnknownShell,
                Message = "Unknown completion shell '" + shell + "'.",
                Hint = suggestion is null
                    ? "Use 'bash', 'zsh', 'fish', or 'pwsh'."
                    : "Did you mean '" + suggestion + "'?",
                ExitCode = RunesHelaExitCode.Usage,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.UnknownShell),
                NextSteps = new[] { "Run 'completion <bash|zsh|fish|pwsh>'." },
            },
            json);
    }

    /// <summary>
    /// Writes an error for an unhandled exception.
    /// </summary>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="verbose">Whether to include the full exception text.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int Unexpected(Exception exception, bool verbose, bool json)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnexpectedError,
                Message = "An unexpected error occurred.",
                Hint = verbose ? exception.ToString() : exception.Message,
                ExitCode = RunesHelaExitCode.Failure,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.UnexpectedError),
                NextSteps = new[] { "Re-run with '--verbose' for details." },
            },
            json);
    }

    private static int Write(RunesHelaError error, bool json)
    {
        return ErrorOutput.Write(error, json);
    }

    private static string DocsUrl(string code)
    {
        return code switch
        {
            RunesHelaErrorCodes.UnknownCommand => UnknownCommandDocs,
            RunesHelaErrorCodes.UnknownOption => UnknownOptionDocs,
            RunesHelaErrorCodes.MissingValue => MissingValueDocs,
            RunesHelaErrorCodes.InvalidValue => InvalidValueDocs,
            RunesHelaErrorCodes.MissingRequired => MissingRequiredDocs,
            RunesHelaErrorCodes.UnexpectedArgument => UnexpectedArgumentDocs,
            RunesHelaErrorCodes.ValidationFailed => ValidationFailedDocs,
            RunesHelaErrorCodes.ConfigNotFound => ConfigNotFoundDocs,
            RunesHelaErrorCodes.ConfigInvalid => ConfigInvalidDocs,
            RunesHelaErrorCodes.ConfigInvalidValue => ConfigInvalidValueDocs,
            RunesHelaErrorCodes.ServicesMissing => ServicesMissingDocs,
            RunesHelaErrorCodes.ServiceMissing => ServiceMissingDocs,
            RunesHelaErrorCodes.NotImplemented => NotImplementedDocs,
            RunesHelaErrorCodes.ConfirmationRequired => ConfirmationRequiredDocs,
            RunesHelaErrorCodes.Aborted => AbortedDocs,
            RunesHelaErrorCodes.UnsupportedFormat => UnsupportedFormatDocs,
            RunesHelaErrorCodes.UnknownShell => UnknownShellDocs,
            RunesHelaErrorCodes.UnexpectedError => UnexpectedErrorDocs,
            _ => DocsBase,
        };
    }

    private static string HelpStep(string commandPath)
    {
        return commandPath.Length == 0
            ? "Run '--help' to list options."
            : "Run '" + commandPath + " --help' to list options.";
    }

    private static class DidYouMean
    {
        public static string? Nearest(string input, string[]? candidates)
        {
            if (candidates is null || candidates.Length == 0 || input.Length == 0)
            {
                return null;
            }

            string? best = null;
            var bestDistance = int.MaxValue;

            foreach (var candidate in candidates)
            {
                var distance = Distance(input, candidate, 3);

                if (candidate.StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
                    input.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    distance = 0;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return bestDistance <= 2 ? best : null;
        }

        private static int Distance(string left, string right, int limit)
        {
            if (Math.Abs(left.Length - right.Length) > limit || right.Length > 64)
            {
                return limit + 1;
            }

            Span<int> previous = stackalloc int[right.Length + 1];
            Span<int> current = stackalloc int[right.Length + 1];

            for (var j = 0; j <= right.Length; j++)
            {
                previous[j] = j;
            }

            for (var i = 1; i <= left.Length; i++)
            {
                current[0] = i;

                for (var j = 1; j <= right.Length; j++)
                {
                    var cost = char.ToLowerInvariant(left[i - 1]) == char.ToLowerInvariant(right[j - 1]) ? 0 : 1;
                    var deletion = previous[j] + 1;
                    var insertion = current[j - 1] + 1;
                    var substitution = previous[j - 1] + cost;
                    current[j] = Math.Min(Math.Min(deletion, insertion), substitution);
                }

                var swap = previous;
                previous = current;
                current = swap;
            }

            return previous[right.Length];
        }
    }
}
