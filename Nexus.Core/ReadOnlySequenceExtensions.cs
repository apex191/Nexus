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
}
