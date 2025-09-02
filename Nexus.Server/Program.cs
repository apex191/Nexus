using Nexus.Core;
using System.Text;


using var server = new NexusServer(9000);

server.OnClientConnected += (connection) =>
{
    Console.WriteLine("A client has connected.");
};

server.OnClientDisconnected += (connection) =>
{
    Console.WriteLine("A client has disconnected.");
};

server.OnMessageReceived += (connection, message) =>
{
    try
    {
        var text = Encoding.UTF8.GetString(message.ToArray());
        Console.WriteLine($"Received message: {text}");
        
        var reply = Encoding.UTF8.GetBytes($"Server acknowledges: {text}");
        var prefixedReply = new byte[4 + reply.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(prefixedReply, reply.Length);
        reply.CopyTo(prefixedReply, 4);
        
        _ = connection.SendMessageAsync(prefixedReply);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing message: {ex.Message}");
    }
};


var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    Console.WriteLine("Stopping server...");
    cts.Cancel();
    e.Cancel = true;
};

try
{
    await server.StartAsync(cts.Token);
}
catch (Exception ex)
{
    Console.WriteLine($"Server error: {ex.Message}");
}