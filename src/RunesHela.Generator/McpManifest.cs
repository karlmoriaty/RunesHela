using System;
using System.Collections.Generic;

namespace RunesHela.Generator;

internal static class McpManifest
{
    public static string Build(string appName, IReadOnlyList<CommandNode> roots)
    {
        var builder = new JsonBuilder();
        builder.StartObject();
        builder.Name("schemaVersion").Value("1.0");
        builder.Name("kind").Value("mcp");
        builder.Name("application").Value(appName);
        builder.Name("tools").StartArray();

        foreach (var node in CompletionData.Flatten(roots))
        {
            if (node.Model.HandlerName is null || node.Model.IsHidden)
            {
                continue;
            }

            AppendTool(builder, node);
        }

        builder.EndArray();
        builder.EndObject();
        return builder.ToString();
    }

    private static void AppendTool(JsonBuilder builder, CommandNode node)
    {
        var model = node.Model;

        builder.StartObject();
        builder.Name("name").Value(node.Path.Replace(' ', '_'));
        builder.Name("description").Value(model.Description.Length == 0 ? node.Path : model.Description);
        builder.Name("inputSchema");
        AppendInputSchema(builder, node);
        builder.Name("annotations").StartObject();
        builder.Name("readOnlyHint").Value(false);
        builder.Name("destructiveHint").Value(model.IsDestructive);
        builder.Name("idempotentHint").Value(!model.IsDestructive);
        builder.Name("openWorldHint").Value(true);
        builder.EndObject();
        builder.Name("x-runeshela-path").Value(node.Path);
        builder.EndObject();
    }

    private static void AppendInputSchema(JsonBuilder builder, CommandNode node)
    {
        var model = node.Model;
        var used = new HashSet<string>(StringComparer.Ordinal);
        var required = new List<string>();

        builder.StartObject();
        builder.Name("type").Value("object");
        builder.Name("properties").StartObject();

        foreach (var parameter in model.Parameters)
        {
            var property = MakeUnique(used, TypeSchema.PropertyName(parameter.Name), "arg");

            builder.Name(property);
            builder.StartObject();
            AppendValueSchema(builder, parameter.Type);
            AppendDescription(builder, parameter.Description);
            builder.Name("x-runeshela-position").Value(parameter.Position);
            builder.EndObject();

            if (parameter.IsRequired && !parameter.IsParams)
            {
                required.Add(property);
            }
        }

        foreach (var option in model.Options)
        {
            if (option.IsHidden)
            {
                continue;
            }

            var property = MakeUnique(used, option.LongName, "opt");

            builder.Name(property);
            builder.StartObject();
            AppendValueSchema(builder, option.Type);
            AppendDescription(builder, option.Description);
            TypeSchema.AppendDefault(builder, option);
            builder.Name("x-runeshela-option").Value(option.LongName);

            if (option.IsNegatable)
            {
                builder.Name("x-runeshela-negatable").Value(true);
            }

            builder.EndObject();

            if (option.IsRequired)
            {
                required.Add(property);
            }
        }

        if (model.IsDestructive)
        {
            builder.Name("yes");
            builder.StartObject();
            builder.Name("type").Value("boolean");
            builder.Name("description").Value("Confirm the destructive operation.");
            builder.Name("x-runeshela-global").Value("yes");
            builder.EndObject();
        }

        builder.EndObject();
        builder.Name("required").StartArray();

        foreach (var name in required)
        {
            builder.Value(name);
        }

        builder.EndArray();
        builder.Name("additionalProperties").Value(false);
        builder.EndObject();
    }

    private static void AppendValueSchema(JsonBuilder builder, TypeModel type)
    {
        if (type.Kind == TypeKind.Array && type.ElementType is not null)
        {
            builder.Name("type").Value("array");
            builder.Name("items").StartObject();
            TypeSchema.Append(builder, type.ElementType);
            builder.EndObject();
            return;
        }

        TypeSchema.Append(builder, type);
    }

    private static void AppendDescription(JsonBuilder builder, string description)
    {
        if (description.Length > 0)
        {
            builder.Name("description").Value(description);
        }
    }

    private static string MakeUnique(HashSet<string> used, string name, string prefix)
    {
        if (used.Add(name))
        {
            return name;
        }

        var index = 2;
        var candidate = prefix + "-" + name;

        while (!used.Add(candidate))
        {
            candidate = prefix + index + "-" + name;
            index++;
        }

        return candidate;
    }
}
