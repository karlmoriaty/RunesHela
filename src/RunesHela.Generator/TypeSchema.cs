using System;

namespace RunesHela.Generator;

internal static class TypeSchema
{
    public static void Append(JsonBuilder builder, TypeModel type)
    {
        switch (type.Kind)
        {
            case TypeKind.Boolean:
                builder.Name("type").Value("boolean");
                break;

            case TypeKind.Integer:
                builder.Name("type").Value("integer");
                break;

            case TypeKind.Floating:
            case TypeKind.Decimal:
                builder.Name("type").Value("number");
                break;

            case TypeKind.Enum:
                builder.Name("type").Value("string");
                builder.Name("enum").StartArray();

                foreach (var member in type.Members)
                {
                    builder.Value(member);
                }

                builder.EndArray();
                break;

            case TypeKind.Guid:
                builder.Name("type").Value("string");
                builder.Name("format").Value("uuid");
                break;

            case TypeKind.Uri:
                builder.Name("type").Value("string");
                builder.Name("format").Value("uri");
                break;

            default:
                builder.Name("type").Value("string");
                break;
        }
    }

    public static void AppendDefault(JsonBuilder builder, OptionModel option)
    {
        if (!option.HasDefault || option.IsSecret || option.DefaultLiteral == "null" || option.DefaultDisplay.Length == 0)
        {
            return;
        }

        builder.Name("default");

        switch (option.Type.Kind)
        {
            case TypeKind.Boolean:
                builder.Raw(option.DefaultLiteral == "true" ? "true" : "false");
                break;

            case TypeKind.Integer:
            case TypeKind.Floating:
            case TypeKind.Decimal:
                builder.Raw(option.DefaultDisplay);
                break;

            default:
                builder.Value(option.DefaultDisplay);
                break;
        }
    }

    public static string PropertyName(string name)
    {
        return name.ToLowerInvariant();
    }
}
