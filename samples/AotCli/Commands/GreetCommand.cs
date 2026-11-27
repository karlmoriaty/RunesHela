using RunesHela;

namespace AotCli.Commands;

/// <summary>
/// Greets a user.
/// </summary>
/// <example>greet Ada</example>
[Command("greet", Description = "Print an AOT-friendly greeting.")]
public sealed class GreetCommand
{
    /// <summary>
    /// Name to greet.
    /// </summary>
    [Argument(0, Description = "Name to greet.")]
    public string Name { get; init; } = "world";

    /// <summary>
    /// Greeting text.
    /// </summary>
    [Option(Short = 't', Description = "Greeting text.")]
    public string Text { get; init; } = "Hello";

    public void Run()
    {
        Console.WriteLine(Text + ", " + Name + " from Native AOT.");
    }
}
