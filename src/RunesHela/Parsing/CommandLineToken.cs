namespace RunesHela.Parsing;

/// <summary>
/// Describes the kind of token produced by <see cref="CommandLineReader"/>.
/// </summary>
public enum CommandLineTokenKind
{
    /// <summary>An option such as <c>--name</c>, <c>--name=value</c>, or <c>-n</c>.</summary>
    Option,

    /// <summary>A positional value.</summary>
    Positional,

    /// <summary>The <c>--</c> terminator. All following tokens are positional.</summary>
    Terminator,
}

/// <summary>
/// A single token from a command line. The token references the original argument strings.
/// </summary>
public readonly ref struct CommandLineToken
{
    private CommandLineToken(
        CommandLineTokenKind kind,
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> value,
        bool hasValue)
    {
        Kind = kind;
        Name = name;
        Value = value;
        HasValue = hasValue;
    }

    /// <summary>Gets the token kind.</summary>
    public CommandLineTokenKind Kind { get; }

    /// <summary>Gets the option name without leading dashes, or an empty span for positional tokens.</summary>
    public ReadOnlySpan<char> Name { get; }

    /// <summary>Gets the inline option value or the positional value.</summary>
    public ReadOnlySpan<char> Value { get; }

    /// <summary>Gets a value indicating whether the option carried an inline value such as <c>--name=value</c>.</summary>
    public bool HasValue { get; }

    /// <summary>Creates an option token.</summary>
    public static CommandLineToken Option(
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> value = default,
        bool hasValue = false)
    {
        return new CommandLineToken(CommandLineTokenKind.Option, name, value, hasValue);
    }

    /// <summary>Creates a positional token.</summary>
    public static CommandLineToken Positional(ReadOnlySpan<char> value)
    {
        return new CommandLineToken(CommandLineTokenKind.Positional, default, value, false);
    }

    /// <summary>Creates the <c>--</c> terminator token.</summary>
    public static CommandLineToken Terminator()
    {
        return new CommandLineToken(CommandLineTokenKind.Terminator, default, default, false);
    }
}
