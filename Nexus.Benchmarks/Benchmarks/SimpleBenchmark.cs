using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nexus.Core;
using System.Text;

namespace Nexus.Benchmarks.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class SimpleBenchmark
{
    private NexusServer? _server;
    private NexusClient? _client;

    [GlobalSetup]
    public async Task Setup()
    {
        Console.WriteLine("Setting up simple benchmark...");
        
        _server = new NexusServer(19005);
        _server.OnClientConnected += (conn) => Console.WriteLine("Client connected");
        _server.OnMessageReceived += (connection, message) =>
        {
            Console.WriteLine($"Server received message of {message.Length} bytes");

            _ = Task.Run(async () =>
            {
                try
                {
                    await connection.SendMessageAsync(message.ToArray());
                    Console.WriteLine("Echo sent back");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Echo error: {ex.Message}");
                }
            });
        };

        _ = _server.StartAsync();
        await Task.Delay(1000);

        _client = new NexusClient();
        _client.OnMessageReceived += (message) =>
        {
            Console.WriteLine($"Client received response of {message.Length} bytes");
        };

        Console.WriteLine("Connecting client...");
        await _client.ConnectAsync("127.0.0.1", 19005);
        await Task.Delay(500);
        Console.WriteLine("Setup complete");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Console.WriteLine("Cleaning up...");
        _client?.Dispose();
        _server?.Dispose();
    }

    [Benchmark]
    public async Task SendSimpleMessage()
    {
        var message = Encoding.UTF8.GetBytes("Hello World!");
        await _client!.SendMessageAsync(message);
        await Task.Delay(10); // Small delay to allow processing
    }
}
