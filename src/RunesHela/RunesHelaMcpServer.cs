using System.Text;
using System.Text.Json;

namespace RunesHela;

/// <summary>
/// Serves the generated command model as MCP tools over a JSON-RPC stdio transport.
/// </summary>
public static class RunesHelaMcpServer
{
    private const string DefaultProtocolVersion = "2024-11-05";

    /// <summary>
    /// Runs the stdio server until the input stream ends.
    /// </summary>
    /// <param name="manifestJson">The generated MCP manifest.</param>
    /// <param name="appName">The application name reported during initialization.</param>
    /// <param name="version">The application version reported during initialization.</param>
    /// <param name="input">The JSON-RPC input stream, one message per line.</param>
    /// <param name="output">The JSON-RPC output stream.</param>
    /// <param name="invoke">Executes command-line arguments and returns the exit code.</param>
    /// <returns><c>0</c> when the stream ended normally.</returns>
    public static int Run(
        string manifestJson,
        string appName,
        string version,
        TextReader input,
        TextWriter output,
        Func<string[], int> invoke)
    {
        using var document = JsonDocument.Parse(manifestJson);
        var root = document.RootElement;
        var toolsJson = "[]";
        var tools = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (root.TryGetProperty("tools", out var toolsElement) && toolsElement.ValueKind == JsonValueKind.Array)
        {
            toolsJson = toolsElement.GetRawText();

            foreach (var tool in toolsElement.EnumerateArray())
            {
                if (tool.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                {
                    tools[name.GetString()!] = tool;
                }
            }
        }

        while (true)
        {
            var line = input.ReadLine();

            if (line is null)
            {
                return 0;
            }

            if (line.Length == 0)
            {
                continue;
            }

            var response = HandleMessage(line, tools, toolsJson, appName, version, invoke);

            if (response.Length == 0)
            {
                continue;
            }

            output.WriteLine(response);
            output.Flush();
        }
    }

    private static string HandleMessage(
        string line,
        Dictionary<string, JsonElement> tools,
        string toolsJson,
        string appName,
        string version,
        Func<string[], int> invoke)
    {
        JsonDocument request;

        try
        {
            request = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return Error(null, -32700, "Parse error");
        }

        using (request)
        {
            var root = request.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return Error(null, -32600, "Invalid Request");
            }

            var hasId = root.TryGetProperty("id", out var idElement) && idElement.ValueKind != JsonValueKind.Null;
            var id = hasId ? idElement.GetRawText() : null;

            if (!root.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
            {
                return hasId ? Error(id, -32600, "Invalid Request") : string.Empty;
            }

            var method = methodElement.GetString()!;

            switch (method)
            {
                case "initialize":
                {
                    var protocol = DefaultProtocolVersion;

                    if (root.TryGetProperty("params", out var parameters) &&
                        parameters.ValueKind == JsonValueKind.Object &&
                        parameters.TryGetProperty("protocolVersion", out var requested) &&
                        requested.ValueKind == JsonValueKind.String)
                    {
                        protocol = requested.GetString()!;
                    }

                    var result = "{\"protocolVersion\":" + Quote(protocol)
                        + ",\"capabilities\":{\"tools\":{\"listChanged\":false}}"
                        + ",\"serverInfo\":{\"name\":" + Quote(appName)
                        + ",\"version\":" + Quote(version) + "}}";

                    return hasId ? Result(id, result) : string.Empty;
                }

                case "notifications/initialized":
                    return string.Empty;

                case "ping":
                    return hasId ? Result(id, "{}") : string.Empty;

                case "tools/list":
                    return hasId ? Result(id, "{\"tools\":" + toolsJson + "}") : string.Empty;

                case "tools/call":
                {
                    var response = CallTool(id, root, tools, invoke);
                    return hasId ? response : string.Empty;
                }

                default:
                    return hasId ? Error(id, -32601, "Method not found: " + method) : string.Empty;
            }
        }
    }

    private static string CallTool(
        string? id,
        JsonElement root,
        Dictionary<string, JsonElement> tools,
        Func<string[], int> invoke)
    {
        if (!root.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object)
        {
            return Error(id, -32602, "Missing tool parameters.");
        }

        if (!parameters.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            return Error(id, -32602, "Missing tool name.");
        }

        var name = nameElement.GetString()!;

        if (!tools.TryGetValue(name, out var tool))
        {
            return Error(id, -32602, "Unknown tool '" + name + "'.");
        }

        var arguments = default(JsonElement);

        if (parameters.TryGetProperty("arguments", out var argumentsElement) &&
            argumentsElement.ValueKind == JsonValueKind.Object)
        {
            arguments = argumentsElement;
        }

        if (!TryBuildArguments(tool, arguments, out var args, out var error))
        {
            return Error(id, -32602, error!);
        }

        var exitCode = Execute(invoke, args, out var stdout, out var stderr);
        return Result(id, BuildToolResult(stdout, stderr, exitCode));
    }

    private static bool TryBuildArguments(
        JsonElement tool,
        JsonElement arguments,
        out string[] args,
        out string? error)
    {
        error = null;
        var command = new List<string>();

        if (tool.TryGetProperty("x-runeshela-path", out var pathElement) &&
            pathElement.ValueKind == JsonValueKind.String)
        {
            foreach (var part in pathElement.GetString()!.Split(' '))
            {
                command.Add(part);
            }
        }

        if (!tool.TryGetProperty("inputSchema", out var schema) ||
            !schema.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
        {
            args = command.ToArray();
            return true;
        }

        var positionals = new List<Positional>();
        var options = new List<string>();

        if (arguments.ValueKind == JsonValueKind.Object)
        {
            foreach (var argument in arguments.EnumerateObject())
            {
                if (!properties.TryGetProperty(argument.Name, out var propertySchema))
                {
                    error = "Unknown argument '" + argument.Name + "'.";
                    args = Array.Empty<string>();
                    return false;
                }

                if (propertySchema.TryGetProperty("x-runeshela-position", out var positionElement) &&
                    positionElement.TryGetInt32(out var position))
                {
                    if (!TryAddPositionals(argument.Name, argument.Value, position, positionals, out error))
                    {
                        args = Array.Empty<string>();
                        return false;
                    }

                    continue;
                }

                if (propertySchema.TryGetProperty("x-runeshela-option", out var optionElement) &&
                    optionElement.ValueKind == JsonValueKind.String)
                {
                    if (!TryAddOption(argument.Name, optionElement.GetString()!, argument.Value, propertySchema, options, out error))
                    {
                        args = Array.Empty<string>();
                        return false;
                    }

                    continue;
                }

                if (propertySchema.TryGetProperty("x-runeshela-global", out var globalElement) &&
                    globalElement.ValueKind == JsonValueKind.String &&
                    globalElement.GetString() == "yes")
                {
                    if (argument.Value.ValueKind == JsonValueKind.True)
                    {
                        options.Add("--yes");
                    }

                    continue;
                }
            }
        }

        positionals.Sort(static (left, right) => left.Position.CompareTo(right.Position));

        foreach (var positional in positionals)
        {
            command.Add(positional.Value);
        }

        command.AddRange(options);
        args = command.ToArray();
        return true;
    }

    private static bool TryAddPositionals(
        string name,
        JsonElement value,
        int position,
        List<Positional> positionals,
        out string? error)
    {
        error = null;

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (!TryGetScalar(item, out var text))
                {
                    error = "Argument '" + name + "' must contain scalar values.";
                    return false;
                }

                positionals.Add(new Positional(position, text));
            }

            return true;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (!TryGetScalar(value, out var scalar))
        {
            error = "Argument '" + name + "' must be a scalar value.";
            return false;
        }

        positionals.Add(new Positional(position, scalar));
        return true;
    }

    private static bool TryAddOption(
        string name,
        string optionName,
        JsonElement value,
        JsonElement propertySchema,
        List<string> options,
        out string? error)
    {
        error = null;

        if (value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (propertySchema.TryGetProperty("type", out var typeElement) &&
            typeElement.ValueKind == JsonValueKind.String &&
            typeElement.GetString() == "boolean" &&
            value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            if (value.ValueKind == JsonValueKind.True)
            {
                options.Add("--" + optionName);
            }
            else if (propertySchema.TryGetProperty("x-runeshela-negatable", out var negatable) &&
                     negatable.ValueKind == JsonValueKind.True)
            {
                options.Add("--no-" + optionName);
            }

            return true;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (!TryGetScalar(item, out var text))
                {
                    error = "Option '" + name + "' must contain scalar values.";
                    return false;
                }

                options.Add("--" + optionName);
                options.Add(text);
            }

            return true;
        }

        if (!TryGetScalar(value, out var scalar))
        {
            error = "Option '" + name + "' must be a scalar value.";
            return false;
        }

        options.Add("--" + optionName);
        options.Add(scalar);
        return true;
    }

    private static bool TryGetScalar(JsonElement value, out string text)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                text = value.GetString() ?? string.Empty;
                return true;

            case JsonValueKind.Number:
                text = value.GetRawText();
                return true;

            case JsonValueKind.True:
                text = "true";
                return true;

            case JsonValueKind.False:
                text = "false";
                return true;

            default:
                text = string.Empty;
                return false;
        }
    }

    private static int Execute(Func<string[], int> invoke, string[] args, out string stdout, out string stderr)
    {
        var previousConsole = RunesHelaConsole.Current;
        var previousInteractive = RunesHelaConsole.Interactive;
        var previousOut = Console.Out;
        var previousError = Console.Error;
        var capture = new CaptureConsole();

        RunesHelaConsole.Current = capture;
        RunesHelaConsole.Interactive = capture;
        Console.SetOut(capture.Out);
        Console.SetError(capture.Error);

        try
        {
            var exitCode = invoke(args);
            stdout = capture.OutWriter.ToString();
            stderr = capture.ErrorWriter.ToString();
            return exitCode;
        }
        catch (Exception exception)
        {
            stdout = capture.OutWriter.ToString();
            stderr = capture.ErrorWriter.ToString();
            stderr += "RunesHela: " + exception.Message;
            return RunesHelaExitCode.Failure;
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
            RunesHelaConsole.Current = previousConsole;
            RunesHelaConsole.Interactive = previousInteractive;
        }
    }

    private static string BuildToolResult(string stdout, string stderr, int exitCode)
    {
        var builder = new StringBuilder();
        builder.Append("{\"content\":[");

        var first = true;

        if (stdout.Length > 0)
        {
            AppendTextItem(builder, stdout, ref first);
        }

        if (stderr.Length > 0)
        {
            AppendTextItem(builder, stderr, ref first);
        }

        if (first)
        {
            AppendTextItem(builder, string.Empty, ref first);
        }

        builder.Append("],\"isError\":").Append(exitCode == 0 ? "false" : "true");
        builder.Append(",\"structuredContent\":{\"exitCode\":").Append(exitCode).Append("}}");
        return builder.ToString();
    }

    private static void AppendTextItem(StringBuilder builder, string text, ref bool first)
    {
        if (!first)
        {
            builder.Append(',');
        }

        builder.Append("{\"type\":\"text\",\"text\":");
        AppendString(builder, text);
        builder.Append('}');
        first = false;
    }

    private static string Result(string? id, string result)
    {
        return "{\"jsonrpc\":\"2.0\",\"id\":" + (id ?? "null") + ",\"result\":" + result + "}";
    }

    private static string Error(string? id, int code, string message)
    {
        return "{\"jsonrpc\":\"2.0\",\"id\":" + (id ?? "null")
            + ",\"error\":{\"code\":" + code + ",\"message\":" + Quote(message) + "}}";
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        AppendString(builder, value);
        return builder.ToString();
    }

    private static void AppendString(StringBuilder builder, string value)
    {
        builder.Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (c < ' ')
                    {
                        builder.Append("\\u");
                        builder.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        builder.Append('"');
    }

    private readonly struct Positional
    {
        public Positional(int position, string value)
        {
            Position = position;
            Value = value;
        }

        public int Position { get; }

        public string Value { get; }
    }

    private sealed class CaptureConsole : IInteractiveConsole
    {
        public StringWriter OutWriter { get; } = new();

        public StringWriter ErrorWriter { get; } = new();

        public TextWriter Out => OutWriter;

        public TextWriter Error => ErrorWriter;

        public TextReader In => TextReader.Null;

        public bool IsOutputRedirected => true;

        public bool IsInputRedirected => true;

        public bool IsAnsiSupported => false;
    }
}
