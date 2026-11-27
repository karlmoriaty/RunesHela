using BenchmarkDotNet.Attributes;

namespace RunesHela.Benchmarks;

[MemoryDiagnoser]
public class BindingBenchmarks
{
    private static readonly string[] FiveOptionsArgs =
    [
        "bind5", "prod",
        "--count", "42",
        "--ratio", "1.5",
        "--verbose",
        "--level", "high",
        "--name", "runes",
    ];

    private static readonly string[] NoOptionsArgs = ["cmd0"];

    [GlobalSetup]
    public void Setup()
    {
        RunesHelaConsole.Current = NullConsole.Instance;
    }

    [Benchmark]
    public int FiveOptions()
    {
        return RunesHelaApp.Run(FiveOptionsArgs);
    }

    [Benchmark]
    public int NoOptions()
    {
        return RunesHelaApp.Run(NoOptionsArgs);
    }
}
