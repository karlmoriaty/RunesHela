namespace RunesHela;

/// <summary>
/// Loads JSON configuration values used by generated command binders.
/// Parsing uses the built-in <see cref="JsonParser"/> and never reflection, so it stays Native AOT compatible.
/// </summary>
public sealed class RunesHelaConfiguration
{
    private const string ConfigEnvVar = "RHL_CONFIG";
    private const string WarningEnvVar = "RHL_NO_CONFIG_WARNINGS";

    private static readonly string[] SensitiveMarkers = { "token", "secret", "password", "key" };

    private static readonly RunesHelaConfiguration Empty = new(null);

    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string[]> _arrays = new(StringComparer.OrdinalIgnoreCase);

    private RunesHelaConfiguration(string? explicitPath)
    {
        ExplicitPath = explicitPath;
    }

    /// <summary>
    /// Gets the path supplied through <c>--config</c> or <c>RHL_CONFIG</c>, or <see langword="null"/>.
    /// </summary>
    public string? ExplicitPath { get; }

    /// <summary>
    /// Gets the configuration file that was read, or <see langword="null"/> when none was found.
    /// </summary>
    public string? Path { get; private set; }

    /// <summary>
    /// Gets the exit code of the load operation: <c>0</c> on success, otherwise <c>3</c>.
    /// The error envelope has already been written when this is non-zero.
    /// </summary>
    public int ExitCode { get; private set; }

    /// <summary>
    /// Loads the configuration file using explicit and conventional discovery.
    /// </summary>
    /// <param name="path">The path from <c>--config</c>, or <see langword="null"/> to discover a file.</param>
    /// <param name="appName">The application name used for the user configuration directory.</param>
    /// <param name="json">Whether errors are written as JSON.</param>
    /// <param name="fileName">
    /// The implicit file name enabled by <c>[assembly: RunesHelaConfigFile]</c>,
    /// or <see langword="null"/> to require an explicit path.
    /// </param>
    /// <param name="command">The command path used in error envelopes.</param>
    /// <returns>The loaded configuration; check <see cref="ExitCode"/> before binding values.</returns>
    public static RunesHelaConfiguration Load(
        string? path,
        string appName,
        bool json,
        string? fileName = null,
        string? command = null)
    {
        if (path is null && fileName is null)
        {
            return Empty;
        }

        var configuration = new RunesHelaConfiguration(Normalize(path));
        configuration.Read(appName, fileName, json, command);
        return configuration;
    }

    /// <summary>
    /// Tries to read a scalar value by dot path.
    /// </summary>
    /// <param name="key">The configuration key, such as <c>deploy.environment</c>.</param>
    /// <param name="value">The string representation of the value.</param>
    /// <returns><see langword="true"/> when the key exists and is not an array.</returns>
    public bool TryGetValue(string key, out string value)
    {
        return _values.TryGetValue(key, out value!);
    }

    /// <summary>
    /// Tries to read an array value by dot path.
    /// </summary>
    /// <param name="key">The configuration key, such as <c>greet.tags</c>.</param>
    /// <param name="values">The array items.</param>
    /// <returns><see langword="true"/> when the key exists and is an array.</returns>
    public bool TryGetValues(string key, out string[] values)
    {
        return _arrays.TryGetValue(key, out values!);
    }

    private static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path.Trim();
    }

    private void Read(string appName, string? fileName, bool json, string? command)
    {
        string? file;

        if (ExplicitPath is not null)
        {
            if (!File.Exists(ExplicitPath))
            {
                ExitCode = RunesHelaErrors.ConfigNotFound(ExplicitPath, json, command);
                return;
            }

            file = ExplicitPath;
        }
        else
        {
            var environmentPath = Normalize(Environment.GetEnvironmentVariable(ConfigEnvVar));

            if (environmentPath is not null)
            {
                if (!File.Exists(environmentPath))
                {
                    ExitCode = RunesHelaErrors.ConfigNotFound(environmentPath, json, command);
                    return;
                }

                file = environmentPath;
            }
            else
            {
                file = fileName is null ? null : Discover(appName, fileName);

                if (file is null)
                {
                    return;
                }
            }
        }

        try
        {
            using var stream = File.OpenRead(file);
            var root = JsonParser.Parse(stream);

            if (root.Kind != JsonKind.Object)
            {
                ExitCode = RunesHelaErrors.ConfigInvalid(file, "the root value must be a JSON object.", json, command);
                return;
            }

            if (!Flatten(root, string.Empty, file, json, command))
            {
                return;
            }

            Path = file;
            WarnIfInsecure(file);
        }
        catch (JsonParseException exception)
        {
            ExitCode = RunesHelaErrors.ConfigInvalid(
                file,
                "the JSON could not be parsed at line " + exception.Line + ", column " + exception.Column + ".",
                json,
                command);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            ExitCode = RunesHelaErrors.ConfigInvalid(file, "the file could not be read: " + exception.Message, json, command);
        }
    }

    private static string? Discover(string appName, string fileName)
    {
        var current = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fileName);

        if (File.Exists(current))
        {
            return current;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (home.Length == 0)
        {
            return null;
        }

        var user = System.IO.Path.Combine(home, ".config", appName, fileName);
        return File.Exists(user) ? user : null;
    }

    private bool Flatten(JsonValue element, string prefix, string file, bool json, string? command)
    {
        foreach (var property in element.Properties!)
        {
            var key = prefix.Length == 0 ? property.Key : prefix + "." + property.Key;
            var value = property.Value;

            switch (value.Kind)
            {
                case JsonKind.Object:
                    if (!Flatten(value, key, file, json, command))
                    {
                        return false;
                    }

                    break;

                case JsonKind.Array:
                    if (!AddArray(value, key, file, json, command))
                    {
                        return false;
                    }

                    break;

                case JsonKind.String:
                case JsonKind.Number:
                case JsonKind.Boolean:
                    _values[key] = value.Text;
                    break;

                case JsonKind.Null:
                    break;
            }
        }

        return true;
    }

    private bool AddArray(JsonValue element, string key, string file, bool json, string? command)
    {
        var items = new List<string>();

        foreach (var item in element.Items!)
        {
            switch (item.Kind)
            {
                case JsonKind.String:
                case JsonKind.Number:
                case JsonKind.Boolean:
                    items.Add(item.Text);
                    break;

                default:
                    ExitCode = RunesHelaErrors.ConfigInvalid(
                        file,
                        "key '" + key + "' must contain scalar values.",
                        json,
                        command);
                    return false;
            }
        }

        _arrays[key] = items.ToArray();
        return true;
    }

    private void WarnIfInsecure(string file)
    {
        if (OperatingSystem.IsWindows() ||
            Environment.GetEnvironmentVariable(WarningEnvVar) == "1" ||
            !HasSensitiveKey())
        {
            return;
        }

        try
        {
            var mode = File.GetUnixFileMode(file);

            if ((mode & (UnixFileMode.GroupRead | UnixFileMode.OtherRead)) == 0)
            {
                return;
            }

            RunesHelaConsole.Current.Error.WriteLine(
                "warning: configuration file '" + file + "' is readable by other users and contains sensitive keys.");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private bool HasSensitiveKey()
    {
        foreach (var key in _values.Keys)
        {
            if (IsSensitiveKey(key))
            {
                return true;
            }
        }

        foreach (var key in _arrays.Keys)
        {
            if (IsSensitiveKey(key))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSensitiveKey(string key)
    {
        var start = 0;

        while (start <= key.Length)
        {
            var dot = key.IndexOf('.', start);
            var end = dot < 0 ? key.Length : dot;
            var segment = key.AsSpan(start, end - start);

            foreach (var marker in SensitiveMarkers)
            {
                if (segment.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            if (dot < 0)
            {
                return false;
            }

            start = dot + 1;
        }

        return false;
    }
}
