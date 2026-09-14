using ExamTemplate.TemplateBuilder;

namespace ExamTemplate.Tests.Template;

public class FontEmbeddingTests
{
    private static readonly Guid SampleKey = new("00112233-4455-6677-8899-AABBCCDDEEFF");

    [Fact]
    public void Key_is_the_guid_bytes_in_reverse_order() =>
        Assert.Equal(Convert.FromHexString("FFEEDDCCBBAA99887766554433221100"), FontEmbedding.DeriveKey(SampleKey));

    [Fact]
    public void Obfuscation_changes_only_the_first_32_bytes_and_is_its_own_inverse()
    {
        var font = Enumerable.Range(0, 64).Select(value => (byte)value).ToArray();

        var obfuscated = FontEmbedding.Obfuscate(font, SampleKey);

        Assert.NotEqual(font[..32], obfuscated[..32]);
        Assert.Equal(font[32..], obfuscated[32..]);
        Assert.Equal(font, FontEmbedding.Obfuscate(obfuscated, SampleKey));
    }

    [Fact]
    public void First_key_byte_is_applied_at_offsets_0_and_16()
    {
        var obfuscated = FontEmbedding.Obfuscate(new byte[40], SampleKey);

        Assert.Equal(0xFF, obfuscated[0]);
        Assert.Equal(0xFF, obfuscated[16]);
        Assert.Equal(0x00, obfuscated[15]);
    }

    [Fact]
    public void Font_key_is_the_braced_upper_case_guid() =>
        Assert.Equal("{00112233-4455-6677-8899-AABBCCDDEEFF}", FontEmbedding.FontKey(SampleKey));
}
