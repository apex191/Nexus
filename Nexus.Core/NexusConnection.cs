using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace Nexus.Core;

/// <summary>
/// Represents a single, low-level connection that handles the pipeline-based read/write logic.
/// This is the internal workhorse of the library.
/// </summary>
public class NexusConnection : IDisposable
{
    private readonly Socket _socket;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    // Internal events for the server/client to subscribe to.
    internal event Action<NexusConnection, ReadOnlySequence<byte>>? OnMessageReceived;
    internal event Action<NexusConnection>? OnDisconnected;

    public NexusConnection(Socket socket)
    {
        _socket = socket;
    }

    /// <summary>
    /// Starts the read/write loops for this connection.
    /// </summary>
    public void Start()
    {
        // Fire-and-forget the processing task.
        _ = ProcessSocketAsync();
    }
    
    /// <summary>
    /// Stops the connection gracefully.
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _socket.Close();
    }

    /// <summary>
    /// Sends a message payload asynchronously. The payload should already include the length prefix.
    /// </summary>
    public async Task SendMessageAsync(ReadOnlyMemory<byte> prefixedMessage)
    {
        try
        {
            await _socket.SendAsync(prefixedMessage, SocketFlags.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            Stop();
        }
    }

    private async Task ProcessSocketAsync()
    {
        var pipe = new Pipe();
        var token = _cancellationTokenSource.Token;

        try
        {
            Task writing = FillPipeAsync(pipe.Writer, token);
            Task reading = ReadPipeAsync(pipe.Reader, token);

            await Task.WhenAll(reading, writing);
        }
        catch (Exception ex)
        {
            // Log exceptions that might occur during setup or teardown.
            Console.WriteLine($"An error occurred in the connection processing: {ex.Message}");
        }
        finally
        {
            OnDisconnected?.Invoke(this);
        }
    }

    private async Task FillPipeAsync(PipeWriter writer, CancellationToken token)
    {
        const int minimumBufferSize = 512;
        while (!token.IsCancellationRequested)
        {
            Memory<byte> memory = writer.GetMemory(minimumBufferSize);
            try
            {
                int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, token);
                if (bytesRead == 0)
                {
                    break; // Socket closed gracefully.
                }
                writer.Advance(bytesRead);
            }
            catch (OperationCanceledException)
            {
                break; // Shutdown was requested.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data: {ex.Message}");
                break;
            }

            FlushResult result = await writer.FlushAsync(token);
            if (result.IsCompleted)
            {
                break;
            }
        }
        await writer.CompleteAsync();
    }

    private async Task ReadPipeAsync(PipeReader reader, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            ReadResult result;
            try
            {
                result = await reader.ReadAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            
            ReadOnlySequence<byte> buffer = result.Buffer;

            while (TryReadMessage(ref buffer, out ReadOnlySequence<byte> message))
            {
                OnMessageReceived?.Invoke(this, message);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }
        await reader.CompleteAsync();
    }

    private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> message)
    {
        if (buffer.Length < 4)
        {
            message = default;
            return false;
        }

        // Using System.Buffers.Binary.BinaryPrimitives is faster than BitConverter.
        var lengthSpan = buffer.Slice(0, 4).IsSingleSegment ? buffer.FirstSpan.Slice(0, 4) : buffer.Slice(0, 4).ToArray();
        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(lengthSpan);
        
        // Malformed message check: protect against invalid length prefixes.
        if (payloadLength < 0 || payloadLength > 1_048_576) // e.g., 1MB limit
        {
             throw new InvalidOperationException($"Invalid message size: {payloadLength}. Must be between 0 and 1,048,576 bytes.");
        }

        if (buffer.Length < 4 + payloadLength)
        {
            message = default;
            return false;
        }
        
        message = buffer.Slice(4, payloadLength);
        buffer = buffer.Slice(4 + payloadLength);
        
        return true;
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
        _socket?.Dispose();
    }
}