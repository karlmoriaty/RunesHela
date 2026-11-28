using System.Diagnostics;
using RunesHela;

namespace RunesHela.Performance.Tests;

internal sealed class NullConsole : IConsole
{
    public static readonly NullConsole Instance = new();

    public TextWriter Out => TextWriter.Null;

    public TextWriter Error => TextWriter.Null;

    public bool IsOutputRedirected => true;
}

internal static class Measure
{
    public static long AllocatedBytesPerOperation(Action action, int iterations)
    {
        for (var i = 0; i < 1000; i++)
        {
            action();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < iterations; i++)
        {
            action();
        }

        return (GC.GetAllocatedBytesForCurrentThread() - before) / iterations;
    }

    public static double BestNanosecondsPerOperation(Action action, int iterations)
    {
        var warmup = Stopwatch.StartNew();

        for (var i = 0; i < 1_000_000 && warmup.ElapsedMilliseconds < 150; i++)
        {
            action();
        }

        var best = double.MaxValue;

        for (var round = 0; round < 10; round++)
        {
            var start = Stopwatch.GetTimestamp();

            for (var i = 0; i < iterations; i++)
            {
                action();
            }

            var elapsed = Stopwatch.GetTimestamp() - start;
            var nanoseconds = (double)elapsed / Stopwatch.Frequency * 1_000_000_000 / iterations;

            if (nanoseconds < best)
            {
                best = nanoseconds;
            }
        }

        return best;
    }
}
