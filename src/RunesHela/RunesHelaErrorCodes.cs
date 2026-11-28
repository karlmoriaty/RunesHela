namespace RunesHela;

/// <summary>
/// Stable error codes emitted by generated command binders.
/// </summary>
public static class RunesHelaErrorCodes
{
    /// <summary>The command name is not registered.</summary>
    public const string UnknownCommand = "RHL_USAGE_UNKNOWN_COMMAND";

    /// <summary>The option name is not declared on the command.</summary>
    public const string UnknownOption = "RHL_USAGE_UNKNOWN_OPTION";

    /// <summary>An option was supplied without its value.</summary>
    public const string MissingValue = "RHL_USAGE_MISSING_VALUE";

    /// <summary>An option value could not be converted to the declared type.</summary>
    public const string InvalidValue = "RHL_USAGE_INVALID_VALUE";

    /// <summary>A required option or argument was not supplied.</summary>
    public const string MissingRequired = "RHL_USAGE_MISSING_REQUIRED";

    /// <summary>More positional arguments were supplied than the command accepts.</summary>
    public const string UnexpectedArgument = "RHL_USAGE_UNEXPECTED_ARGUMENT";

    /// <summary>A value failed a declared validation attribute.</summary>
    public const string ValidationFailed = "RHL_USAGE_VALIDATION_FAILED";

    /// <summary>A destructive command was run without confirmation.</summary>
    public const string ConfirmationRequired = "RHL_USAGE_CONFIRMATION_REQUIRED";

    /// <summary>The user declined an interactive confirmation.</summary>
    public const string Aborted = "RHL_USAGE_ABORTED";

    /// <summary>An output format value is not supported.</summary>
    public const string UnsupportedFormat = "RHL_USAGE_UNSUPPORTED_FORMAT";

    /// <summary>A requested completion shell is not supported.</summary>
    public const string UnknownShell = "RHL_USAGE_UNKNOWN_SHELL";

    /// <summary>The command needs services but no service provider was supplied.</summary>
    public const string ServicesMissing = "RHL_DI_SERVICES_MISSING";

    /// <summary>A constructor dependency could not be resolved.</summary>
    public const string ServiceMissing = "RHL_DI_SERVICE_MISSING";

    /// <summary>The command declares no handler method.</summary>
    public const string NotImplemented = "RHL_INTERNAL_NOT_IMPLEMENTED";

    /// <summary>An unhandled exception escaped the command.</summary>
    public const string UnexpectedError = "RHL_INTERNAL_UNEXPECTED";
}
