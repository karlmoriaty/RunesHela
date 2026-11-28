using System.Text;

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
        var manifest = JsonParser.Parse(manifestJson);
        var tools = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
        var toolsElement = JsonValue.Array(new List<JsonValue>());

        if (manifest.TryGetProperty("tools", out var manifestTools) && manifestTools.Kind == JsonKind.Array)
        {
            toolsElement = manifestTools;

            foreach (var tool in manifestTools.Items!)
            {
                if (tool.TryGetProperty("name", out var name) && name.Kind == JsonKind.String)
                {
                    tools[name.Text] = tool;
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

            var response = HandleMessage(line, tools, toolsElement, appName, version, invoke);

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
        Dictionary<string, JsonValue> tools,
        JsonValue toolsElement,
        string appName,
        string version,
        Func<string[], int> invoke)
    {
        JsonValue root;

        try
        {
            root = JsonParser.Parse(line);
        }
        catch (JsonParseException)
        {
            return Error(null, -32700, "Parse error");
        }

        if (root.Kind != JsonKind.Object)
        {
            return Error(null, -32600, "Invalid Request");
        }

        JsonValue? id = null;

        if (root.TryGetProperty("id", out var idElement) && idElement.Kind != JsonKind.Null)
        {
            id = idElement;
        }

        if (!root.TryGetProperty("method", out var methodElement) || methodElement.Kind != JsonKind.String)
        {
            return id is null ? string.Empty : Error(id, -32600, "Invalid Request");
        }

        var method = methodElement.Text;

        switch (method)
        {
            case "initialize":
            {
                var protocol = DefaultProtocolVersion;

                if (root.TryGetProperty("params", out var parameters) &&
                    parameters.Kind == JsonKind.Object &&
                    parameters.TryGetProperty("protocolVersion", out var requested) &&
                    requested.Kind == JsonKind.String)
                {
                    protocol = requested.Text;
                }

                var result = "{\"protocolVersion\":" + Quote(protocol)
                    + ",\"capabilities\":{\"tools\":{\"listChanged\":false}}"
                    + ",\"serverInfo\":{\"name\":" + Quote(appName)
                    + ",\"version\":" + Quote(version) + "}}";

                return id is null ? string.Empty : Result(id, result);
            }

            case "notifications/initialized":
                return string.Empty;

            case "ping":
                return id is null ? string.Empty : Result(id, "{}");

            case "tools/list":
                return id is null
                    ? string.Empty
                    : Result(id, "{\"tools\":" + JsonWriter.ToJson(toolsElement) + "}");

            case "tools/call":
            {
                var response = CallTool(id, root, tools, invoke);
                return id is null ? string.Empty : response;
            }

            default:
                return id is null ? string.Empty : Error(id, -32601, "Method not found: " + method);
        }
    }

    private static string CallTool(
        JsonValue? id,
        JsonValue root,
        Dictionary<string, JsonValue> tools,
        Func<string[], int> invoke)
    {
        if (!root.TryGetProperty("params", out var parameters) || parameters.Kind != JsonKind.Object)
        {
            return Error(id, -32602, "Missing tool parameters.");
        }

        if (!parameters.TryGetProperty("name", out var nameElement) || nameElement.Kind != JsonKind.String)
        {
            return Error(id, -32602, "Missing tool name.");
        }

        var name = nameElement.Text;

        if (!tools.TryGetValue(name, out var tool))
        {
            return Error(id, -32602, "Unknown tool '" + name + "'.");
        }

        var arguments = JsonValue.Object(new Dictionary<string, JsonValue>(StringComparer.Ordinal));

        if (parameters.TryGetProperty("arguments", out var argumentsElement) && argumentsElement.Kind == JsonKind.Object)
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
        JsonValue tool,
        JsonValue arguments,
        out string[] args,
        out string? error)
    {
        error = null;
        var command = new List<string>();

        if (tool.TryGetProperty("x-runeshela-path", out var pathElement) && pathElement.Kind == JsonKind.String)
        {
            foreach (var part in pathElement.Text.Split(' '))
            {
                command.Add(part);
            }
        }

        if (!tool.TryGetProperty("inputSchema", out var schema) ||
            schema.Kind != JsonKind.Object ||
            !schema.TryGetProperty("properties", out var properties) ||
            properties.Kind != JsonKind.Object)
        {
            args = command.ToArray();
            return true;
        }

        var positionals = new List<Positional>();
        var options = new List<string>();

        foreach (var argument in arguments.Properties!)
        {
            if (!properties.TryGetProperty(argument.Key, out var propertySchema))
            {
                error = "Unknown argument '" + argument.Key + "'.";
                args = Array.Empty<string>();
                return false;
            }

            if (propertySchema.TryGetProperty("x-runeshela-position", out var positionElement) &&
                positionElement.TryGetInt32(out var position))
            {
                if (!TryAddPositionals(argument.Key, argument.Value, position, positionals, out error))
                {
                    args = Array.Empty<string>();
                    return false;
                }

                continue;
            }

            if (propertySchema.TryGetProperty("x-runeshela-option", out var optionElement) &&
                optionElement.Kind == JsonKind.String)
            {
                if (!TryAddOption(argument.Key, optionElement.Text, argument.Value, propertySchema, options, out error))
                {
                    args = Array.Empty<string>();
                    return false;
                }

                continue;
            }

            if (propertySchema.TryGetProperty("x-runeshela-global", out var globalElement) &&
                globalElement.Kind == JsonKind.String &&
                globalElement.Text == "yes")
            {
                if (argument.Value.Kind == JsonKind.Boolean && argument.Value.Text == "true")
                {
                    options.Add("--yes");
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
        JsonValue value,
        int position,
        List<Positional> positionals,
        out string? error)
    {
        error = null;

        if (value.Kind == JsonKind.Array)
        {
            foreach (var item in value.Items!)
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

        if (value.Kind == JsonKind.Null)
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
        JsonValue value,
        JsonValue propertySchema,
        List<string> options,
        out string? error)
    {
        error = null;

        if (value.Kind == JsonKind.Null)
        {
            return true;
        }

        if (propertySchema.TryGetProperty("type", out var typeElement) &&
            typeElement.Kind == JsonKind.String &&
            typeElement.Text == "boolean" &&
            value.Kind == JsonKind.Boolean)
        {
            if (value.Text == "true")
            {
                options.Add("--" + optionName);
            }
            else if (propertySchema.TryGetProperty("x-runeshela-negatable", out var negatable) &&
                     negatable.Kind == JsonKind.Boolean &&
                     negatable.Text == "true")
            {
                options.Add("--no-" + optionName);
            }

            return true;
        }

        if (value.Kind == JsonKind.Array)
        {
            foreach (var item in value.Items!)
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

    private static bool TryGetScalar(JsonValue value, out string text)
    {
        switch (value.Kind)
        {
            case JsonKind.String:
            case JsonKind.Number:
            case JsonKind.Boolean:
                text = value.Text;
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
        JsonWriter.WriteString(builder, text);
        builder.Append('}');
        first = false;
    }

    private static string Result(JsonValue? id, string result)
    {
        return "{\"jsonrpc\":\"2.0\",\"id\":" + Id(id) + ",\"result\":" + result + "}";
    }

    private static string Error(JsonValue? id, int code, string message)
    {
        return "{\"jsonrpc\":\"2.0\",\"id\":" + Id(id)
            + ",\"error\":{\"code\":" + code + ",\"message\":" + Quote(message) + "}}";
    }

    private static string Id(JsonValue? id)
    {
        return id is null ? "null" : JsonWriter.ToJson(id);
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        JsonWriter.WriteString(builder, value);
        return builder.ToString();
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
