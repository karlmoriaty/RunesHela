using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RunesHela.Generator;

internal readonly struct LocationInfo : IEquatable<LocationInfo>
{
    public LocationInfo(
        string filePath,
        int spanStart,
        int spanLength,
        int startLine,
        int startCharacter,
        int endLine,
        int endCharacter)
    {
        FilePath = filePath;
        SpanStart = spanStart;
        SpanLength = spanLength;
        StartLine = startLine;
        StartCharacter = startCharacter;
        EndLine = endLine;
        EndCharacter = endCharacter;
    }

    public string FilePath { get; }

    public int SpanStart { get; }

    public int SpanLength { get; }

    public int StartLine { get; }

    public int StartCharacter { get; }

    public int EndLine { get; }

    public int EndCharacter { get; }

    public static LocationInfo None => default;

    public static LocationInfo From(Location? location)
    {
        if (location is null || location == Location.None)
        {
            return None;
        }

        var lineSpan = location.GetLineSpan();

        return new LocationInfo(
            location.SourceTree?.FilePath ?? string.Empty,
            location.SourceSpan.Start,
            location.SourceSpan.Length,
            lineSpan.StartLinePosition.Line,
            lineSpan.StartLinePosition.Character,
            lineSpan.EndLinePosition.Line,
            lineSpan.EndLinePosition.Character);
    }

    public Location ToLocation()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            return Location.None;
        }

        return Location.Create(
            FilePath,
            new TextSpan(SpanStart, SpanLength),
            new LinePositionSpan(
                new LinePosition(StartLine, StartCharacter),
                new LinePosition(EndLine, EndCharacter)));
    }

    public bool Equals(LocationInfo other)
    {
        return FilePath == other.FilePath &&
               SpanStart == other.SpanStart &&
               SpanLength == other.SpanLength &&
               StartLine == other.StartLine &&
               StartCharacter == other.StartCharacter &&
               EndLine == other.EndLine &&
               EndCharacter == other.EndCharacter;
    }

    public override bool Equals(object? obj)
    {
        return obj is LocationInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = FilePath?.GetHashCode() ?? 0;
            hash = (hash * 397) ^ SpanStart;
            hash = (hash * 397) ^ SpanLength;
            hash = (hash * 397) ^ StartLine;
            hash = (hash * 397) ^ StartCharacter;
            hash = (hash * 397) ^ EndLine;
            hash = (hash * 397) ^ EndCharacter;
            return hash;
        }
    }
}
