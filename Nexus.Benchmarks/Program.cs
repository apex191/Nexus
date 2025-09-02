using BenchmarkDotNet.Running;
using Nexus.Benchmarks.Benchmarks;

Console.WriteLine("🚀 Nexus Performance Benchmarks");
Console.WriteLine("================================");

var switcher = new BenchmarkSwitcher(new[]
{
    typeof(BaselineBenchmark),
    typeof(SimpleBenchmark),
    typeof(LatencyBenchmarks),
    typeof(MessageThroughputBenchmarks),
    typeof(ConnectionBenchmarks),
    typeof(MemoryBenchmarks),
    typeof(ConcurrencyBenchmarks)
});

switcher.Run(args);
