using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nexus.Core;
using System.Diagnostics;
using System.Text;

namespace Nexus.Benchmarks.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class LatencyBenchmarks
{
    private NexusServer? _server;
    private NexusClient? _client;
    private readonly byte[] _pingMessage = Encoding.UTF8.GetBytes("ping");
    private TaskCompletionSource<bool>? _responseReceived;
    private Stopwatch? _stopwatch;

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new NexusServer(19004);
        _server.OnMessageReceived += (connection, message) =>
        {
            // Echo back immediately with proper error handling
            try
            {
                var messageData = message.ToArray();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await connection.SendMessageAsync(messageData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error echoing message: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing received message: {ex.Message}");
            }
        };

        _ = _server.StartAsync();
        await Task.Delay(500); // Increased delay for server startup

        _client = new NexusClient();
        _client.OnMessageReceived += (message) =>
        {
            try
            {
                _stopwatch?.Stop();
                _responseReceived?.SetResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in client message handler: {ex.Message}");
                _responseReceived?.SetException(ex);
            }
        };

        await _client.ConnectAsync("127.0.0.1", 19004);
        await Task.Delay(200); // Allow connection to stabilize
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    [Benchmark]
    public async Task<TimeSpan> SinglePingLatency()
    {
        _responseReceived = new TaskCompletionSource<bool>();
        _stopwatch = Stopwatch.StartNew();
        
        await _client!.SendMessageAsync(_pingMessage);
        await _responseReceived.Task;
        
        return _stopwatch.Elapsed;
    }

    [Benchmark]
    public async Task<TimeSpan> SmallMessageLatency()
    {
        var message = Encoding.UTF8.GetBytes("Hello World! This is a small test message.");
        
        _responseReceived = new TaskCompletionSource<bool>();
        _stopwatch = Stopwatch.StartNew();
        
        await _client!.SendMessageAsync(message);
        await _responseReceived.Task;
        
        return _stopwatch.Elapsed;
    }

    [Benchmark]
    public async Task<double> MessagesPerSecond_Small()
    {
        const int messageCount = 1000;
        var tasks = new Task[messageCount];
        var startTime = Stopwatch.StartNew();
        
        for (int i = 0; i < messageCount; i++)
        {
            tasks[i] = _client!.SendMessageAsync(_pingMessage);
        }
        
        await Task.WhenAll(tasks);
        startTime.Stop();
        
        return messageCount / startTime.Elapsed.TotalSeconds;
    }

    [Benchmark]
    public async Task<double> MessagesPerSecond_Medium()
    {
        const int messageCount = 500;
        var mediumMessage = new byte[1024]; // 1KB
        var tasks = new Task[messageCount];
        var startTime = Stopwatch.StartNew();
        
        for (int i = 0; i < messageCount; i++)
        {
            tasks[i] = _client!.SendMessageAsync(mediumMessage);
        }
        
        await Task.WhenAll(tasks);
        startTime.Stop();
        
        return messageCount / startTime.Elapsed.TotalSeconds;
    }

    [Benchmark]
    public async Task<long> ThroughputBytesPerSecond()
    {
        const int messageCount = 100;
        const int messageSize = 8192; // 8KB
        var message = new byte[messageSize];
        var tasks = new Task[messageCount];
        var startTime = Stopwatch.StartNew();
        
        for (int i = 0; i < messageCount; i++)
        {
            tasks[i] = _client!.SendMessageAsync(message);
        }
        
        await Task.WhenAll(tasks);
        startTime.Stop();
        
        var totalBytes = messageCount * messageSize;
        return (long)(totalBytes / startTime.Elapsed.TotalSeconds);
    }
}
