namespace RunesHela;

/// <summary>
/// Marks a bound option or argument as required even when its type has a default value.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RequiredAttribute : Attribute
{
}

/// <summary>
/// Requires a numeric value to fall inside an inclusive range.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class RangeAttribute : Attribute
{
    /// <summary>
    /// Creates a range check for integral values.
    /// </summary>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="max">The inclusive maximum.</param>
    public RangeAttribute(long min, long max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Creates a range check for floating-point values.
    /// </summary>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="max">The inclusive maximum.</param>
    public RangeAttribute(double min, double max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>Gets the inclusive minimum.</summary>
    public double Min { get; }

    /// <summary>Gets the inclusive maximum.</summary>
    public double Max { get; }
}

/// <summary>
/// Requires a string value to match a regular expression.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class RegexAttribute : Attribute
{
    /// <summary>
    /// Creates a regular expression check.
    /// </summary>
    /// <param name="pattern">The pattern the value must match.</param>
    public RegexAttribute(string pattern)
    {
        Pattern = pattern;
    }

    /// <summary>Gets the required pattern.</summary>
    public string Pattern { get; }
}

/// <summary>
/// Requires a string value length to fall inside an inclusive range.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class LengthAttribute : Attribute
{
    /// <summary>
    /// Creates a maximum length check.
    /// </summary>
    /// <param name="max">The inclusive maximum length.</param>
    public LengthAttribute(int max)
        : this(0, max)
    {
    }

    /// <summary>
    /// Creates a length check for an inclusive range.
    /// </summary>
    /// <param name="min">The inclusive minimum length.</param>
    /// <param name="max">The inclusive maximum length.</param>
    public LengthAttribute(int min, int max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>Gets the inclusive minimum length.</summary>
    public int Min { get; }

    /// <summary>Gets the inclusive maximum length.</summary>
    public int Max { get; }
}
