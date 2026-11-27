using RunesHela;

namespace MinimalCli.Commands;

/// <summary>
/// Shows the current runtime version.
/// </summary>
[Command("info", Description = "Show runtime information.")]
public static class InfoCommand
{
    public static Task RunAsync()
    {
        Console.WriteLine($"RunesHela {RunesHelaApp.GetVersion()}");
        return Task.CompletedTask;
    }
}
