using System;
using System.Text;

namespace RunesHela.Generator;

internal static class Names
{
    public static string ToKebab(string name)
    {
        if (name.Length == 0)
        {
            return "command";
        }

        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                var previousIsLowerOrDigit = i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                var previousIsUpper = i > 0 && char.IsUpper(name[i - 1]);

                if (i > 0 && (previousIsLowerOrDigit || (previousIsUpper && nextIsLower)))
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(c));
                continue;
            }

            if (c == '_' || c == ' ')
            {
                if (builder.Length > 0 && builder[builder.Length - 1] != '-')
                {
                    builder.Append('-');
                }

                continue;
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.Length == 0 ? "command" : builder.ToString();
    }

    public static string ToCommandName(string typeName)
    {
        const string suffix = "Command";

        if (typeName.Length > suffix.Length && typeName.EndsWith(suffix, StringComparison.Ordinal))
        {
            typeName = typeName.Substring(0, typeName.Length - suffix.Length);
        }

        return ToKebab(typeName);
    }

    public static string ToUpperKebab(string name)
    {
        return ToKebab(name).ToUpperInvariant();
    }

    public static bool IsValidCliName(string name)
    {
        if (name.Length == 0 || name[0] == '-')
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!(char.IsLower(c) || (c >= '0' && c <= '9') || c == '-' || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    public static string ValueNameOf(TypeModel type)
    {
        if (type.Kind == TypeKind.Array && type.ElementType is not null)
        {
            return ValueNameOf(type.ElementType);
        }

        return type.Kind switch
        {
            TypeKind.String => "VALUE",
            TypeKind.Char => "CHAR",
            TypeKind.Boolean => "BOOL",
            TypeKind.Integer or TypeKind.Floating or TypeKind.Decimal => "NUMBER",
            TypeKind.Enum => ToUpperKebab(type.SimpleName),
            TypeKind.Guid => "GUID",
            TypeKind.DateTime or TypeKind.DateTimeOffset => "TIMESTAMP",
            TypeKind.TimeSpan => "DURATION",
            TypeKind.Uri => "URL",
            TypeKind.FileInfo or TypeKind.DirectoryInfo => "PATH",
            _ => ToUpperKebab(type.SimpleName),
        };
    }
}
