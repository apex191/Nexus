# 📊 Nexus Performance Benchmarks

This project contains comprehensive performance benchmarks for the Nexus networking library using BenchmarkDotNet.

## 🚀 Quick Start

```bash
# Run all benchmarks
cd Nexus.Benchmarks && dotnet run -c Release

# Run specific benchmark category
dotnet run -c Release -- --filter "*MessageThroughput*"
dotnet run -c Release -- --filter "*Connection*"
dotnet run -c Release -- --filter "*Memory*"
dotnet run -c Release -- --filter "*Concurrency*"

# Run with memory profiler
dotnet run -c Release -- --memory
```

## 📈 Benchmark Categories

### 🔥 **Message Throughput**
- **Small Messages** (12 bytes) - Tests raw message processing speed
- **Medium Messages** (1KB) - Tests typical application data
- **Large Messages** (64KB) - Tests bulk data transfer performance

### 🔌 **Connection Management**
- **Single Connection** - Basic connection setup/teardown
- **Sequential Connections** - Multiple connections in sequence
- **Concurrent Connections** - Simultaneous connection handling

### 🧠 **Memory Allocation**
- **ReadOnlySequence Conversion** - Memory efficiency comparisons
- **Message Framing** - Allocation patterns during message creation
- **Repeated Operations** - GC pressure under load

### ⚡ **Concurrency Performance**
- **Multiple Clients** - Server performance with concurrent clients
- **High Frequency Messaging** - Burst message handling
- **Connection Churn** - Resource cleanup under stress

## 📊 Sample Results

*(Run benchmarks to see actual performance data for your system)*

```
BenchmarkDotNet=v0.13.12
|                Method | MessageCount |      Mean |     Error |    StdDev |   Gen0 |   Gen1 | Allocated |
|---------------------- |------------- |----------:|----------:|----------:|-------:|-------:|----------:|
|          SmallMessages|          100 |  1.234 ms | 0.0234 ms | 0.0345 ms | 45.123 |  2.345 |     1.2KB |
|         MediumMessages|          100 | 12.456 ms | 0.1234 ms | 0.2345 ms | 89.012 |  5.678 |    12.3KB |
```

## 🎯 Interpreting Results

- **Mean**: Average execution time
- **Error/StdDev**: Statistical accuracy measures
- **Gen0/Gen1**: Garbage collection pressure
- **Allocated**: Memory allocated per operation

## ⚙️ Configuration

Benchmarks run with:
- **.NET 8.0** runtime
- **Release** configuration  
- **Server GC** enabled
- **Memory diagnostics** enabled
- **Threading diagnostics** enabled

## 🔧 Custom Benchmarks

Add your own benchmarks by:

1. Creating a new class in `Benchmarks/`
2. Adding `[Benchmark]` attributes to methods
3. Including the class in `Program.cs`

Example:
```csharp
[Benchmark]
public async Task MyCustomBenchmark()
{
    // Your performance test here
}
```
