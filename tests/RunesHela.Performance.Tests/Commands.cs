using RunesHela;

namespace RunesHela.Performance.Tests;

public enum PerfMode
{
    A,
    B,
    C,
}

[Command("perf-noop", Description = "Does nothing.")]
public sealed class PerfNoopCommand
{
    public int Run()
    {
        return 0;
    }
}

[Command("perf-bind", Description = "Binds five options.")]
public sealed class PerfBindCommand
{
    [Option(Description = "Count.")]
    public int Count { get; init; }

    [Option(Description = "Ratio.")]
    public double Ratio { get; init; }

    [Option(Description = "Flag.")]
    public bool Flag { get; init; }

    [Option(Description = "Mode.")]
    public PerfMode Mode { get; init; }

    [Option(Description = "Name.")]
    public string? Name { get; init; }

    public int Run()
    {
        return 0;
    }
}

[Command("perf-baseline", Description = "Same shape without options.")]
public sealed class PerfBaselineCommand
{
    public int Count { get; init; }

    public double Ratio { get; init; }

    public bool Flag { get; init; }

    public PerfMode Mode { get; init; }

    public string? Name { get; init; }

    public int Run()
    {
        return 0;
    }
}
