namespace RunesHela;

/// <summary>
/// Marks a class as a command root for the RunesHela generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// Creates a command whose name is derived from the type name.
    /// </summary>
    public CommandAttribute()
    {
    }

    /// <summary>
    /// Creates a command with an explicit name.
    /// </summary>
    /// <param name="name">The command name shown on the command line.</param>
    public CommandAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the explicit command name, or <see langword="null"/> to derive it from the type name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets or sets the one-line description shown in help output.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command is hidden from help output.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command performs a destructive operation.
    /// </summary>
    public bool IsDestructive { get; set; }
}
