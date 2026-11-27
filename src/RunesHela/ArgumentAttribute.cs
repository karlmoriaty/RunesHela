namespace RunesHela;

/// <summary>
/// Marks a settable property as a positional argument.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ArgumentAttribute : Attribute
{
    /// <summary>
    /// Creates a positional argument at the specified zero-based position.
    /// </summary>
    /// <param name="position">The zero-based argument position.</param>
    public ArgumentAttribute(int position)
    {
        Position = position;
    }

    /// <summary>
    /// Gets the zero-based argument position.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Gets or sets the name shown in help output, or <see langword="null"/> to derive it from the property name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the one-line description shown in help output.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the argument must be supplied.
    /// </summary>
    public bool Required { get; set; }
}
