using Nexus.Core;
using System.Text;

using var client = new NexusClient();

client.OnMessageReceived += (message) =>
{
    var text = Encoding.UTF8.GetString(message.ToArray());
    Console.WriteLine($"Server response: {text}");
};

client.OnDisconnected += () =>
{
    Console.WriteLine("Disconnected from server.");
};

try
{
    await client.ConnectAsync("127.0.0.1", 9000);
    Console.WriteLine("Connected to server. Type messages and press Enter. Type 'exit' to quit.");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to connect: {ex.Message}");
    return;
}

// Main loop to send messages.
while (true)
{
    var line = Console.ReadLine();
    if (string.IsNullOrEmpty(line)) continue;
    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        await client.SendMessageAsync(Encoding.UTF8.GetBytes(line));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send message: {ex.Message}");
        break;
    }
}