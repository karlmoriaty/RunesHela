using RunesHela;

namespace MinimalCli.Commands;

/// <summary>
/// Manages remotes.
/// </summary>
[Command("remote", Description = "Manage remotes.")]
public sealed class RemoteCommand
{
    /// <summary>
    /// Adds a remote.
    /// </summary>
    [Command("add", Description = "Add a remote.")]
    public sealed class AddCommand
    {
        public void Run()
        {
            Console.WriteLine("remote add");
        }
    }

    /// <summary>
    /// Lists remotes.
    /// </summary>
    [Command("list", Description = "List remotes.")]
    public sealed class ListCommand
    {
        public int Run()
        {
            Console.WriteLine("remote list");
            return 0;
        }
    }
}
