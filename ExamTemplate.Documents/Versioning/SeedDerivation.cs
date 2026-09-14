using System.Buffers.Binary;

namespace ExamTemplate.Documents.Versioning;

public enum SeedPurpose
{
    QuestionOrder = 1,
    OptionOrder = 2,
}

/// <summary>Derives an independent seed per version, purpose and section/question id.</summary>
public static class SeedDerivation
{
    public static ulong Derive(int shuffleSeed, int versionIndex, SeedPurpose purpose, Guid id)
    {
        Span<byte> idBytes = stackalloc byte[16];
        id.TryWriteBytes(idBytes);

        var state = Mix((uint)shuffleSeed);
        state = Mix(state ^ (ulong)versionIndex);
        state = Mix(state ^ (ulong)purpose);
        state = Mix(state ^ BinaryPrimitives.ReadUInt64LittleEndian(idBytes));
        return Mix(state ^ BinaryPrimitives.ReadUInt64LittleEndian(idBytes[8..]));
    }

    // One SplitMix64 step: spreads every input bit and has no fixed point at zero.
    private static ulong Mix(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
