using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RunesHela.Generator;

internal static class DocumentationReader
{
    public static string? GetSummary(ISymbol symbol, CancellationToken cancellationToken)
    {
        var xml = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        var summary = ExtractSection(xml!, "summary");

        if (summary is null)
        {
            return null;
        }

        summary = CollapseWhitespace(summary);
        return summary.Length == 0 ? null : summary;
    }

    public static string[] GetExamples(ISymbol symbol, CancellationToken cancellationToken)
    {
        var xml = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(xml))
        {
            return Array.Empty<string>();
        }

        var examples = new List<string>();
        var searchStart = 0;

        while (true)
        {
            var section = ExtractSection(xml!, "example", searchStart, out var sectionStart);

            if (section is null)
            {
                break;
            }

            var normalized = NormalizeExample(section);

            if (normalized.Length > 0)
            {
                examples.Add(normalized);
            }

            searchStart = sectionStart;
        }

        return examples.ToArray();
    }

    private static string NormalizeExample(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var builder = new StringBuilder(text.Length);
        var started = false;

        foreach (var line in lines)
        {
            var trimmed = StripTags(line).Trim();

            if (trimmed.Length == 0)
            {
                if (started)
                {
                    builder.Append('\n');
                }

                continue;
            }

            if (started)
            {
                builder.Append('\n');
            }

            builder.Append(trimmed);
            started = true;
        }

        return builder.ToString().Trim();
    }

    private static string CollapseWhitespace(string text)
    {
        var builder = new StringBuilder(text.Length);
        var lastWasSpace = false;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            builder.Append(c);
            lastWasSpace = false;
        }

        return builder.ToString().Trim();
    }

    private static string? ExtractSection(string xml, string name)
    {
        return ExtractSection(xml, name, 0, out _);
    }

    private static string? ExtractSection(string xml, string name, int searchStart, out int sectionStart)
    {
        sectionStart = searchStart;
        var openTag = "<" + name + ">";
        var closeTag = "</" + name + ">";

        var start = xml.IndexOf(openTag, searchStart, StringComparison.Ordinal);

        if (start < 0)
        {
            return null;
        }

        start += openTag.Length;
        var end = xml.IndexOf(closeTag, start, StringComparison.Ordinal);

        if (end < 0)
        {
            return null;
        }

        sectionStart = end + closeTag.Length;
        return xml.Substring(start, end - start);
    }

    private static string StripTags(string text)
    {
        var builder = new StringBuilder(text.Length);
        var insideTag = false;

        foreach (var c in text)
        {
            if (c == '<')
            {
                insideTag = true;
            }
            else if (c == '>')
            {
                insideTag = false;
            }
            else if (!insideTag)
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
