using System.Globalization;
using System.Text;

namespace RunesHela.Generator;

internal sealed class JsonBuilder
{
    private readonly StringBuilder _builder = new StringBuilder();
    private bool _needsComma;

    public JsonBuilder StartObject()
    {
        Separate();
        _builder.Append('{');
        _needsComma = false;
        return this;
    }

    public JsonBuilder EndObject()
    {
        _builder.Append('}');
        _needsComma = true;
        return this;
    }

    public JsonBuilder StartArray()
    {
        Separate();
        _builder.Append('[');
        _needsComma = false;
        return this;
    }

    public JsonBuilder EndArray()
    {
        _builder.Append(']');
        _needsComma = true;
        return this;
    }

    public JsonBuilder Name(string name)
    {
        Separate();
        AppendString(name);
        _builder.Append(':');
        _needsComma = false;
        return this;
    }

    public JsonBuilder Value(string? value)
    {
        Separate();

        if (value is null)
        {
            _builder.Append("null");
        }
        else
        {
            AppendString(value);
        }

        _needsComma = true;
        return this;
    }

    public JsonBuilder Value(bool value)
    {
        Separate();
        _builder.Append(value ? "true" : "false");
        _needsComma = true;
        return this;
    }

    public JsonBuilder Value(int value)
    {
        Separate();
        _builder.Append(value.ToString(CultureInfo.InvariantCulture));
        _needsComma = true;
        return this;
    }

    public JsonBuilder Raw(string raw)
    {
        Separate();
        _builder.Append(raw);
        _needsComma = true;
        return this;
    }

    public override string ToString()
    {
        return _builder.ToString();
    }

    private void Separate()
    {
        if (_needsComma)
        {
            _builder.Append(',');
        }
    }

    private void AppendString(string value)
    {
        _builder.Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    _builder.Append("\\\"");
                    break;
                case '\\':
                    _builder.Append("\\\\");
                    break;
                case '\b':
                    _builder.Append("\\b");
                    break;
                case '\f':
                    _builder.Append("\\f");
                    break;
                case '\n':
                    _builder.Append("\\n");
                    break;
                case '\r':
                    _builder.Append("\\r");
                    break;
                case '\t':
                    _builder.Append("\\t");
                    break;
                default:
                    if (c < ' ')
                    {
                        _builder.Append("\\u");
                        _builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        _builder.Append(c);
                    }

                    break;
            }
        }

        _builder.Append('"');
    }
}
