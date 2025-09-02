# 🚀 Nexus

> **High-performance TCP networking library for .NET 8** with pipeline-based message handling

[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](#)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](#)

## ✨ Features

- 🔥 **High-performance** pipeline-based I/O using `System.IO.Pipelines`
- 📦 **Length-prefixed messaging** with automatic framing
- 🔄 **Multi-client server** with concurrent connection handling
- 🛡️ **Memory-safe** with proper resource disposal
- ⚡ **Zero-copy** operations where possible
- 🎯 **Simple API** - connect, send, receive

## 🚀 Quick Start

### Server
```csharp
using var server = new NexusServer(9000);

server.OnClientConnected += (connection) => 
    Console.WriteLine("Client connected!");

server.OnMessageReceived += (connection, message) => {
    var text = Encoding.UTF8.GetString(message.ToArray());
    Console.WriteLine($"Received: {text}");
};

await server.StartAsync();
```

### Client
```csharp
using var client = new NexusClient();

client.OnMessageReceived += (message) => {
    var text = Encoding.UTF8.GetString(message.ToArray());
    Console.WriteLine($"Server: {text}");
};

await client.ConnectAsync("127.0.0.1", 9000);
await client.SendMessageAsync(Encoding.UTF8.GetBytes("Hello!"));
```

## 🏃‍♂️ Running the Examples

```bash
# Terminal 1 - Start Server
cd Nexus.Server && dotnet run

# Terminal 2 - Start Client  
cd Nexus.Client && dotnet run
```

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐
│   NexusClient   │    │   NexusServer   │
├─────────────────┤    ├─────────────────┤
│ • Connect()     │    │ • StartAsync()  │
│ • Send()        │    │ • Multi-client  │
│ • Events        │    │ • Events        │
└─────────┬───────┘    └─────────┬───────┘
          │                      │
          └──────┬─────────────┬──┘
                 │             │
        ┌────────▼─────────────▼────────┐
        │      NexusConnection         │
        ├──────────────────────────────┤
        │ • Pipeline I/O               │
        │ • Message framing            │
        │ • Length prefixes (4 bytes)  │
        │ • Automatic buffering        │
        └──────────────────────────────┘
```

## 📦 Projects

| Project | Description |
|---------|-------------|
| **Nexus.Core** | Core networking library |
| **Nexus.Server** | Example TCP server |
| **Nexus.Client** | Example TCP client |
| **Nexus.Benchmarks** | Performance benchmarks with BenchmarkDotNet |

## 🔧 Message Protocol

```
┌──────────────┬────────────────────┐
│   4 bytes    │    N bytes         │
│   Length     │    Payload         │
│ (Little End) │                    │
└──────────────┴────────────────────┘
```

All messages are automatically framed with a 4-byte little-endian length prefix.

## 🛠️ Building

```bash
dotnet build
dotnet test  # (when tests are added)
```

## 📊 Performance Benchmarks

```bash
# Run all performance benchmarks
cd Nexus.Benchmarks && dotnet run -c Release

# Run specific benchmark categories
dotnet run -c Release -- --filter "*MessageThroughput*"
dotnet run -c Release -- --filter "*Connection*"
```

See [Nexus.Benchmarks](./Nexus.Benchmarks/) for detailed performance analysis.

## 🎯 Use Cases

- **Game servers** - low-latency multiplayer communication
- **Microservices** - high-throughput service-to-service messaging  
- **IoT systems** - efficient device communication
- **Chat applications** - real-time messaging
- **File transfer** - reliable bulk data transmission

## 🔒 Thread Safety

- ✅ **NexusServer**: Thread-safe for multiple clients
- ✅ **NexusClient**: Thread-safe for concurrent operations
- ✅ **NexusConnection**: Internal synchronization handled

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

---

<div align="center">
<b>Built with ❤️ for high-performance .NET networking</b>
</div>
