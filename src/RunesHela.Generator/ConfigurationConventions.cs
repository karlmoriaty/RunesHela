using System;
using System.Text;

namespace RunesHela.Generator;

internal static class ConfigurationConventions
{
    public static string ConfigKey(string commandPath, OptionModel option)
    {
        if (option.ConfigKey is not null)
        {
            return option.ConfigKey;
        }

        var builder = new StringBuilder(commandPath.Length + option.LongName.Length + 1);
        var start = 0;

        while (start < commandPath.Length)
        {
            var space = commandPath.IndexOf(' ', start);

            if (space < 0)
            {
                builder.Append(commandPath, start, commandPath.Length - start);
                break;
            }

            builder.Append(commandPath, start, space - start).Append('.');
            start = space + 1;
        }

        return builder.Append('.').Append(option.LongName).ToString();
    }

    public static string EnvVar(string appName, string commandPath, OptionModel option)
    {
        if (option.EnvVar is not null)
        {
            return option.EnvVar;
        }

        var builder = new StringBuilder(appName.Length + commandPath.Length + option.LongName.Length + 3);
        AppendUpper(builder, appName);
        builder.Append('_');
        AppendPath(builder, commandPath);
        builder.Append('_');
        AppendUpper(builder, option.LongName);
        return builder.ToString();
    }

    private static void AppendPath(StringBuilder builder, string commandPath)
    {
        var previousWasSeparator = false;

        foreach (var c in commandPath)
        {
            var separator = c == ' ' || c == '-' || c == '.';

            if (separator)
            {
                if (!previousWasSeparator && builder.Length > 0)
                {
                    builder.Append('_');
                }

                previousWasSeparator = true;
                continue;
            }

            builder.Append(char.ToUpperInvariant(c));
            previousWasSeparator = false;
        }
    }

    private static void AppendUpper(StringBuilder builder, string value)
    {
        var previousWasSeparator = false;

        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c))
            {
                if (!previousWasSeparator && builder.Length > 0)
                {
                    builder.Append('_');
                }

                previousWasSeparator = true;
                continue;
            }

            builder.Append(char.ToUpperInvariant(c));
            previousWasSeparator = false;
        }
    }
}
