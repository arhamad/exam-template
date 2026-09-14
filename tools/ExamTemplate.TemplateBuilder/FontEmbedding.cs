namespace ExamTemplate.TemplateBuilder;

/// <summary>Obfuscated font embedding as defined in ECMA-376 Part 1, §17.8.1.</summary>
public static class FontEmbedding
{
    private const int ObfuscatedLength = 32;

    /// <summary>The 16 key bytes: the GUID's hex digits read as bytes, in reverse order.</summary>
    public static byte[] DeriveKey(Guid key)
    {
        var bytes = Convert.FromHexString(key.ToString("N"));
        Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>XORs the first 32 bytes of the font with the key. Applying it twice restores the font.</summary>
    public static byte[] Obfuscate(byte[] font, Guid key)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(font.Length, ObfuscatedLength);

        var keyBytes = DeriveKey(key);
        var result = (byte[])font.Clone();
        for (var index = 0; index < ObfuscatedLength; index++)
            result[index] ^= keyBytes[index % keyBytes.Length];
        return result;
    }

    /// <summary>The <c>w:fontKey</c> attribute value, e.g. <c>{00112233-4455-6677-8899-AABBCCDDEEFF}</c>.</summary>
    public static string FontKey(Guid key) => key.ToString("B").ToUpperInvariant();
}
