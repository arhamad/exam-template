namespace ExamTemplate.Documents.Versioning;

/// <summary>
/// SplitMix64 generator. Used instead of <see cref="Random"/> because printed versions must be
/// reproducible from the saved seed on any runtime; the algorithm is part of the exam file format.
/// </summary>
public sealed class ShuffleRandom(ulong seed)
{
    private ulong _state = seed;

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A value in <c>[0, exclusiveMax)</c> taken from the high half of a 128-bit product.</summary>
    public int NextInt(int exclusiveMax)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMax);
        return (int)Math.BigMul(NextUInt64(), (ulong)exclusiveMax, out _);
    }
}
