namespace RunesHela;

/// <summary>
/// Guards destructive commands so non-interactive callers never hang.
/// </summary>
public static class RunesHelaGuard
{
    /// <summary>
    /// Ensures a destructive command may run.
    /// </summary>
    /// <param name="commandPath">The command path used in messages.</param>
    /// <param name="assumeYes">Whether <c>--yes</c> was supplied.</param>
    /// <param name="noInput">Whether <c>--no-input</c> was supplied.</param>
    /// <param name="json">Whether errors should be written as JSON.</param>
    /// <returns>Zero when the command may run; otherwise the exit code.</returns>
    public static int EnsureConfirmed(string commandPath, bool assumeYes, bool noInput, bool json)
    {
        if (assumeYes)
        {
            return RunesHelaExitCode.Success;
        }

        var console = RunesHelaConsole.Current;

        if (noInput || console is not IInteractiveConsole interactive || interactive.IsInputRedirected)
        {
            return RunesHelaErrors.ConfirmationRequired(commandPath, json);
        }

        interactive.Error.Write("Command '");
        interactive.Error.Write(commandPath);
        interactive.Error.Write("' is destructive. Continue? [y/N] ");

        string? answer;

        try
        {
            answer = interactive.In.ReadLine();
        }
        catch (IOException)
        {
            return RunesHelaErrors.ConfirmationRequired(commandPath, json);
        }

        if (answer is not null &&
            (answer.Equals("y", StringComparison.OrdinalIgnoreCase) ||
             answer.Equals("yes", StringComparison.OrdinalIgnoreCase)))
        {
            return RunesHelaExitCode.Success;
        }

        return RunesHelaErrors.Aborted(commandPath, json);
    }
}
