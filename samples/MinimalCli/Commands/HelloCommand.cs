using RunesHela;

namespace MinimalCli.Commands;

/// <summary>
/// Prints a friendly greeting.
/// </summary>
[Command("hello", Description = "Say hello.")]
public sealed class HelloCommand
{
    public int Run()
    {
        Console.WriteLine("Hello from RunesHela.");
        return 0;
    }
}
