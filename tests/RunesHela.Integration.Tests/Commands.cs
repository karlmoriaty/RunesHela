using RunesHela;

namespace RunesHela.Integration.Tests;

public enum Mode
{
    Fast,
    Safe,
}

public interface IGreeter
{
    string Greet(string name);
}

/// <summary>
/// Greets a person.
/// </summary>
/// <example>greet Ada --count 2 --loud</example>
[Command("greet", Aliases = new[] { "hi" }, Description = "Greets a person.")]
public sealed class GreetCommand
{
    /// <summary>
    /// Person to greet.
    /// </summary>
    [Argument(0, Description = "Person to greet.")]
    public string Name { get; init; } = "world";

    /// <summary>
    /// Number of greetings.
    /// </summary>
    [Option(Short = 'c', Description = "Number of greetings.", Env = "RHL_GREET_COUNT")]
    [Range(1, 5)]
    public int Count { get; init; } = 1;

    /// <summary>
    /// Uppercase the greeting.
    /// </summary>
    [Option(Description = "Uppercase the greeting.")]
    public bool Loud { get; init; }

    /// <summary>
    /// Greeting mode.
    /// </summary>
    [Option(Description = "Greeting mode.")]
    public Mode Mode { get; init; } = Mode.Fast;

    /// <summary>
    /// Tags to print.
    /// </summary>
    [Option(Short = 't', Description = "Tags to print.")]
    public string[] Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// API token.
    /// </summary>
    [Option(Description = "API token.", Env = "RHL_TOKEN")]
    [Secret]
    public string? Token { get; init; }

    /// <summary>
    /// Secret numeric code.
    /// </summary>
    [Option(Description = "Secret numeric code.")]
    [Secret]
    public int? Pin { get; init; }

    public void Run()
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

        if (Mode == Mode.Safe)
        {
            Console.WriteLine("mode=safe");
        }

        if (Tags.Length > 0)
        {
            Console.WriteLine("tags=" + string.Join(",", Tags));
        }

        if (Token is not null)
        {
            Console.WriteLine("token-length=" + Token.Length);
        }
    }
}

[Command("deploy", Description = "Deploys a target.")]
public sealed class DeployCommand
{
    /// <summary>
    /// Deployment target.
    /// </summary>
    [Argument(0, Description = "Deployment target.")]
    public string Target { get; init; } = "all";

    /// <summary>
    /// Target environment.
    /// </summary>
    [Option(Short = 'e', Description = "Target environment.")]
    public string Environment { get; init; }

    public int Run()
    {
        Console.WriteLine("deploy " + Target + " -> " + Environment);
        return 0;
    }
}

[Command("copy", Description = "Copies a file.")]
public sealed class CopyCommand
{
    /// <summary>
    /// Source path.
    /// </summary>
    [Argument(0, Description = "Source path.")]
    public string Source { get; init; }

    /// <summary>
    /// Destination path.
    /// </summary>
    [Argument(1, Description = "Destination path.")]
    public string Destination { get; init; }

    public int Run()
    {
        Console.WriteLine("copy " + Source + " -> " + Destination);
        return 0;
    }
}

[Command("sum", Description = "Sums numbers.")]
public sealed class SumCommand
{
    /// <summary>
    /// Numbers to sum.
    /// </summary>
    [Argument(0, Description = "Numbers to sum.")]
    public int[] Numbers { get; init; } = Array.Empty<int>();

    public int Run()
    {
        Console.WriteLine("sum=" + Numbers.Sum());
        return 0;
    }
}

[Command("parent", Description = "Parent command.")]
public sealed class ParentCommand
{
    [Command("child", Description = "Child command.")]
    public sealed class ChildCommand
    {
        /// <summary>
        /// Leaf command.
        /// </summary>
        /// <example>parent child leaf --value 3</example>
        [Command("leaf", Description = "Leaf command.")]
        public sealed class LeafCommand
        {
            /// <summary>
            /// Value to echo.
            /// </summary>
            [Option(Description = "Value to echo.")]
            public int Value { get; init; } = 7;

            public int Run()
            {
                Console.WriteLine("leaf=" + Value);
                return 0;
            }
        }
    }
}

[Command("fail", Description = "Returns a custom exit code.")]
public sealed class FailCommand
{
    public int Run()
    {
        return 7;
    }
}

[Command("count", Description = "Counts asynchronously.")]
public sealed class CountCommand
{
    public async ValueTask<int> RunAsync()
    {
        await Task.Yield();
        Console.WriteLine("counted");
        return 4;
    }
}

[Command("pending", Description = "Declares no handler.")]
public sealed class PendingCommand
{
}

[Command("di", Description = "Uses constructor injection.")]
public sealed class DiCommand
{
    private readonly IGreeter _greeter;

    public DiCommand(IGreeter greeter)
    {
        _greeter = greeter;
    }

    public void Run()
    {
        Console.WriteLine(_greeter.Greet("di"));
    }
}
