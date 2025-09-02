using System.Net.Sockets;
using System.Text;

using var client = new TcpClient();
await client.ConnectAsync("127.0.0.1", 9000);
Console.WriteLine("Connected to server!");

using var stream = client.GetStream();

_ = Task.Run(async () =>
{
    var buffer = new byte[4096];
    while (true)
    {
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0) break;
        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Server response: {message}");
    }
});


while (true)
{
    Console.Write("Enter message to send: ");
    var line = Console.ReadLine();
    if (string.IsNullOrEmpty(line)) continue;
    
    var messageBytes = Encoding.UTF8.GetBytes(line);
    await stream.WriteAsync(messageBytes);
}