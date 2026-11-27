using RunesHela;

namespace AotCli.Commands;

/// <summary>
/// Greets a fixed user. Parameter binding arrives after M0.
/// </summary>
[Command("greet", Description = "Print an AOT-friendly greeting.")]
public sealed class GreetCommand
{
    public void Run()
    {
        Console.WriteLine("Hello from Native AOT.");
    }
}
