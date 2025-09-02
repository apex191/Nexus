using System.Buffers;

namespace Nexus.Core;

public static class ReadOnlySequenceExtensions
{
    /// <summary>
    /// Converts a ReadOnlySequence&lt;byte&gt; to a byte array efficiently.
    /// </summary>
    public static byte[] ToArray(this ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsSingleSegment)
        {
            return sequence.FirstSpan.ToArray();
        }

        var array = new byte[sequence.Length];
        sequence.CopyTo(array);
        return array;
    }

    /// <summary>
    /// Copies ReadOnlySequence data to a pre-allocated span for zero-allocation scenarios.
    /// </summary>
    public static void CopyTo(this ReadOnlySequence<byte> sequence, Span<byte> destination)
    {
        if (sequence.IsSingleSegment)
        {
            sequence.FirstSpan.CopyTo(destination);
            return;
        }

        var offset = 0;
        foreach (var segment in sequence)
        {
            segment.Span.CopyTo(destination[offset..]);
            offset += segment.Length;
        }
    }
}
