using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nexus.Core;
using System.Collections.Concurrent;
using System.Text;

namespace Nexus.Benchmarks.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ConcurrencyBenchmarks
{
    private NexusServer? _server;
    private readonly byte[] _testMessage = Encoding.UTF8.GetBytes("Concurrency test message");
    private readonly ConcurrentBag<int> _receivedMessages = new();

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new NexusServer(19003);
        _server.OnMessageReceived += (connection, message) =>
        {
            _receivedMessages.Add(1);
            // Echo the message back
            _ = connection.SendMessageAsync(message.ToArray());
        };

        _ = _server.StartAsync();
        await Task.Delay(100);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _server?.Dispose();
    }

    [Benchmark]
    [Arguments(5, 100)]
    [Arguments(10, 100)]
    [Arguments(25, 50)]
    public async Task MultipleClientsMessaging(int clientCount, int messagesPerClient)
    {
        _receivedMessages.Clear();
        var clients = new NexusClient[clientCount];
        var clientTasks = new Task[clientCount];

        // Setup clients
        for (int i = 0; i < clientCount; i++)
        {
            clients[i] = new NexusClient();
            await clients[i].ConnectAsync("127.0.0.1", 19003);
        }

        // Send messages concurrently from all clients
        for (int i = 0; i < clientCount; i++)
        {
            var client = clients[i];
            clientTasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < messagesPerClient; j++)
                {
                    await client.SendMessageAsync(_testMessage);
                }
            });
        }

        await Task.WhenAll(clientTasks);

        // Wait for all messages to be processed
        var expectedMessages = clientCount * messagesPerClient;
        while (_receivedMessages.Count < expectedMessages)
        {
            await Task.Delay(1);
        }

        // Cleanup clients
        foreach (var client in clients)
        {
            client.Dispose();
        }
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    public async Task HighFrequencyMessaging(int messageCount)
    {
        _receivedMessages.Clear();
        using var client = new NexusClient();
        await client.ConnectAsync("127.0.0.1", 19003);

        // Send messages as fast as possible
        var tasks = new Task[messageCount];
        for (int i = 0; i < messageCount; i++)
        {
            tasks[i] = client.SendMessageAsync(_testMessage);
        }

        await Task.WhenAll(tasks);

        // Wait for all messages to be processed
        while (_receivedMessages.Count < messageCount)
        {
            await Task.Delay(1);
        }
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task ConnectionChurn(int iterations)
    {
        // Repeatedly connect and disconnect to test resource cleanup
        for (int i = 0; i < iterations; i++)
        {
            using var client = new NexusClient();
            await client.ConnectAsync("127.0.0.1", 19003);
            await client.SendMessageAsync(_testMessage);
            await Task.Delay(1); // Brief pause
        }
    }
}
