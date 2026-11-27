using BenchmarkDotNet.Attributes;

namespace RunesHela.Benchmarks;

[MemoryDiagnoser]
public class FrameworkBenchmarks
{
    private static readonly string[] HelpArgs = ["--help"];
    private static readonly string[] ErrorArgs = ["--nope"];
    private static readonly string[] RouteArgs = ["cmd5"];

    [GlobalSetup]
    public void Setup()
    {
        RunesHelaConsole.Current = NullConsole.Instance;
    }

    [Benchmark]
    public int Help()
    {
        return RunesHelaApp.Run(HelpArgs);
    }

    [Benchmark]
    public int Error()
    {
        return RunesHelaApp.Run(ErrorArgs);
    }

    [Benchmark]
    public int Route()
    {
        return RunesHelaApp.Run(RouteArgs);
    }
}
