using System;
using System.Collections.Generic;

namespace RunesHela.Generator;

internal static class CompletionData
{
    public static List<string> RootCandidates(IReadOnlyList<CommandNode> roots)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in roots)
        {
            if (root.Model.IsHidden)
            {
                continue;
            }

            Add(result, seen, root.Model.Name);

            foreach (var alias in root.Model.Aliases)
            {
                Add(result, seen, alias);
            }
        }

        Add(result, seen, "--help");
        Add(result, seen, "--version");
        return result;
    }

    public static List<string> Candidates(CommandNode node, bool isRoot)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var child in node.Children)
        {
            if (child.Model.IsHidden)
            {
                continue;
            }

            Add(result, seen, child.Model.Name);

            foreach (var alias in child.Model.Aliases)
            {
                Add(result, seen, alias);
            }
        }

        foreach (var option in node.Model.Options)
        {
            if (option.IsHidden)
            {
                continue;
            }

            Add(result, seen, "--" + option.LongName);

            foreach (var alias in option.Aliases)
            {
                Add(result, seen, "--" + alias);
            }

            if (option.ShortName != '\0')
            {
                Add(result, seen, "-" + option.ShortName);
            }
        }

        if (node.Model.IsDestructive)
        {
            Add(result, seen, "--yes");
            Add(result, seen, "-y");
            Add(result, seen, "--no-input");
        }

        Add(result, seen, "--help");

        if (isRoot)
        {
            Add(result, seen, "--version");
        }

        return result;
    }

    public static List<CommandNode> Flatten(IReadOnlyList<CommandNode> roots)
    {
        var nodes = new List<CommandNode>();
        Collect(roots, nodes);
        return nodes;
    }

    private static void Collect(IReadOnlyList<CommandNode> nodes, List<CommandNode> result)
    {
        foreach (var node in nodes)
        {
            result.Add(node);
            Collect(node.Children, result);
        }
    }

    private static void Add(List<string> values, HashSet<string> seen, string value)
    {
        if (seen.Add(value))
        {
            values.Add(value);
        }
    }
}
