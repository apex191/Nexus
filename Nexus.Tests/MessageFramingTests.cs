using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Nexus.Core;
using Xunit;

namespace Nexus.Tests;

public class MessageFramingTests
{
    [Fact]
    public void ToArray_Extension_Should_Work_For_SingleSegment()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello World!");
        var sequence = new ReadOnlySequence<byte>(data);

        // Act
        var result = sequence.ToArray();

        // Assert
        Assert.Equal(data, result);
    }

    [Fact]
    public void ToArray_Extension_Should_Work_For_MultipleSegments()
    {
        // Arrange
        var segment1 = new byte[] { 1, 2, 3 };
        var segment2 = new byte[] { 4, 5, 6 };
        
        var firstSegment = new TestSegment(segment1);
        var secondSegment = firstSegment.Append(segment2);
        
        var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, segment2.Length);

        // Act
        var result = sequence.ToArray();

        // Assert
        var expected = new byte[] { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1024)]
    [InlineData(65536)]
    public void CreateMessageFrame_Should_Create_Correct_Length_Prefix(int payloadLength)
    {
        // Arrange
        var payload = new byte[payloadLength];
        Random.Shared.NextBytes(payload);

        // Act - Simulate what NexusClient.SendMessageAsync does
        var message = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(message.AsSpan(0, 4), payload.Length);
        payload.CopyTo(message, 4);

        // Assert
        var readLength = BinaryPrimitives.ReadInt32LittleEndian(message.AsSpan(0, 4));
        Assert.Equal(payloadLength, readLength);
        
        var readPayload = message.AsSpan(4);
        Assert.True(payload.AsSpan().SequenceEqual(readPayload));
    }

    [Fact]
    public void LittleEndian_Consistency_Should_Work()
    {
        // Arrange
        var lengths = new int[] { 0, 1, 255, 256, 65535, 65536, 1000000 };

        foreach (var length in lengths)
        {
            // Act
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, length);
            var readBack = BinaryPrimitives.ReadInt32LittleEndian(buffer);

            // Assert
            Assert.Equal(length, readBack);
        }
    }

    // Helper class for creating multi-segment sequences
    private class TestSegment : ReadOnlySequenceSegment<byte>
    {
        public TestSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public TestSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new TestSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }
}
