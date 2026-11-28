using System;
using System.Collections.Generic;

namespace RunesHela.Generator;

internal static class ConfigSchema
{
    public static string Build(string appName, IReadOnlyList<CommandNode> roots)
    {
        var root = new SchemaNode();

        foreach (var node in CompletionData.Flatten(roots))
        {
            foreach (var option in node.Model.Options)
            {
                root.Add(ConfigurationConventions.ConfigKey(node.Path, option), option);
            }
        }

        var builder = new JsonBuilder();
        builder.StartObject();
        builder.Name("schemaVersion").Value("1.0");
        builder.Name("kind").Value("config");
        builder.Name("$schema").Value("https://json-schema.org/draft/2020-12/schema");
        builder.Name("$id").Value(appName + ".config.schema.json");
        builder.Name("title").Value(appName + " configuration");
        builder.Name("type").Value("object");
        builder.Name("additionalProperties").Value(true);
        builder.Name("properties");
        AppendObject(builder, root);
        builder.EndObject();
        return builder.ToString();
    }

    private static void AppendObject(JsonBuilder builder, SchemaNode node)
    {
        builder.StartObject();

        foreach (var pair in node.Children)
        {
            builder.Name(pair.Key);

            if (pair.Value.Option is null)
            {
                builder.StartObject();
                builder.Name("type").Value("object");
                builder.Name("additionalProperties").Value(true);
                builder.Name("properties");
                AppendObject(builder, pair.Value);
                builder.EndObject();
            }
            else
            {
                AppendValue(builder, pair.Value.Option);
            }
        }

        builder.EndObject();
    }

    private static void AppendValue(JsonBuilder builder, OptionModel option)
    {
        builder.StartObject();

        if (option.IsRepeatable)
        {
            builder.Name("type").Value("array");
            builder.Name("items").StartObject();
            AppendType(builder, option.Type.ElementType!);
            builder.EndObject();
        }
        else
        {
            AppendType(builder, option.Type);
        }

        if (option.Description.Length > 0)
        {
            builder.Name("description").Value(option.Description);
        }

        if (option.HasDefault && !option.IsSecret && option.DefaultLiteral != "null" && option.DefaultDisplay.Length > 0)
        {
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

        builder.EndObject();
    }

    private static void AppendType(JsonBuilder builder, TypeModel type)
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

    private sealed class SchemaNode
    {
        public SortedDictionary<string, SchemaNode> Children { get; } =
            new SortedDictionary<string, SchemaNode>(StringComparer.Ordinal);

        public OptionModel? Option { get; private set; }

        public void Add(string key, OptionModel option)
        {
            var node = this;
            var start = 0;

            while (true)
            {
                var dot = key.IndexOf('.', start);

                if (dot < 0)
                {
                    node = node.GetOrAdd(key.Substring(start));
                    node.Option = option;
                    return;
                }

                node = node.GetOrAdd(key.Substring(start, dot - start));
                start = dot + 1;
            }
        }

        private SchemaNode GetOrAdd(string name)
        {
            if (!Children.TryGetValue(name, out var child))
            {
                child = new SchemaNode();
                Children.Add(name, child);
            }

            return child;
        }
    }
}
