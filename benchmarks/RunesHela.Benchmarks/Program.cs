using BenchmarkDotNet.Running;
using RunesHela.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(ParserBenchmarks).Assembly).Run(args);
