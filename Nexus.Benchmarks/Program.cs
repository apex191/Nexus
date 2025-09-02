using BenchmarkDotNet.Running;
using Nexus.Benchmarks.Benchmarks;

Console.WriteLine("🚀 Nexus Performance Benchmarks");
Console.WriteLine("================================");

var switcher = new BenchmarkSwitcher(new[]
{
    typeof(BaselineBenchmark),           // BASELINE: Simple, working performance test
    typeof(SimpleBenchmark),             // SIMPLE: Basic functionality test
    typeof(LatencyBenchmarks),           // NEW: Ultra-low latency focused (currently broken)
    typeof(MessageThroughputBenchmarks),
    typeof(ConnectionBenchmarks),
    typeof(MemoryBenchmarks),
    typeof(ConcurrencyBenchmarks)
});

switcher.Run(args);
