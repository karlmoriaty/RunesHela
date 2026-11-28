namespace RunesHela;

/// <summary>
/// Provides environment-variable binding helpers used by generated command binders.
/// </summary>
public static class RunesHelaEnvironment
{
    /// <summary>
    /// Tries to parse a boolean environment value.
    /// Accepts <c>1/0</c>, <c>true/false</c>, <c>yes/no</c>, and <c>on/off</c>.
    /// </summary>
    /// <param name="value">The environment value.</param>
    /// <param name="result">The parsed value.</param>
    /// <returns><see langword="true"/> when the value was recognized.</returns>
    public static bool TryParseBoolean(string? value, out bool result)
    {
        result = false;

        if (value is null)
        {
            return false;
        }

        var text = value.AsSpan().Trim();

        if (text.Equals("1", StringComparison.Ordinal) ||
            text.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }

        if (text.Equals("0", StringComparison.Ordinal) ||
            text.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }

        return false;
    }
}
