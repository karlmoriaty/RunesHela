namespace RunesHela;

/// <summary>
/// Writes <see cref="RunesHelaError"/> instances to standard error as plain text or JSON.
/// </summary>
public static class ErrorOutput
{
    private const string Red = "\u001b[31m";
    private const string Reset = "\u001b[0m";

    /// <summary>
    /// Writes the error to the ambient console error stream.
    /// </summary>
    /// <param name="error">The error to write.</param>
    /// <param name="json">Whether to write the JSON envelope instead of plain text.</param>
    /// <returns>The error exit code.</returns>
    public static int Write(RunesHelaError error, bool json = false)
    {
        ArgumentNullException.ThrowIfNull(error);

        var writer = RunesHelaConsole.Current.Error;

        if (json)
        {
            WriteJson(error, writer);
        }
        else
        {
            WriteText(error, writer, RunesHelaConsole.Current.IsAnsiSupported);
        }

        return error.ExitCode;
    }

    private static void WriteText(RunesHelaError error, TextWriter writer, bool useColor)
    {
        if (useColor)
        {
            writer.Write(Red);
            writer.Write("Error:");
            writer.Write(Reset);
            writer.Write(' ');
        }
        else
        {
            writer.Write("Error: ");
        }

        writer.WriteLine(error.Message);

        if (!string.IsNullOrEmpty(error.Hint))
        {
            writer.Write("Hint: ");
            writer.WriteLine(error.Hint);
        }

        foreach (var step in error.NextSteps)
        {
            writer.Write("Next: ");
            writer.WriteLine(step);
        }
    }

    private static void WriteJson(RunesHelaError error, TextWriter writer)
    {
        writer.Write("{\"ok\":false,\"schemaVersion\":\"1.0\",\"error\":{\"code\":");
        WriteJsonString(writer, error.Code);
        writer.Write(",\"message\":");
        WriteJsonString(writer, error.Message);
        writer.Write(",\"hint\":");
        WriteJsonString(writer, error.Hint);
        writer.Write(",\"nextSteps\":[");

        for (var i = 0; i < error.NextSteps.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(',');
            }

            WriteJsonString(writer, error.NextSteps[i]);
        }

        writer.Write("],\"docsUrl\":");
        WriteJsonString(writer, error.DocsUrl);
        writer.Write(",\"command\":");
        WriteJsonString(writer, error.CommandPath);
        writer.Write(",\"exitCode\":");
        writer.Write(error.ExitCode);
        writer.WriteLine("}}");
    }

    private static void WriteJsonString(TextWriter writer, string? value)
    {
        if (value is null)
        {
            writer.Write("null");
            return;
        }

        writer.Write('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    writer.Write("\\\"");
                    break;
                case '\\':
                    writer.Write("\\\\");
                    break;
                case '\b':
                    writer.Write("\\b");
                    break;
                case '\f':
                    writer.Write("\\f");
                    break;
                case '\n':
                    writer.Write("\\n");
                    break;
                case '\r':
                    writer.Write("\\r");
                    break;
                case '\t':
                    writer.Write("\\t");
                    break;
                default:
                    if (c < ' ')
                    {
                        writer.Write("\\u");
                        writer.Write(((int)c).ToString("x4"));
                    }
                    else
                    {
                        writer.Write(c);
                    }

                    break;
            }
        }

        writer.Write('"');
    }
}
