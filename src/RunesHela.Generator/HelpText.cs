using System;
using System.Collections.Generic;

namespace RunesHela.Generator;

internal static class HelpText
{
    public static string Build(string appName, CommandNode? node, IReadOnlyList<CommandNode> children, bool isRoot)
    {
        var lines = new List<string>();
        var model = node?.Model;

        if (isRoot)
        {
            lines.Add(appName);
            lines.Add(string.Empty);
            lines.Add("Usage: " + appName + " <command> [options]");
        }
        else
        {
            lines.Add("Usage: " + BuildUsage(appName, node!));

            if (model!.Description.Length > 0)
            {
                lines.Add(string.Empty);
                lines.Add(model.Description);
            }
        }

        if (!isRoot && model!.Parameters.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Arguments:");
            AppendRows(lines, BuildArgumentRows(model));
        }

        if (children.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Commands:");
            AppendRows(lines, BuildCommandRows(children));
        }

        lines.Add(string.Empty);
        lines.Add("Options:");
        AppendRows(lines, BuildOptionRows(model, isRoot, appName, node));

        if (!isRoot && model!.Examples.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Examples:");

            foreach (var example in model.Examples)
            {
                foreach (var line in example.Split('\n'))
                {
                    lines.Add("  " + line);
                }
            }
        }

        lines.Add(string.Empty);
        lines.Add("Exit codes:");
        lines.Add("  0  Success");
        lines.Add("  1  Failure");
        lines.Add("  2  Usage error");

        return string.Join("\n", lines);
    }

    private static string BuildUsage(string appName, CommandNode node)
    {
        var builder = new List<string> { appName, node.Path };

        foreach (var parameter in node.Model.Parameters)
        {
            if (parameter.IsParams)
            {
                builder.Add("<" + parameter.Name + ">...");
            }
            else if (parameter.IsRequired)
            {
                builder.Add("<" + parameter.Name + ">");
            }
            else
            {
                builder.Add("[<" + parameter.Name + ">]");
            }
        }

        if (node.Children.Count > 0)
        {
            builder.Add("[command]");
        }

        builder.Add("[options]");
        return string.Join(" ", builder);
    }
    private static List<Row> BuildArgumentRows(CommandModel model)
    {
        var rows = new List<Row>();

        foreach (var parameter in model.Parameters)
        {
            var left = parameter.IsParams
                ? "  <" + parameter.Name + ">..."
                : parameter.IsRequired
                    ? "  <" + parameter.Name + ">"
                    : "  [<" + parameter.Name + ">]";

            var right = parameter.Description;

            if (parameter.HasDefault && parameter.DefaultDisplay.Length > 0)
            {
                right = AppendAnnotation(right, "[default: " + parameter.DefaultDisplay + "]");
            }

            rows.Add(new Row(left, right));
        }

        return rows;
    }

    private static List<Row> BuildCommandRows(IReadOnlyList<CommandNode> children)
    {
        var rows = new List<Row>();

        foreach (var child in children)
        {
            if (!child.Model.IsHidden)
            {
                rows.Add(new Row("  " + child.Model.Name, child.Model.Description));
            }
        }

        if (rows.Count == 0)
        {
            rows.Add(new Row("  (none)", "No visible commands."));
        }

        return rows;
    }

    private static List<Row> BuildOptionRows(CommandModel? model, bool isRoot, string appName, CommandNode? node)
    {
        var rows = new List<Row>();

        if (model is not null)
        {
            foreach (var option in model.Options)
            {
                if (option.IsHidden)
                {
                    continue;
                }

                var left = option.ShortName == '\0'
                    ? "      --" + option.LongName
                    : "  -" + option.ShortName + ", --" + option.LongName;

                if (!option.IsFlag)
                {
                    left += " <" + option.ValueName + ">";
                }

                var right = option.Description;

                if (option.IsRequired)
                {
                    right = AppendAnnotation(right, "[required]");
                }

                if (option.HasDefault)
                {
                    var display = option.IsSecret ? "***" : option.DefaultDisplay;

                    if (display.Length > 0)
                    {
                        right = AppendAnnotation(right, "[default: " + display + "]");
                    }
                }

                var envVar = ConfigurationConventions.EnvVar(appName, node!.Path, option);
                right = AppendAnnotation(right, "[env: " + envVar + "]");

                if (option.IsSecret)
                {
                    right = AppendAnnotation(right, "[secret]");
                }

                rows.Add(new Row(left, right));
            }

            if (model.IsDestructive)
            {
                rows.Add(new Row("  -y, --yes", "Confirm the destructive operation."));
                rows.Add(new Row("      --no-input", "Disable interactive prompts."));
            }
        }

        rows.Add(new Row("      --config <PATH>", "Read configuration from PATH."));
        rows.Add(new Row("  -h, --help", "Show help."));

        if (isRoot)
        {
            rows.Add(new Row("      --version", "Show version."));
        }

        return rows;
    }

    private static string AppendAnnotation(string text, string annotation)
    {
        return text.Length == 0 ? annotation : text + " " + annotation;
    }

    private static void AppendRows(List<string> lines, List<Row> rows)
    {
        var width = 0;

        foreach (var row in rows)
        {
            if (row.Right.Length > 0)
            {
                width = Math.Max(width, row.Left.Length);
            }
        }

        foreach (var row in rows)
        {
            if (row.Right.Length == 0)
            {
                lines.Add(row.Left);
                continue;
            }

            lines.Add(row.Left.PadRight(width) + "  " + row.Right);
        }
    }

    private readonly struct Row
    {
        public Row(string left, string right)
        {
            Left = left;
            Right = right;
        }

        public string Left { get; }

        public string Right { get; }
    }
}
