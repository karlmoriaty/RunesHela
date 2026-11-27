using System.Runtime.InteropServices;

namespace RunesHela.Parsing;

/// <summary>
/// Converts sliced command-line text back to strings without copying when the slice covers a whole argument.
/// </summary>
public static class SpanText
{
    /// <summary>
    /// Returns the string represented by the memory, reusing the original argument instance when possible.
    /// </summary>
    /// <param name="value">The memory to convert.</param>
    /// <returns>The resulting string.</returns>
    public static string ToString(ReadOnlyMemory<char> value)
    {
        if (MemoryMarshal.TryGetString(value, out var text, out var start, out var length))
        {
            return start == 0 && length == text.Length ? text : text.Substring(start, length);
        }

        return value.ToString();
    }
}
