using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nexus.Core;

namespace Nexus.Benchmarks.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ConnectionBenchmarks
{
    private NexusServer? _server;

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new NexusServer(19002);
        _ = _server.StartAsync();
        await Task.Delay(100); // Give server time to start
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _server?.Dispose();
    }

    [Benchmark]
    public async Task SingleConnectionSetup()
    {
        using var client = new NexusClient();
        await client.ConnectAsync("127.0.0.1", 19002);
        client.Disconnect();
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public async Task MultipleConnectionsSequential(int connectionCount)
    {
        var tasks = new Task[connectionCount];
        
        for (int i = 0; i < connectionCount; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using var client = new NexusClient();
                await client.ConnectAsync("127.0.0.1", 19002);
                await Task.Delay(10); // Simulate some work
                client.Disconnect();
            });
        }
        
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(25)]
    [Arguments(50)]
    public async Task ConcurrentConnections(int connectionCount)
    {
        var clients = new NexusClient[connectionCount];
        var connectTasks = new Task[connectionCount];

        // Connect all clients concurrently
        for (int i = 0; i < connectionCount; i++)
        {
            clients[i] = new NexusClient();
            connectTasks[i] = clients[i].ConnectAsync("127.0.0.1", 19002);
        }

        await Task.WhenAll(connectTasks);

        // Disconnect all clients
        for (int i = 0; i < connectionCount; i++)
        {
            clients[i].Dispose();
        }
    }
}
