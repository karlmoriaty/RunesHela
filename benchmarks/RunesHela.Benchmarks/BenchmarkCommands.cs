namespace RunesHela.Benchmarks.Commands;

public enum BenchLevel
{
    Low,
    Medium,
    High,
}

[Command("bind5")]
public sealed class Bind5Command
{
    [Option(Short = 'c', Description = "Count.")]
    public int Count { get; init; }

    [Option(Short = 'r', Description = "Ratio.")]
    public double Ratio { get; init; }

    [Option(Short = 'v', Description = "Verbose.")]
    public bool Verbose { get; init; }

    [Option(Short = 'l', Description = "Level.")]
    public BenchLevel Level { get; init; }

    [Option(Short = 'n', Description = "Name.")]
    public string? Name { get; init; }

    [Argument(0, Description = "Target.")]
    public string Target { get; init; } = "all";

    public int Run()
    {
        return 0;
    }
}

[Command("cmd0")]
public sealed class Cmd0
{
    public void Run()
    {
    }
}

[Command("cmd1")]
public sealed class Cmd1
{
    public void Run()
    {
    }
}

[Command("cmd2")]
public sealed class Cmd2
{
    public void Run()
    {
    }
}

[Command("cmd3")]
public sealed class Cmd3
{
    public void Run()
    {
    }
}

[Command("cmd4")]
public sealed class Cmd4
{
    public void Run()
    {
    }
}

[Command("cmd5")]
public sealed class Cmd5
{
    public void Run()
    {
    }
}

[Command("cmd6")]
public sealed class Cmd6
{
    public void Run()
    {
    }
}

[Command("cmd7")]
public sealed class Cmd7
{
    public void Run()
    {
    }
}

[Command("cmd8")]
public sealed class Cmd8
{
    public void Run()
    {
    }
}

[Command("cmd9")]
public sealed class Cmd9
{
    public void Run()
    {
    }
}
