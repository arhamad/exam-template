using ExamTemplate.Documents.Word;

namespace ExamTemplate.Tests.Word;

public class TemplifyTextTests
{
    [Fact]
    public void Protect_swaps_markdown_and_brace_characters_for_private_use_characters()
    {
        var expected = string.Concat("a", (char)0xE000, "b", (char)0xE001, "c", (char)0xE002, "d", (char)0xE003, "e", (char)0xE004);

        Assert.Equal(expected, TemplifyText.Protect("a*b_c~d{e}"));
    }

    [Theory]
    [InlineData("**عريض** __خط__ ~~شطب~~ {{CourseName}} x_1 5*4*2")]
    [InlineData("نص عربي عادي بلا رموز")]
    [InlineData("")]
    public void Restore_reverses_protect(string text) =>
        Assert.Equal(text, TemplifyText.Restore(TemplifyText.Protect(text)));
}
