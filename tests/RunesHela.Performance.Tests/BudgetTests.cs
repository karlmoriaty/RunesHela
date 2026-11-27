using RunesHela;
using Xunit;

namespace RunesHela.Performance.Tests;

public sealed class BudgetTests
{
    private static readonly string[] BaselineArgs = ["perf-baseline"];

    private static readonly string[] FiveOptionsArgs =
    [
        "perf-bind",
        "--count", "42",
        "--ratio", "1.5",
        "--flag",
        "--mode", "b",
        "--name", "runes",
    ];

    private static readonly string[] HelpArgs = ["perf-bind", "--help"];
    private static readonly string[] ErrorArgs = ["unknown-command"];

    public BudgetTests()
    {
        RunesHelaConsole.Current = NullConsole.Instance;
    }

    [Fact]
    public void BindingAllocatesOnlyTheCommandInstance()
    {
        var baseline = Measure.AllocatedBytesPerOperation(() => RunesHelaApp.Run(BaselineArgs), 50_000);
        var binding = Measure.AllocatedBytesPerOperation(() => RunesHelaApp.Run(FiveOptionsArgs), 50_000);

        Assert.Equal(baseline, binding);
    }

    [Fact]
    public void HelpStaysWithinAllocationBudget()
    {
        var allocated = Measure.AllocatedBytesPerOperation(() => RunesHelaApp.Run(HelpArgs), 5_000);

        Assert.InRange(allocated, 0, 128);
    }

    [Fact]
    public void ErrorsStayWithinAllocationBudget()
    {
        var allocated = Measure.AllocatedBytesPerOperation(() => RunesHelaApp.Run(ErrorArgs), 5_000);

        Assert.InRange(allocated, 0, 1024);
    }

    [Fact]
    public void ErrorsReturnUsageExitCode()
    {
        Assert.Equal(2, RunesHelaApp.Run(ErrorArgs));
    }

#if !DEBUG
    [Fact]
    public void ParsingAndBindingStayWithinLatencyBudget()
    {
        var nanoseconds = Measure.BestNanosecondsPerOperation(() => RunesHelaApp.Run(FiveOptionsArgs), 50_000);

        Assert.True(nanoseconds <= 1100, nanoseconds.ToString("F1") + " ns");
    }

    [Fact]
    public void HelpStaysWithinLatencyBudget()
    {
        var nanoseconds = Measure.BestNanosecondsPerOperation(() => RunesHelaApp.Run(HelpArgs), 50_000);

        Assert.True(nanoseconds <= 5500, nanoseconds.ToString("F1") + " ns");
    }

    [Fact]
    public void ErrorsStayWithinLatencyBudget()
    {
        var nanoseconds = Measure.BestNanosecondsPerOperation(() => RunesHelaApp.Run(ErrorArgs), 50_000);

        Assert.True(nanoseconds <= 5500, nanoseconds.ToString("F1") + " ns");
    }
#endif
}
