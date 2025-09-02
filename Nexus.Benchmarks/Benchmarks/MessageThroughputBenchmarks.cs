using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nexus.Core;
using System.Text;

namespace Nexus.Benchmarks.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class MessageThroughputBenchmarks
{
    private NexusServer? _server;
    private NexusClient? _client;
    private readonly byte[] _smallMessage = Encoding.UTF8.GetBytes("Hello World!");
    private readonly byte[] _mediumMessage = Encoding.UTF8.GetBytes(new string('A', 1024)); // 1KB
    private readonly byte[] _largeMessage = Encoding.UTF8.GetBytes(new string('B', 65536)); // 64KB
    private int _messagesReceived;

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new NexusServer(19001);
        _server.OnMessageReceived += (connection, message) =>
        {
            Interlocked.Increment(ref _messagesReceived);
            // Echo back the message
            _ = connection.SendMessageAsync(message.ToArray());
        };

        _ = _server.StartAsync();
        await Task.Delay(100); // Give server time to start

        _client = new NexusClient();
        await _client.ConnectAsync("127.0.0.1", 19001);
        await Task.Delay(100); // Give connection time to establish
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    [Arguments(10000)]
    public async Task SmallMessages(int messageCount)
    {
        _messagesReceived = 0;
        var tasks = new Task[messageCount];
        
        for (int i = 0; i < messageCount; i++)
        {
            tasks[i] = _client!.SendMessageAsync(_smallMessage);
        }
        
        await Task.WhenAll(tasks);
        
        // Wait for all messages to be processed
        while (_messagesReceived < messageCount)
        {
            await Task.Delay(1);
        }
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task MediumMessages(int messageCount)
    {
        _messagesReceived = 0;
        var tasks = new Task[messageCount];
        
        for (int i = 0; i < messageCount; i++)
        {
            tasks[i] = _client!.SendMessageAsync(_mediumMessage);
        }
        
        await Task.WhenAll(tasks);
        
        while (_messagesReceived < messageCount)
        {
            await Task.Delay(1);
        }
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    public async Task LargeMessages(int messageCount)
    {
        _messagesReceived = 0;
        var tasks = new Task[messageCount];
        
        for (int i = 0; i < messageCount; i++)
        {
            tasks[i] = _client!.SendMessageAsync(_largeMessage);
        }
        
        await Task.WhenAll(tasks);
        
        while (_messagesReceived < messageCount)
        {
            await Task.Delay(1);
        }
    }
}
