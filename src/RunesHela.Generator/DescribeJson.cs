using System.Collections.Generic;

namespace RunesHela.Generator;

internal static class DescribeJson
{
    public static string Build(string appName, IReadOnlyList<CommandNode> roots)
    {
        var builder = new JsonBuilder();

        builder.StartObject();
        builder.Name("schemaVersion").Value("1.0");
        builder.Name("kind").Value("describe");
        builder.Name("application").Value(appName);
        builder.Name("commands").StartArray();

        foreach (var root in roots)
        {
            AppendCommand(builder, root);
        }

        builder.EndArray();
        builder.EndObject();
        return builder.ToString();
    }

    private static void AppendCommand(JsonBuilder builder, CommandNode node)
    {
        var model = node.Model;

        builder.StartObject();
        builder.Name("name").Value(model.Name);
        builder.Name("path").Value(node.Path);
        builder.Name("description").Value(model.Description);

        builder.Name("aliases").StartArray();

        foreach (var alias in model.Aliases)
        {
            builder.Value(alias);
        }

        builder.EndArray();

        builder.Name("hidden").Value(model.IsHidden);
        builder.Name("destructive").Value(model.IsDestructive);

        builder.Name("arguments").StartArray();

        foreach (var parameter in model.Parameters)
        {
            HelpJson.AppendArgument(builder, parameter);
        }

        builder.EndArray();
        builder.Name("options").StartArray();

        foreach (var option in model.Options)
        {
            HelpJson.AppendOption(builder, option);
        }

        if (model.IsDestructive)
        {
            HelpJson.AppendGlobalOption(builder, "yes", "y", "Confirm the destructive operation.");
            HelpJson.AppendGlobalOption(builder, "no-input", null, "Disable interactive prompts.");
        }

        HelpJson.AppendGlobalOption(builder, "help", "h", "Show help.");

        builder.EndArray();
        builder.Name("examples").StartArray();

        foreach (var example in model.Examples)
        {
            builder.Value(example);
        }

        builder.EndArray();
        builder.Name("exitCodes").StartArray();
        HelpJson.AppendExitCode(builder, 0, "Success");
        HelpJson.AppendExitCode(builder, 1, "Failure");
        HelpJson.AppendExitCode(builder, 2, "Usage error");
        HelpJson.AppendExitCode(builder, 3, "Configuration error");
        HelpJson.AppendExitCode(builder, 4, "Not found");
        HelpJson.AppendExitCode(builder, 5, "Permission denied");
        HelpJson.AppendExitCode(builder, 6, "Conflict");
        HelpJson.AppendExitCode(builder, 7, "Timeout");
        HelpJson.AppendExitCode(builder, 130, "Interrupted");
        HelpJson.AppendExitCode(builder, 141, "Broken pipe");
        builder.EndArray();
        builder.Name("commands").StartArray();

        foreach (var child in node.Children)
        {
            AppendCommand(builder, child);
        }

        builder.EndArray();
        builder.EndObject();
    }
}
