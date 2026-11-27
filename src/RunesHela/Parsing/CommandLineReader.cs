namespace RunesHela.Parsing;

/// <summary>
/// A single-pass command-line tokenizer. It never allocates and never interprets option values.
/// </summary>
public ref struct CommandLineReader
{
    private readonly ReadOnlySpan<string> _args;
    private int _index;
    private bool _afterTerminator;

    /// <summary>
    /// Creates a reader over the supplied arguments.
    /// </summary>
    /// <param name="args">Arguments excluding the executable name.</param>
    public CommandLineReader(ReadOnlySpan<string> args)
    {
        _args = args;
        _index = 0;
        _afterTerminator = false;
    }

    /// <summary>
    /// Reads the next token.
    /// </summary>
    /// <param name="token">The token, valid until the underlying argument array is collected.</param>
    /// <returns><see langword="true"/> when a token was read.</returns>
    public bool TryRead(out CommandLineToken token)
    {
        while (_index < _args.Length)
        {
            var arg = _args[_index++];

            if (_afterTerminator)
            {
                token = CommandLineToken.Positional(arg.AsSpan());
                return true;
            }

            if (arg == "--")
            {
                _afterTerminator = true;
                token = CommandLineToken.Terminator();
                return true;
            }

            if (arg.Length > 1 && arg[0] == '-')
            {
                if (arg[1] == '-')
                {
                    token = ReadLongOption(arg.AsSpan(2));
                    return true;
                }

                // Negative numbers are positional values, not options.
                if (char.IsAsciiDigit(arg[1]) || arg[1] == '.')
                {
                    token = CommandLineToken.Positional(arg.AsSpan());
                    return true;
                }

                token = ReadShortOption(arg.AsSpan(1));
                return true;
            }

            token = CommandLineToken.Positional(arg.AsSpan());
            return true;
        }

        token = default;
        return false;
    }

    /// <summary>
    /// Reads the value for an option token. If the token carried an inline value, that value is returned.
    /// Otherwise the next argument is consumed, unless it is the <c>--</c> terminator.
    /// </summary>
    /// <param name="token">The option token previously returned by <see cref="TryRead"/>.</param>
    /// <param name="value">The option value.</param>
    /// <returns><see langword="true"/> when a value was available.</returns>
    public bool TryReadValue(in CommandLineToken token, out ReadOnlySpan<char> value)
    {
        if (token.HasValue)
        {
            value = token.Value;
            return true;
        }

        if (_index >= _args.Length)
        {
            value = default;
            return false;
        }

        var next = _args[_index];
        if (next == "--")
        {
            value = default;
            return false;
        }

        _index++;
        value = next.AsSpan();
        return true;
    }

    private static CommandLineToken ReadLongOption(ReadOnlySpan<char> body)
    {
        var equals = body.IndexOf('=');
        return equals >= 0
            ? CommandLineToken.Option(body[..equals], body[(equals + 1)..], hasValue: true)
            : CommandLineToken.Option(body);
    }

    private static CommandLineToken ReadShortOption(ReadOnlySpan<char> body)
    {
        var equals = body.IndexOf('=');
        return equals >= 0
            ? CommandLineToken.Option(body[..equals], body[(equals + 1)..], hasValue: true)
            : CommandLineToken.Option(body);
    }
}
