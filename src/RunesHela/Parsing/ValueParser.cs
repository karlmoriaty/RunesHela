using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace RunesHela.Parsing;

/// <summary>
/// Culture-invariant value parsing used by generated command binders.
/// </summary>
public static class ValueParser
{
    /// <summary>
    /// Parses a value using <see cref="ISpanParsable{TSelf}"/> without allocating.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns><see langword="true"/> when parsing succeeded.</returns>
    public static bool TryParse<T>(ReadOnlySpan<char> text, [MaybeNullWhen(false)] out T value)
        where T : ISpanParsable<T>
    {
        return T.TryParse(text, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Parses a value using <see cref="IParsable{TSelf}"/> for types without span support.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns><see langword="true"/> when parsing succeeded.</returns>
    public static bool TryParseString<T>(string text, [MaybeNullWhen(false)] out T value)
        where T : IParsable<T>
    {
        return T.TryParse(text, CultureInfo.InvariantCulture, out value);
    }
}
