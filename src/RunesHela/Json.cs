using System.Globalization;
using System.Text;

namespace RunesHela;

internal enum JsonKind
{
    Null,
    Boolean,
    Number,
    String,
    Array,
    Object,
}

internal sealed class JsonValue
{
    private static readonly JsonValue s_null = new(JsonKind.Null, string.Empty, null, null);

    private JsonValue(
        JsonKind kind,
        string text,
        List<JsonValue>? items,
        Dictionary<string, JsonValue>? properties)
    {
        Kind = kind;
        Text = text;
        Items = items;
        Properties = properties;
    }

    public JsonKind Kind { get; }

    public string Text { get; }

    public List<JsonValue>? Items { get; }

    public Dictionary<string, JsonValue>? Properties { get; }

    public static JsonValue Null => s_null;

    public static JsonValue Boolean(bool value)
    {
        return new JsonValue(JsonKind.Boolean, value ? "true" : "false", null, null);
    }

    public static JsonValue Number(string raw)
    {
        return new JsonValue(JsonKind.Number, raw, null, null);
    }

    public static JsonValue String(string value)
    {
        return new JsonValue(JsonKind.String, value, null, null);
    }

    public static JsonValue Array(List<JsonValue> items)
    {
        return new JsonValue(JsonKind.Array, string.Empty, items, null);
    }

    public static JsonValue Object(Dictionary<string, JsonValue> properties)
    {
        return new JsonValue(JsonKind.Object, string.Empty, null, properties);
    }

    public bool TryGetProperty(string name, out JsonValue value)
    {
        if (Properties is not null && Properties.TryGetValue(name, out var found))
        {
            value = found;
            return true;
        }

        value = Null;
        return false;
    }

    public bool TryGetInt32(out int value)
    {
        if (Kind == JsonKind.Number &&
            int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }
}

internal sealed class JsonParseException : Exception
{
    public JsonParseException(string message, int line, int column)
        : base(message)
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }

    public int Column { get; }
}

internal static class JsonParser
{
    private const int MaxDepth = 64;

    public static JsonValue Parse(string text)
    {
        var parser = new Parser(text);
        return parser.ParseDocument();
    }

    public static JsonValue Parse(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return Parse(reader.ReadToEnd());
    }

    private struct Parser
    {
        private readonly string _text;
        private int _index;
        private int _depth;

        public Parser(string text)
        {
            _text = text;
            _index = 0;
            _depth = 0;
        }

        public JsonValue ParseDocument()
        {
            SkipWhitespace();
            var value = ParseValue();

            SkipWhitespace();

            if (_index != _text.Length)
            {
                throw Error("Unexpected trailing content");
            }

            return value;
        }

        private JsonValue ParseValue()
        {
            if (_index >= _text.Length)
            {
                throw Error("Unexpected end of input");
            }

            switch (_text[_index])
            {
                case '{':
                    return ParseObject();

                case '[':
                    return ParseArray();

                case '"':
                    return JsonValue.String(ParseString());

                case 't':
                    Expect("true");
                    return JsonValue.Boolean(true);

                case 'f':
                    Expect("false");
                    return JsonValue.Boolean(false);

                case 'n':
                    Expect("null");
                    return JsonValue.Null;

                default:
                    return ParseNumber();
            }
        }

        private JsonValue ParseObject()
        {
            Enter();

            var properties = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            _index++;
            SkipWhitespace();

            if (TryConsume('}'))
            {
                Leave();
                return JsonValue.Object(properties);
            }

            while (true)
            {
                SkipWhitespace();

                if (_index >= _text.Length || _text[_index] != '"')
                {
                    throw Error("Expected a property name");
                }

                var name = ParseString();
                SkipWhitespace();
                Expect(':');
                SkipWhitespace();
                properties[name] = ParseValue();
                SkipWhitespace();

                if (TryConsume('}'))
                {
                    Leave();
                    return JsonValue.Object(properties);
                }

                Expect(',');
                SkipWhitespace();

                if (TryConsume('}'))
                {
                    Leave();
                    return JsonValue.Object(properties);
                }
            }
        }

        private JsonValue ParseArray()
        {
            Enter();

            var items = new List<JsonValue>();
            _index++;
            SkipWhitespace();

            if (TryConsume(']'))
            {
                Leave();
                return JsonValue.Array(items);
            }

            while (true)
            {
                SkipWhitespace();
                items.Add(ParseValue());
                SkipWhitespace();

                if (TryConsume(']'))
                {
                    Leave();
                    return JsonValue.Array(items);
                }

                Expect(',');
                SkipWhitespace();

                if (TryConsume(']'))
                {
                    Leave();
                    return JsonValue.Array(items);
                }
            }
        }

        private string ParseString()
        {
            _index++;
            var start = _index;
            StringBuilder? builder = null;

            while (_index < _text.Length)
            {
                var c = _text[_index];

                if (c == '"')
                {
                    var value = builder is null
                        ? _text.Substring(start, _index - start)
                        : builder.Append(_text, start, _index - start).ToString();

                    _index++;
                    return value;
                }

                if (c != '\\')
                {
                    if (c < ' ')
                    {
                        throw Error("Control characters must be escaped");
                    }

                    _index++;
                    continue;
                }

                builder ??= new StringBuilder(Math.Max(16, _index - start + 8));
                builder.Append(_text, start, _index - start);
                _index++;

                if (_index >= _text.Length)
                {
                    throw Error("Unterminated escape sequence");
                }

                var escape = _text[_index++];

                switch (escape)
                {
                    case '"':
                        builder.Append('"');
                        break;

                    case '\\':
                        builder.Append('\\');
                        break;

                    case '/':
                        builder.Append('/');
                        break;

                    case 'b':
                        builder.Append('\b');
                        break;

                    case 'f':
                        builder.Append('\f');
                        break;

                    case 'n':
                        builder.Append('\n');
                        break;

                    case 'r':
                        builder.Append('\r');
                        break;

                    case 't':
                        builder.Append('\t');
                        break;

                    case 'u':
                        builder.Append(ParseUnicode());
                        break;

                    default:
                        throw Error("Invalid escape sequence");
                }

                start = _index;
            }

            throw Error("Unterminated string");
        }

        private char ParseUnicode()
        {
            if (_index + 4 > _text.Length)
            {
                throw Error("Incomplete unicode escape");
            }

            var value = 0;

            for (var i = 0; i < 4; i++)
            {
                var c = _text[_index + i];
                var digit = c switch
                {
                    >= '0' and <= '9' => c - '0',
                    >= 'a' and <= 'f' => c - 'a' + 10,
                    >= 'A' and <= 'F' => c - 'A' + 10,
                    _ => -1,
                };

                if (digit < 0)
                {
                    throw Error("Invalid unicode escape");
                }

                value = (value << 4) | digit;
            }

            _index += 4;
            return (char)value;
        }

        private JsonValue ParseNumber()
        {
            var start = _index;

            if (TryConsume('-'))
            {
            }

            if (_index >= _text.Length || !IsDigit(_text[_index]))
            {
                throw Error("Invalid number");
            }

            if (_text[_index] == '0')
            {
                _index++;
            }
            else
            {
                while (_index < _text.Length && IsDigit(_text[_index]))
                {
                    _index++;
                }
            }

            if (_index < _text.Length && _text[_index] == '.')
            {
                _index++;

                if (_index >= _text.Length || !IsDigit(_text[_index]))
                {
                    throw Error("Invalid number");
                }

                while (_index < _text.Length && IsDigit(_text[_index]))
                {
                    _index++;
                }
            }

            if (_index < _text.Length && (_text[_index] == 'e' || _text[_index] == 'E'))
            {
                _index++;

                if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-'))
                {
                    _index++;
                }

                if (_index >= _text.Length || !IsDigit(_text[_index]))
                {
                    throw Error("Invalid number");
                }

                while (_index < _text.Length && IsDigit(_text[_index]))
                {
                    _index++;
                }
            }

            return JsonValue.Number(_text.Substring(start, _index - start));
        }

        private void Expect(string literal)
        {
            if (_index + literal.Length > _text.Length ||
                string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0)
            {
                throw Error("Invalid literal");
            }

            _index += literal.Length;
        }

        private void Expect(char value)
        {
            if (_index >= _text.Length || _text[_index] != value)
            {
                throw Error("Expected '" + value + "'");
            }

            _index++;
        }

        private bool TryConsume(char value)
        {
            if (_index < _text.Length && _text[_index] == value)
            {
                _index++;
                return true;
            }

            return false;
        }

        private void SkipWhitespace()
        {
            while (_index < _text.Length)
            {
                var c = _text[_index];

                if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
                {
                    return;
                }

                _index++;
            }
        }

        private void Enter()
        {
            _depth++;

            if (_depth > MaxDepth)
            {
                throw Error("The JSON value is nested too deeply");
            }
        }

        private void Leave()
        {
            _depth--;
        }

        private JsonParseException Error(string message)
        {
            var line = 1;
            var column = 1;

            for (var i = 0; i < _index && i < _text.Length; i++)
            {
                if (_text[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return new JsonParseException(message + " at line " + line + ", column " + column + ".", line, column);
        }

        private static bool IsDigit(char value)
        {
            return value >= '0' && value <= '9';
        }
    }
}

internal static class JsonWriter
{
    public static void Write(StringBuilder builder, JsonValue value)
    {
        switch (value.Kind)
        {
            case JsonKind.Null:
                builder.Append("null");
                break;

            case JsonKind.Boolean:
            case JsonKind.Number:
                builder.Append(value.Text);
                break;

            case JsonKind.String:
                WriteString(builder, value.Text);
                break;

            case JsonKind.Array:
                builder.Append('[');

                for (var i = 0; i < value.Items!.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    Write(builder, value.Items[i]);
                }

                builder.Append(']');
                break;

            case JsonKind.Object:
                builder.Append('{');
                var first = true;

                foreach (var property in value.Properties!)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    WriteString(builder, property.Key);
                    builder.Append(':');
                    Write(builder, property.Value);
                    first = false;
                }

                builder.Append('}');
                break;
        }
    }

    public static string ToJson(JsonValue value)
    {
        var builder = new StringBuilder();
        Write(builder, value);
        return builder.ToString();
    }

    public static void WriteString(StringBuilder builder, string value)
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
                        builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
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
}
