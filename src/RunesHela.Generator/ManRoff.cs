using System;
using System.Collections.Generic;
using System.Text;

namespace RunesHela.Generator;

internal static class ManRoff
{
    public static string Build(string appName, IReadOnlyList<CommandNode> roots)
    {
        var builder = new StringBuilder();
        builder.Append(".TH \"").Append(appName.ToUpperInvariant()).Append("\" \"1\" \"\" \"")
            .Append(appName).Append("\" \"").Append(appName).AppendLine(" Manual\"");
        builder.AppendLine(".SH NAME");
        builder.Append(appName).AppendLine(" \\- command-line application");
        builder.AppendLine(".SH SYNOPSIS");
        builder.Append(".B ").AppendLine(appName);
        builder.AppendLine("[\\fIcommand\\fR] [\\fIoptions\\fR]");
        builder.AppendLine(".SH COMMANDS");

        foreach (var root in roots)
        {
            AppendNode(builder, root);
        }

        return builder.ToString().Replace("\r\n", "\n");
    }

    private static void AppendNode(StringBuilder builder, CommandNode node)
    {
        builder.AppendLine(".TP");
        builder.Append(".B ").AppendLine(Roff(node.Path));

        if (node.Model.Description.Length > 0)
        {
            builder.AppendLine(Roff(node.Model.Description));
        }

        if (node.Model.Parameters.Count > 0)
        {
            builder.AppendLine(".RS");
            builder.AppendLine(".SH ARGUMENTS");

            foreach (var parameter in node.Model.Parameters)
            {
                builder.AppendLine(".TP");
                builder.Append(".B \\fI").Append(parameter.Name).AppendLine("\\fR");

                if (parameter.Description.Length > 0)
                {
                    builder.AppendLine(Roff(parameter.Description));
                }
            }

            builder.AppendLine(".RE");
        }

        if (node.Model.Options.Count > 0)
        {
            builder.AppendLine(".RS");

            foreach (var option in node.Model.Options)
            {
                if (option.IsHidden)
                {
                    continue;
                }

                builder.AppendLine(".TP");
                builder.Append(".B ").AppendLine(Roff(option.ShortName == '\0'
                    ? "\\-\\-" + option.LongName
                    : "\\-" + option.ShortName + ", \\-\\-" + option.LongName));

                if (option.Description.Length > 0)
                {
                    builder.AppendLine(Roff(option.Description));
                }
            }

            builder.AppendLine(".RE");
        }

        foreach (var child in node.Children)
        {
            AppendNode(builder, child);
        }
    }

    private static string Roff(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\r", " ").Replace("\n", " ");
    }
}
