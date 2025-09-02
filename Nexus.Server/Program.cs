using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Any, 9000);
listener.Start();
Console.WriteLine("Server started on port 9000...");

using var client = await listener.AcceptTcpClientAsync();
Console.WriteLine("Client connected!");

using var stream = client.GetStream();
var buffer = new byte[4096]; // A buffer to read data into.

while (true)
{
    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
    if (bytesRead == 0) // The client has disconnected.
    {
        Console.WriteLine("Client disconnected.");
        break;
    }
    
    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    Console.WriteLine($"Received: {message}");

    await stream.WriteAsync(buffer, 0, bytesRead);
    Console.WriteLine($"Echoed: {message}");
}