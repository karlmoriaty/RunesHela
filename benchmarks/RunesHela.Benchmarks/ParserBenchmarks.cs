using BenchmarkDotNet.Attributes;
using RunesHela.Parsing;

namespace RunesHela.Benchmarks;

[MemoryDiagnoser]
public class ParserBenchmarks
{
    private static readonly string[] Args =
    [
        "--name", "runes",
        "--count", "42",
        "--verbose",
        "--tag", "alpha",
        "--output", "out.txt",
    ];

    [Benchmark(Baseline = true)]
    public int HandWritten()
    {
        ReadOnlyMemory<char> name = default;
        ReadOnlyMemory<char> tag = default;
        ReadOnlyMemory<char> output = default;
        var count = 0;
        var verbose = false;

        for (var i = 0; i < Args.Length; i++)
        {
            var argument = Args[i];

            switch (argument)
            {
                case "--name":
                    name = Args[++i].AsMemory();
                    break;
                case "--count":
                    int.TryParse(Args[++i].AsSpan(), out count);
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--tag":
                    tag = Args[++i].AsMemory();
                    break;
                case "--output":
                    output = Args[++i].AsMemory();
                    break;
            }
        }

        return count + name.Length + tag.Length + output.Length + (verbose ? 1 : 0);
    }

    [Benchmark]
    public int Reader()
    {
        ReadOnlyMemory<char> name = default;
        ReadOnlyMemory<char> tag = default;
        ReadOnlyMemory<char> output = default;
        var count = 0;
        var verbose = false;

        var reader = new CommandLineReader(Args);

        while (reader.TryRead(out var token))
        {
            if (token.Kind != CommandLineTokenKind.Option)
            {
                continue;
            }

            var option = token.Name;

            if (option.SequenceEqual("name"))
            {
                reader.TryReadValue(out name);
            }
            else if (option.SequenceEqual("count"))
            {
                if (reader.TryReadValue(out var raw))
                {
                    int.TryParse(raw.Span, out count);
                }
            }
            else if (option.SequenceEqual("verbose"))
            {
                verbose = true;
            }
            else if (option.SequenceEqual("tag"))
            {
                reader.TryReadValue(out tag);
            }
            else if (option.SequenceEqual("output"))
            {
                reader.TryReadValue(out output);
            }
            else
            {
                reader.TryReadValue(out _);
            }
        }

        return count + name.Length + tag.Length + output.Length + (verbose ? 1 : 0);
    }
}
