using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;

namespace Nexus.Core;

/// <summary>
/// A high-level TCP client for connecting to a NexusServer.
/// </summary>
public class NexusClient : IDisposable
{
    private NexusConnection? _connection;
    private TcpClient? _tcpClient;

    /// <summary>
    /// Fires when a complete message is received from the server.
    /// </summary>
    public event Action<ReadOnlySequence<byte>>? OnMessageReceived;
    
    /// <summary>
    /// Fires when the client disconnects from the server.
    /// </summary>
    public event Action? OnDisconnected;
    
    /// <summary>
    /// Connects to the server.
    /// </summary>
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port, cancellationToken);
        
        _connection = new NexusConnection(_tcpClient.Client);
        
        // Subscribe to events.
        _connection.OnMessageReceived += (conn, message) => OnMessageReceived?.Invoke(message);
        _connection.OnDisconnected += (conn) => OnDisconnected?.Invoke();
        
        _connection.Start();
    }
    
    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    public void Disconnect()
    {
        _connection?.Stop();
        _tcpClient?.Close();
        _tcpClient?.Dispose();
        _tcpClient = null;
        _connection = null;
    }
    
    /// <summary>
    /// Sends a message to the server. The length prefix will be added automatically.
    /// </summary>
    public async Task SendMessageAsync(byte[] payload)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }
        
        // Create the full message with the 4-byte length prefix.
        var message = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(message.AsSpan(0, 4), payload.Length);
        payload.CopyTo(message, 4);
        
        await _connection.SendMessageAsync(message);
    }

    public void Dispose()
    {
        Disconnect();
    }
}