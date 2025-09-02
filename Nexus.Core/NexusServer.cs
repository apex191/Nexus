using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Nexus.Core;

/// <summary>
/// A high-level TCP server for handling multiple client connections.
/// </summary>
public class NexusServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<Guid, NexusConnection> _connections = new();
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Fires when a new client connects to the server.
    /// </summary>
    public event Action<NexusConnection>? OnClientConnected;

    /// <summary>
    /// Fires when a client disconnects from the server.
    /// </summary>
    public event Action<NexusConnection>? OnClientDisconnected;
    
    /// <summary>
    /// Fires when a complete message is received from a client.
    /// </summary>
    public event Action<NexusConnection, ReadOnlySequence<byte>>? OnMessageReceived;
    
    public NexusServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }
    
    /// <summary>
    /// Starts the server and begins listening for new clients.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;
        
        _listener.Start();
        Console.WriteLine("Server started...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await _listener.AcceptSocketAsync(token);
                var connection = new NexusConnection(clientSocket);
                var connectionId = Guid.NewGuid();
                
                _connections[connectionId] = connection;
                
                connection.OnMessageReceived += (conn, message) => OnMessageReceived?.Invoke(conn, message);
                connection.OnDisconnected += (conn) => HandleClientDisconnection(conn, connectionId);

                OnClientConnected?.Invoke(connection);
                connection.Start();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
        _listener.Stop();
        Console.WriteLine("Server stopped.");
    }
    
    /// <summary>
    /// Stops the server.
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        

        foreach (var connection in _connections.Values)
        {
            connection.Stop();
        }
        _connections.Clear();
        
        _listener?.Stop();
    }
    
    private void HandleClientDisconnection(NexusConnection connection, Guid connectionId)
    {
        _connections.TryRemove(connectionId, out _);
        OnClientDisconnected?.Invoke(connection);
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
    }
}