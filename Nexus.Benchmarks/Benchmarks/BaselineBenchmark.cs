using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nexus.Core;
using System.Text;

namespace Nexus.Benchmarks.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class BaselineBenchmark
{
    private NexusServer? _server;
    private NexusClient? _client;
    private readonly byte[] _testMessage = Encoding.UTF8.GetBytes("ping");
    private volatile bool _responseReceived;

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new NexusServer(19010);
        
        // Simple server that just receives messages (no echo to avoid feedback loops)
        _server.OnMessageReceived += (connection, message) =>
        {
            // Just mark as received - no echo back to avoid the corruption issue
            _responseReceived = true;
        };

        _ = _server.StartAsync();
        await Task.Delay(300);

        _client = new NexusClient();
        await _client.ConnectAsync("127.0.0.1", 19010);
        await Task.Delay(100);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    [Benchmark]
    public async Task SendSingleMessage()
    {
        _responseReceived = false;
        await _client!.SendMessageAsync(_testMessage);
        
        // Simple delay instead of waiting for response to avoid complexity
        await Task.Delay(1);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    public async Task SendMultipleMessages(int messageCount)
    {
        var tasks = new Task[messageCount];
        
        for (int i = 0; i < messageCount; i++)
        {
            tasks[i] = _client!.SendMessageAsync(_testMessage);
        }
        
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task SendVariousMessageSizes()
    {
        var messages = new[]
        {
            Encoding.UTF8.GetBytes("small"),
            Encoding.UTF8.GetBytes(new string('A', 1024)), // 1KB
            Encoding.UTF8.GetBytes(new string('B', 4096))  // 4KB
        };

        foreach (var message in messages)
        {
            await _client!.SendMessageAsync(message);
        }
    }
}
