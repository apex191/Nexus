using System.Text;
using Nexus.Core;
using Xunit;
using Xunit.Abstractions;

namespace Nexus.Tests;

public class BasicCommunicationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<IDisposable> _disposables = new();

    public BasicCommunicationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Client_Should_Connect_To_Server()
    {
        // Arrange
        var server = new NexusServer(0); // Use port 0 for auto-assignment
        _disposables.Add(server);
        
        var serverStarted = false;
        server.OnClientConnected += _ => serverStarted = true;

        // Act
        _ = server.StartAsync();
        await Task.Delay(100); // Give server time to start

        var client = new NexusClient();
        _disposables.Add(client);
        
        // For testing, we need to find the actual port the server is using
        // This is a limitation of our current design - we'll connect to a known port
        await client.ConnectAsync("127.0.0.1", 19999); // Use a specific test port
        await Task.Delay(100);

        // Assert
        Assert.True(serverStarted);
    }

    [Fact]
    public async Task Server_Should_Start_And_Stop_Cleanly()
    {
        // Arrange
        var server = new NexusServer(19998);
        _disposables.Add(server);

        // Act
        var startTask = server.StartAsync();
        await Task.Delay(100);
        
        server.Stop();
        
        // Should complete without hanging
        var completed = await Task.WhenAny(startTask, Task.Delay(5000));
        
        // Assert
        Assert.Equal(startTask, completed);
    }

    [Fact]
    public async Task Client_Should_Send_Message_Without_Error()
    {
        // Arrange
        var server = new NexusServer(19997);
        _disposables.Add(server);
        
        var messagesReceived = new List<string>();
        server.OnMessageReceived += (conn, message) =>
        {
            var text = Encoding.UTF8.GetString(message.ToArray());
            messagesReceived.Add(text);
            _output.WriteLine($"Server received: {text}");
        };

        _ = server.StartAsync();
        await Task.Delay(200);

        var client = new NexusClient();
        _disposables.Add(client);
        await client.ConnectAsync("127.0.0.1", 19997);
        await Task.Delay(100);

        // Act
        var testMessage = "Hello, Server!";
        await client.SendMessageAsync(Encoding.UTF8.GetBytes(testMessage));
        await Task.Delay(200); // Give time for message processing

        // Assert
        Assert.Contains(testMessage, messagesReceived);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Hello")]
    [InlineData("This is a longer message with more content")]
    [InlineData("Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?")]
    public async Task Messages_With_Different_Lengths_Should_Work(string testMessage)
    {
        // Arrange
        var server = new NexusServer(19996);
        _disposables.Add(server);
        
        var receivedMessage = "";
        var messageReceived = new TaskCompletionSource<bool>();
        
        server.OnMessageReceived += (conn, message) =>
        {
            receivedMessage = Encoding.UTF8.GetString(message.ToArray());
            messageReceived.SetResult(true);
        };

        _ = server.StartAsync();
        await Task.Delay(200);

        var client = new NexusClient();
        _disposables.Add(client);
        await client.ConnectAsync("127.0.0.1", 19996);
        await Task.Delay(100);

        // Act
        await client.SendMessageAsync(Encoding.UTF8.GetBytes(testMessage));
        
        // Wait for message with timeout
        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(2000));
        
        // Assert
        Assert.Equal(messageReceived.Task, received);
        Assert.Equal(testMessage, receivedMessage);
    }

    [Fact]
    public async Task Multiple_Sequential_Messages_Should_Work()
    {
        // Arrange
        var server = new NexusServer(19995);
        _disposables.Add(server);
        
        var messagesReceived = new List<string>();
        var messageCount = 0;
        var allMessagesReceived = new TaskCompletionSource<bool>();
        
        server.OnMessageReceived += (conn, message) =>
        {
            var text = Encoding.UTF8.GetString(message.ToArray());
            messagesReceived.Add(text);
            Interlocked.Increment(ref messageCount);
            
            if (messageCount >= 3)
                allMessagesReceived.SetResult(true);
        };

        _ = server.StartAsync();
        await Task.Delay(200);

        var client = new NexusClient();
        _disposables.Add(client);
        await client.ConnectAsync("127.0.0.1", 19995);
        await Task.Delay(100);

        // Act
        await client.SendMessageAsync(Encoding.UTF8.GetBytes("Message 1"));
        await client.SendMessageAsync(Encoding.UTF8.GetBytes("Message 2"));
        await client.SendMessageAsync(Encoding.UTF8.GetBytes("Message 3"));
        
        var received = await Task.WhenAny(allMessagesReceived.Task, Task.Delay(3000));
        
        // Assert
        Assert.Equal(allMessagesReceived.Task, received);
        Assert.Equal(3, messagesReceived.Count);
        Assert.Contains("Message 1", messagesReceived);
        Assert.Contains("Message 2", messagesReceived);
        Assert.Contains("Message 3", messagesReceived);
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error disposing: {ex.Message}");
            }
        }
    }
}
