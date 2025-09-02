using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nexus.Core;
using System.Buffers;
using System.Text;

namespace Nexus.Benchmarks.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class MemoryBenchmarks
{
    private readonly byte[] _testData = Encoding.UTF8.GetBytes("Test message for memory allocation benchmarks");

    [Benchmark(Baseline = true)]
    public byte[] ToArrayBaseline()
    {
        var sequence = new ReadOnlySequence<byte>(_testData);
        return sequence.ToArray();
    }

    [Benchmark]
    public byte[] ToArrayExtension()
    {
        var sequence = new ReadOnlySequence<byte>(_testData);
        return sequence.ToArray(); // Uses our extension method
    }

    [Benchmark]
    public void MessageFramingAllocation()
    {
        // Simulates the message framing process
        var payload = _testData;
        var message = new byte[4 + payload.Length];
        
        // Write length prefix
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(message.AsSpan(0, 4), payload.Length);
        
        // Copy payload
        payload.CopyTo(message, 4);
    }

    [Benchmark]
    public void MessageFramingWithSpan()
    {
        // More efficient version using Span<T>
        var payload = _testData.AsSpan();
        Span<byte> message = stackalloc byte[4 + payload.Length];
        
        // Write length prefix
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(message[..4], payload.Length);
        
        // Copy payload
        payload.CopyTo(message[4..]);
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    [Arguments(10000)]
    public void RepeatedAllocations(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var sequence = new ReadOnlySequence<byte>(_testData);
            var result = sequence.ToArray();
            
            // Simulate some processing
            _ = result.Length;
        }
    }
}
