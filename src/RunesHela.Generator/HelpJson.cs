using System;
using System.Collections.Generic;

namespace RunesHela.Generator;

internal static class HelpJson
{
    public static string Build(
        string appName,
        CommandNode? node,
        IReadOnlyList<CommandNode> children,
        bool isRoot)
    {
        var model = node?.Model;
        var builder = new JsonBuilder();

        builder.StartObject();
        builder.Name("schemaVersion").Value("1.0");
        builder.Name("kind").Value(isRoot ? "application" : "command");
        builder.Name("name").Value(isRoot ? appName : model!.Name);
        builder.Name("path").Value(isRoot ? string.Empty : node!.Path);
        builder.Name("description").Value(isRoot ? null : model!.Description);

        builder.Name("aliases").StartArray();

        if (model is not null)
        {
            foreach (var alias in model.Aliases)
            {
                builder.Value(alias);
            }
        }

        builder.EndArray();
        builder.Name("hidden").Value(model is not null && model.IsHidden);
        builder.Name("destructive").Value(model is not null && model.IsDestructive);

        builder.Name("arguments").StartArray();

        if (model is not null)
        {
            foreach (var parameter in model.Parameters)
            {
                AppendArgument(builder, parameter);
            }
        }

        builder.EndArray();

        builder.Name("options").StartArray();

        if (model is not null)
        {
            foreach (var option in model.Options)
            {
                AppendOption(
                    builder,
                    option,
                    ConfigurationConventions.ConfigKey(node!.Path, option),
                    ConfigurationConventions.EnvVar(appName, node.Path, option));
            }

            if (model.IsDestructive)
            {
                AppendGlobalOption(builder, "yes", "y", "Confirm the destructive operation.");
                AppendGlobalOption(builder, "no-input", null, "Disable interactive prompts.");
            }
        }

        AppendConfigOption(builder);
        AppendGlobalOption(builder, "help", "h", "Show help.");

        if (isRoot)
        {
            AppendGlobalOption(builder, "version", null, "Show version.");
        }

        builder.EndArray();

        builder.Name("commands").StartArray();

        foreach (var child in children)
        {
            AppendCommandSummary(builder, child);
        }

        builder.EndArray();

        builder.Name("examples").StartArray();

        if (model is not null)
        {
            foreach (var example in model.Examples)
            {
                builder.Value(example);
            }
        }

        builder.EndArray();

        builder.Name("exitCodes").StartArray();
        AppendExitCode(builder, 0, "Success");
        AppendExitCode(builder, 1, "Failure");
        AppendExitCode(builder, 2, "Usage error");
        builder.EndArray();

        builder.EndObject();
        return builder.ToString();
    }

    internal static void AppendOption(JsonBuilder builder, OptionModel option, string configKey, string envVar)
    {
        builder.StartObject();
        builder.Name("name").Value(option.LongName);
        builder.Name("short").Value(option.ShortName == '\0' ? null : option.ShortName.ToString());

        builder.Name("aliases").StartArray();

        foreach (var alias in option.Aliases)
        {
            builder.Value(alias);
        }

        builder.EndArray();

        builder.Name("type").Value(TypeName(option.Type));
        builder.Name("valueName").Value(option.IsFlag ? null : option.ValueName);
        builder.Name("flag").Value(option.IsFlag);
        builder.Name("negatable").Value(option.IsNegatable);
        builder.Name("repeatable").Value(option.IsRepeatable);
        builder.Name("required").Value(option.IsRequired);
        AppendDefault(builder, option.HasDefault, option.IsSecret, option.Type, option.DefaultDisplay, option.DefaultLiteral);
        builder.Name("configKey").Value(configKey);
        builder.Name("env").Value(envVar);
        builder.Name("secret").Value(option.IsSecret);
        builder.Name("hidden").Value(option.IsHidden);
        builder.Name("description").Value(option.Description);
        builder.EndObject();
    }

    internal static void AppendArgument(JsonBuilder builder, ParameterModel parameter)
    {
        builder.StartObject();
        builder.Name("name").Value(parameter.Name);
        builder.Name("position").Value(parameter.Position);
        builder.Name("type").Value(TypeName(parameter.Type));
        builder.Name("required").Value(parameter.IsRequired);
        builder.Name("params").Value(parameter.IsParams);
        AppendDefault(
            builder,
            parameter.HasDefault,
            isSecret: false,
            parameter.Type,
            parameter.DefaultDisplay,
            parameter.DefaultLiteral);
        builder.Name("description").Value(parameter.Description);
        builder.EndObject();
    }

    private static void AppendDefault(
        JsonBuilder builder,
        bool hasDefault,
        bool isSecret,
        TypeModel type,
        string display,
        string literal)
    {
        var available = hasDefault && !isSecret;
        builder.Name("hasDefault").Value(available);
        builder.Name("default");

        if (!available || literal == "null" || display.Length == 0)
        {
            builder.Value((string?)null);
            return;
        }

        switch (type.Kind)
        {
            case TypeKind.Boolean:
                builder.Raw(literal == "true" ? "true" : "false");
                return;

            case TypeKind.Integer:
            case TypeKind.Floating:
            case TypeKind.Decimal:
                builder.Raw(display);
                return;

            default:
                builder.Value(display);
                return;
        }
    }

    internal static void AppendGlobalOption(
        JsonBuilder builder,
        string name,
        string? shortName,
        string description)
    {
        builder.StartObject();
        builder.Name("name").Value(name);
        builder.Name("short").Value(shortName);
        builder.Name("aliases").StartArray().EndArray();
        builder.Name("type").Value("bool");
        builder.Name("valueName").Value((string?)null);
        builder.Name("flag").Value(true);
        builder.Name("negatable").Value(false);
        builder.Name("repeatable").Value(false);
        builder.Name("required").Value(false);
        builder.Name("hasDefault").Value(false);
        builder.Name("default").Value((string?)null);
        builder.Name("configKey").Value((string?)null);
        builder.Name("env").Value((string?)null);
        builder.Name("secret").Value(false);
        builder.Name("hidden").Value(false);
        builder.Name("description").Value(description);
        builder.EndObject();
    }

    internal static void AppendConfigOption(JsonBuilder builder)
    {
        builder.StartObject();
        builder.Name("name").Value("config");
        builder.Name("short").Value((string?)null);
        builder.Name("aliases").StartArray().EndArray();
        builder.Name("type").Value("string");
        builder.Name("valueName").Value("PATH");
        builder.Name("flag").Value(false);
        builder.Name("negatable").Value(false);
        builder.Name("repeatable").Value(false);
        builder.Name("required").Value(false);
        builder.Name("hasDefault").Value(false);
        builder.Name("default").Value((string?)null);
        builder.Name("configKey").Value((string?)null);
        builder.Name("env").Value("RHL_CONFIG");
        builder.Name("secret").Value(false);
        builder.Name("hidden").Value(false);
        builder.Name("description").Value("Read configuration from PATH.");
        builder.EndObject();
    }

    private static void AppendCommandSummary(JsonBuilder builder, CommandNode child)
    {
        builder.StartObject();
        builder.Name("name").Value(child.Model.Name);

        builder.Name("aliases").StartArray();

        foreach (var alias in child.Model.Aliases)
        {
            builder.Value(alias);
        }

        builder.EndArray();

        builder.Name("description").Value(child.Model.Description);
        builder.Name("hidden").Value(child.Model.IsHidden);
        builder.EndObject();
    }

    internal static void AppendExitCode(JsonBuilder builder, int code, string description)
    {
        builder.StartObject();
        builder.Name("code").Value(code);
        builder.Name("description").Value(description);
        builder.EndObject();
    }

    private static string TypeName(TypeModel type)
    {
        const string prefix = "global::";
        return type.DisplayName.StartsWith(prefix, StringComparison.Ordinal)
            ? type.DisplayName.Substring(prefix.Length)
            : type.DisplayName;
    }
}
