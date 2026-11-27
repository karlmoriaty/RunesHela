namespace RunesHela;

/// <summary>
/// Marks a settable property as a command-line option.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class OptionAttribute : Attribute
{
    /// <summary>
    /// Creates an option whose name is derived from the property name.
    /// </summary>
    public OptionAttribute()
    {
    }

    /// <summary>
    /// Creates an option with an explicit long name.
    /// </summary>
    /// <param name="name">The long option name without leading dashes.</param>
    public OptionAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the explicit long option name, or <see langword="null"/> to derive it from the property name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets or sets the single-character short name, or <c>'\0'</c> for none.
    /// </summary>
    public char Short { get; set; }

    /// <summary>
    /// Gets or sets the one-line description shown in help output.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets additional long names accepted for the option.
    /// </summary>
    public string[] Aliases { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the environment variable read when the option is not supplied.
    /// </summary>
    public string? Env { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the option must be supplied.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the option is hidden from help output.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets the value placeholder shown in help output, such as <c>PATH</c>.
    /// </summary>
    public string? ValueName { get; set; }
}
