namespace RunesHela.Parsing;

/// <summary>
/// A single-pass command-line tokenizer. It never allocates and never interprets option values.
/// </summary>
public ref struct CommandLineReader
{
    private readonly ReadOnlySpan<string> _args;
    private int _index;
    private bool _afterTerminator;
    private bool _lastWasOption;
    private bool _lastHasValue;
    private int _lastArgIndex;
    private int _lastValueStart;
    private int _lastValueLength;

    /// <summary>
    /// Creates a reader over the supplied arguments.
    /// </summary>
    /// <param name="args">Arguments excluding the executable name.</param>
    public CommandLineReader(ReadOnlySpan<string> args)
    {
        _args = args;
        _index = 0;
        _afterTerminator = false;
        _lastWasOption = false;
        _lastHasValue = false;
        _lastArgIndex = -1;
        _lastValueStart = 0;
        _lastValueLength = 0;
    }

    /// <summary>
    /// Reads the next token.
    /// </summary>
    /// <param name="token">The token, valid until the underlying argument array is collected.</param>
    /// <returns><see langword="true"/> when a token was read.</returns>
    public bool TryRead(out CommandLineToken token)
    {
        _lastWasOption = false;

        while (_index < _args.Length)
        {
            var arg = _args[_index++];
            var argIndex = _index - 1;

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
                    token = ReadOption(arg.AsSpan(2), argIndex, prefixLength: 2);
                    return true;
                }

                // Negative numbers are positional values, not options.
                if (char.IsAsciiDigit(arg[1]) || arg[1] == '.')
                {
                    token = CommandLineToken.Positional(arg.AsSpan());
                    return true;
                }

                token = ReadOption(arg.AsSpan(1), argIndex, prefixLength: 1);
                return true;
            }

            token = CommandLineToken.Positional(arg.AsSpan());
            return true;
        }

        token = default;
        return false;
    }

    /// <summary>
    /// Reads the value for the option returned by the most recent <see cref="TryRead"/> call.
    /// If the option carried an inline value, that value is returned. Otherwise the next argument
    /// is consumed, unless it is the <c>--</c> terminator.
    /// </summary>
    /// <param name="value">The option value.</param>
    /// <returns><see langword="true"/> when a value was available.</returns>
    public bool TryReadValue(out ReadOnlyMemory<char> value)
    {
        if (!_lastWasOption)
        {
            value = default;
            return false;
        }

        _lastWasOption = false;

        if (_lastHasValue)
        {
            value = _args[_lastArgIndex].AsMemory(_lastValueStart, _lastValueLength);
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
        value = next.AsMemory();
        return true;
    }

    private CommandLineToken ReadOption(ReadOnlySpan<char> body, int argIndex, int prefixLength)
    {
        _lastWasOption = true;
        _lastArgIndex = argIndex;

        var equals = body.IndexOf('=');

        if (equals < 0)
        {
            _lastHasValue = false;
            _lastValueStart = 0;
            _lastValueLength = 0;
            return CommandLineToken.Option(body, prefixLength: prefixLength);
        }

        _lastHasValue = true;
        _lastValueStart = prefixLength + equals + 1;
        _lastValueLength = body.Length - equals - 1;

        return CommandLineToken.Option(
            body[..equals],
            body[(equals + 1)..],
            hasValue: true,
            prefixLength: prefixLength);
    }
}
