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
    private const string ServicesMissingDocs = DocsBase + "rhl_di_services_missing";
    private const string ServiceMissingDocs = DocsBase + "rhl_di_service_missing";
    private const string NotImplementedDocs = DocsBase + "rhl_internal_not_implemented";

    /// <summary>
    /// Writes an unknown command error.
    /// </summary>
    /// <param name="command">The unrecognized command name.</param>
    /// <param name="appName">The application name used in guidance.</param>
    /// <param name="json">Whether to write JSON.</param>
    /// <returns>The exit code.</returns>
    public static int UnknownCommand(string command, string appName, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnknownCommand,
                Message = "Unknown command '" + command + "'.",
                ExitCode = 2,
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
    /// <returns>The exit code.</returns>
    public static int UnknownSubcommand(string commandPath, string command, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnknownCommand,
                Message = "Unknown command '" + commandPath + " " + command + "'.",
                ExitCode = 2,
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
    /// <returns>The exit code.</returns>
    public static int UnknownOption(string commandPath, string optionName, bool json)
    {
        return Write(
            new RunesHelaError
            {
                Code = RunesHelaErrorCodes.UnknownOption,
                Message = "Unknown option '--" + optionName + "'.",
                ExitCode = 2,
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
                ExitCode = 2,
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
                ExitCode = 2,
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
                ExitCode = 2,
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
                ExitCode = 2,
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
                ExitCode = 2,
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
                ExitCode = 2,
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
                ExitCode = 2,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.ValidationFailed),
                NextSteps = new[] { HelpStep(commandPath) },
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
                ExitCode = 1,
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
                ExitCode = 1,
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
                ExitCode = 1,
                CommandPath = commandPath,
                DocsUrl = DocsUrl(RunesHelaErrorCodes.NotImplemented),
                NextSteps = new[] { "Add a public Run, RunAsync, Execute, or ExecuteAsync method." },
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
            RunesHelaErrorCodes.ServicesMissing => ServicesMissingDocs,
            RunesHelaErrorCodes.ServiceMissing => ServiceMissingDocs,
            RunesHelaErrorCodes.NotImplemented => NotImplementedDocs,
            _ => DocsBase,
        };
    }

    private static string HelpStep(string commandPath)
    {
        return commandPath.Length == 0
            ? "Run '--help' to list options."
            : "Run '" + commandPath + " --help' to list options.";
    }
}
