using RunesHela;

namespace MinimalCli.Commands;

/// <summary>
/// Prints a friendly greeting.
/// </summary>
/// <example>hello --name Ada --count 2</example>
[Command("hello", Description = "Say hello.")]
public sealed class HelloCommand
{
    /// <summary>
    /// Name to greet.
    /// </summary>
    [Option(Short = 'n', Description = "Name to greet.", Env = "MINIMALCLI_NAME")]
    public string Name { get; init; } = "world";

    /// <summary>
    /// Number of greetings.
    /// </summary>
    [Option(Short = 'c', Description = "Number of greetings.")]
    [Range(1, 10)]
    public int Count { get; init; } = 1;

    /// <summary>
    /// Uppercase the greeting.
    /// </summary>
    [Option(Description = "Uppercase the greeting.")]
    public bool Loud { get; init; }

    public int Run()
    {
        var text = "Hello, " + Name + "!";

        if (Loud)
        {
            text = text.ToUpperInvariant();
        }

        for (var i = 0; i < Count; i++)
        {
            Console.WriteLine(text);
        }

        return 0;
    }
}
