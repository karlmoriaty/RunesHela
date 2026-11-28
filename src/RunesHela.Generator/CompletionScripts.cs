using System;
using System.Collections.Generic;
using System.Text;

namespace RunesHela.Generator;

internal static class CompletionScripts
{
    public static string Bash(string appName, IReadOnlyList<CommandNode> roots)
    {
        var nodes = CompletionData.Flatten(roots);
        var builder = new StringBuilder();
        var function = FunctionName(appName);

        builder.Append("# bash completion for ").AppendLine(appName);
        builder.Append(function).AppendLine("()");
        builder.AppendLine("{");
        builder.AppendLine("    local cur=\"${COMP_WORDS[COMP_CWORD]}\"");
        builder.AppendLine("    local key=\"\"");
        builder.AppendLine("    if [ \"$COMP_CWORD\" -gt 1 ]; then");
        builder.AppendLine("        key=\"${COMP_WORDS[*]:1:$((COMP_CWORD-1))}\"");
        builder.AppendLine("    fi");
        builder.AppendLine("    local candidates=\"\"");
        builder.AppendLine("    case \"$key\" in");
        builder.Append("        \"\") candidates=\"").Append(string.Join(" ", CompletionData.RootCandidates(roots))).AppendLine("\" ;;");

        foreach (var node in nodes)
        {
            if (node.Children.Count == 0 && node.Model.Options.Count == 0)
            {
                continue;
            }

            builder.Append("        \"").Append(node.Path).Append("\") candidates=\"")
                .Append(string.Join(" ", CompletionData.Candidates(node, isRoot: false))).AppendLine("\" ;;");
        }

        builder.AppendLine("        *) candidates=\"\" ;;");
        builder.AppendLine("    esac");
        builder.AppendLine("    COMPREPLY=( $(compgen -W \"$candidates\" -- \"$cur\") )");
        builder.AppendLine("}");
        builder.Append("complete -F ").Append(function).Append(' ').AppendLine(appName);
        return builder.ToString().Replace("\r\n", "\n");
    }

    public static string Zsh(string appName, IReadOnlyList<CommandNode> roots)
    {
        var nodes = CompletionData.Flatten(roots);
        var builder = new StringBuilder();
        var function = FunctionName(appName);

        builder.Append("#compdef ").AppendLine(appName);
        builder.Append(function).AppendLine("()");
        builder.AppendLine("{");
        builder.AppendLine("    local key=\"${(j: :)words[2,CURRENT-1]}\"");
        builder.AppendLine("    local -a candidates");
        builder.AppendLine("    case \"$key\" in");
        builder.Append("        \"\") candidates=(").Append(string.Join(" ", CompletionData.RootCandidates(roots))).AppendLine(") ;;");

        foreach (var node in nodes)
        {
            if (node.Children.Count == 0 && node.Model.Options.Count == 0)
            {
                continue;
            }

            builder.Append("        \"").Append(node.Path).Append("\") candidates=(")
                .Append(string.Join(" ", CompletionData.Candidates(node, isRoot: false))).AppendLine(") ;;");
        }

        builder.AppendLine("        *) candidates=() ;;");
        builder.AppendLine("    esac");
        builder.AppendLine("    _describe 'values' candidates");
        builder.AppendLine("}");
        builder.Append("compdef ").Append(function).Append(' ').AppendLine(appName);
        return builder.ToString().Replace("\r\n", "\n");
    }

    public static string Fish(string appName, IReadOnlyList<CommandNode> roots)
    {
        var nodes = CompletionData.Flatten(roots);
        var builder = new StringBuilder();

        builder.Append("# fish completion for ").AppendLine(appName);
        builder.Append("complete -c ").Append(appName).AppendLine(" -f");

        foreach (var root in roots)
        {
            if (!root.Model.IsHidden)
            {
                builder.Append("complete -c ").Append(appName)
                    .Append(" -n '__fish_use_subcommand' -a '").Append(FishQuote(root.Model.Name))
                    .Append("' -d '").Append(FishQuote(root.Model.Description)).AppendLine("'");
            }
        }

        foreach (var node in nodes)
        {
            foreach (var child in node.Children)
            {
                if (child.Model.IsHidden)
                {
                    continue;
                }

                builder.Append("complete -c ").Append(appName)
                    .Append(" -n '__fish_seen_subcommand_from ").Append(LastSegment(node.Path)).Append("' -a '")
                    .Append(FishQuote(child.Model.Name)).Append("' -d '")
                    .Append(FishQuote(child.Model.Description)).AppendLine("'");
            }

            foreach (var option in node.Model.Options)
            {
                if (option.IsHidden)
                {
                    continue;
                }

                builder.Append("complete -c ").Append(appName);

                if (node.Path.Length > 0)
                {
                    builder.Append(" -n '__fish_seen_subcommand_from ").Append(LastSegment(node.Path)).Append('\'');
                }

                builder.Append(" -l ").Append(option.LongName);

                if (option.ShortName != '\0')
                {
                    builder.Append(" -s ").Append(option.ShortName);
                }

                if (option.Description.Length > 0)
                {
                    builder.Append(" -d '").Append(FishQuote(option.Description)).Append('\'');
                }

                builder.AppendLine();
            }
        }

        return builder.ToString().Replace("\r\n", "\n");
    }

    public static string PowerShell(string appName, IReadOnlyList<CommandNode> roots)
    {
        var nodes = CompletionData.Flatten(roots);
        var builder = new StringBuilder();

        builder.Append("# PowerShell completion for ").AppendLine(appName);
        builder.Append("Register-ArgumentCompleter -Native -CommandName '").Append(appName).AppendLine("' -ScriptBlock {");
        builder.AppendLine("    param($wordToComplete, $commandAst, $cursorPosition)");
        builder.AppendLine("    $words = $commandAst.CommandElements | ForEach-Object { $_.ToString() }");
        builder.AppendLine("    $key = ''");
        builder.AppendLine("    if ($words.Count -gt 2) {");
        builder.AppendLine("        $key = ($words[1..($words.Count - 2)] -join ' ')");
        builder.AppendLine("    }");
        builder.AppendLine("    $candidates = switch ($key) {");
        builder.Append("        '' { @(").Append(ToPowerShellArray(CompletionData.RootCandidates(roots))).AppendLine(") }");

        foreach (var node in nodes)
        {
            if (node.Children.Count == 0 && node.Model.Options.Count == 0)
            {
                continue;
            }

            builder.Append("        '").Append(PSQuote(node.Path)).Append("' { @(")
                .Append(ToPowerShellArray(CompletionData.Candidates(node, isRoot: false))).AppendLine(") }");
        }

        builder.AppendLine("        default { @() }");
        builder.AppendLine("    }");
        builder.AppendLine("    $candidates | Where-Object { $_ -like \"$wordToComplete*\" } | ForEach-Object {");
        builder.AppendLine("        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().Replace("\r\n", "\n");
    }

    private static string ToPowerShellArray(List<string> values)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append('\'').Append(PSQuote(values[i])).Append('\'');
        }

        return builder.ToString().Replace("\r\n", "\n");
    }

    private static string FunctionName(string appName)
    {
        var builder = new StringBuilder("_RunesHela_");

        foreach (var c in appName)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return builder.ToString().Replace("\r\n", "\n");
    }

    private static string LastSegment(string path)
    {
        var index = path.LastIndexOf(' ');
        return index < 0 ? path : path.Substring(index + 1);
    }

    private static string FishQuote(string value)
    {
        return value.Replace("\\", "\\\\").Replace("'", "\\'");
    }

    private static string PSQuote(string value)
    {
        return value.Replace("'", "''");
    }
}
